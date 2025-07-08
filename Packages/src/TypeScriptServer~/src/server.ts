#!/usr/bin/env node

import { Server } from '@modelcontextprotocol/sdk/server/index.js';
import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';
import {
  CallToolRequestSchema,
  ListToolsRequestSchema,
  InitializeRequestSchema,
  Tool,
} from '@modelcontextprotocol/sdk/types.js';
import { UnityClient } from './unity-client.js';
import { DynamicUnityCommandTool } from './tools/dynamic-unity-command-tool.js';
import { errorToFile, debugToFile, infoToFile } from './utils/log-to-file.js';
import { UnityDiscovery } from './unity-discovery.js';
import {
  ENVIRONMENT,
  MCP_PROTOCOL_VERSION,
  MCP_SERVER_NAME,
  TOOLS_LIST_CHANGED_CAPABILITY,
  DEFAULT_CLIENT_NAME,
  LIST_CHANGED_UNSUPPORTED_CLIENTS,
} from './constants.js';
import packageJson from '../package.json' assert { type: 'json' };

/**
 * Unity MCP Server - Bridge between MCP protocol and Unity Editor
 *
 * Design document reference: Packages/src/Editor/ARCHITECTURE.md
 *
 * Related classes:
 * - UnityClient: Handles the TCP connection to the Unity Editor
 * - DynamicUnityCommandTool: Dynamically creates tools based on commands from Unity
 * - ConnectionManager: Manages connection polling (used via UnityClient)
 * - MessageHandler: Handles JSON-RPC messages (used via UnityClient)
 * - @modelcontextprotocol/sdk/server: The core MCP server implementation
 */
class UnityMcpServer {
  private server: Server;
  private unityClient: UnityClient;
  private readonly isDevelopment: boolean;
  private readonly dynamicTools: Map<string, DynamicUnityCommandTool> = new Map();
  private isShuttingDown: boolean = false;
  private isRefreshing: boolean = false;
  private clientName: string = DEFAULT_CLIENT_NAME;
  private isInitialized: boolean = false;
  private isNotifying: boolean = false;
  private unityDiscovery: UnityDiscovery;

  constructor() {
    // Simple environment variable check
    this.isDevelopment = process.env.NODE_ENV === ENVIRONMENT.NODE_ENV_DEVELOPMENT;

    infoToFile('Unity MCP Server Starting');
    infoToFile(`Environment variable: NODE_ENV=${process.env.NODE_ENV}`);
    infoToFile(`Development mode: ${this.isDevelopment}`);

    this.server = new Server(
      {
        name: MCP_SERVER_NAME,
        version: packageJson.version,
      },
      {
        capabilities: {
          tools: {
            listChanged: TOOLS_LIST_CHANGED_CAPABILITY,
          },
        },
      },
    );

    this.unityClient = new UnityClient();

    // Initialize Unity discovery service (singleton pattern prevents duplicates)
    this.unityDiscovery = new UnityDiscovery(this.unityClient);
    this.unityDiscovery.setOnDiscoveredCallback(async () => {
      await this.handleUnityDiscovered();
    });

    // Setup connection lost callback for connection recovery
    this.unityDiscovery.setOnConnectionLostCallback(() => {
      if (this.isDevelopment) {
        debugToFile(
          '[Unity MCP] Connection lost detected - tools will be refreshed after reconnection',
        );
      }
    });

    // Set UnityDiscovery reference in UnityClient for unified connection management
    this.unityClient.setUnityDiscovery(this.unityDiscovery);

    // Setup polling callback for connection recovery
    this.unityClient.setReconnectedCallback(async () => {
      // Force Unity discovery for faster reconnection
      await this.unityDiscovery.forceDiscovery();
      void this.refreshDynamicToolsSafe();
    });

    this.setupHandlers();
    this.setupSignalHandlers();
  }

  /**
   * Check if client doesn't support list_changed notifications
   */
  private isListChangedUnsupported(clientName: string): boolean {
    if (!clientName) {
      return false;
    }

    const normalizedName = clientName.toLowerCase();
    return LIST_CHANGED_UNSUPPORTED_CLIENTS.some((unsupported) =>
      normalizedName.includes(unsupported),
    );
  }

  /**
   * Wait for Unity connection with timeout
   */
  private async waitForUnityConnectionWithTimeout(timeoutMs: number): Promise<void> {
    return new Promise((resolve, reject) => {
      const timeout = setTimeout(() => {
        reject(new Error(`Unity connection timeout after ${timeoutMs}ms`));
      }, timeoutMs);

      const checkConnection = async () => {
        if (this.unityClient.connected) {
          clearTimeout(timeout);
          resolve();
          return;
        }

        // Start Unity discovery if not already running
        this.unityDiscovery.start();

        // Set up callback for when Unity is discovered
        this.unityDiscovery.setOnDiscoveredCallback(async () => {
          // Wait for actual connection establishment
          await this.unityClient.ensureConnected();
          clearTimeout(timeout);
          resolve();
        });
      };

      void checkConnection();
    });
  }

  /**
   * Get tools from Unity
   */
  private async getToolsFromUnity(): Promise<Tool[]> {
    if (!this.unityClient.connected) {
      return [];
    }

    try {
      await this.handleClientNameInitialization();
      const commandDetails = await this.fetchCommandDetailsFromUnity();

      if (!commandDetails) {
        return [];
      }

      this.createDynamicToolsFromCommands(commandDetails);

      // Convert dynamic tools to Tool array
      const tools: Tool[] = [];
      for (const [toolName, dynamicTool] of this.dynamicTools) {
        tools.push({
          name: toolName,
          description: dynamicTool.description,
          inputSchema: dynamicTool.inputSchema,
        });
      }

      return tools;
    } catch (error) {
      errorToFile('[Unity MCP] Failed to get tools from Unity:', error);
      return [];
    }
  }

  /**
   * Initialize dynamic Unity command tools
   */
  private async initializeDynamicTools(): Promise<void> {
    try {
      await this.unityClient.ensureConnected();

      await this.handleClientNameInitialization();

      const commandDetails = await this.fetchCommandDetailsFromUnity();
      if (!commandDetails) {
        return;
      }

      this.createDynamicToolsFromCommands(commandDetails);

      // Command details processed successfully
    } catch (error) {
      errorToFile('[Unity MCP] Failed to initialize dynamic tools:', error);
      // Continue without dynamic tools
    }
  }

  /**
   * Handle client name initialization and setup
   */
  private async handleClientNameInitialization(): Promise<void> {
    // Client name handling:
    // 1. Primary: clientInfo.name from MCP protocol initialize request
    // 2. Fallback: MCP_CLIENT_NAME environment variable (for backward compatibility)
    // 3. Default: Empty string (Unity will show "No Client" in UI)
    // Note: MCP_CLIENT_NAME is deprecated but kept for compatibility with older setups
    if (!this.clientName) {
      const fallbackName = process.env.MCP_CLIENT_NAME;
      if (fallbackName) {
        this.clientName = fallbackName;
        await this.unityClient.setClientName(fallbackName);
        infoToFile(`[Unity MCP] Fallback client name set to Unity: ${fallbackName}`);
      } else {
        infoToFile('[Unity MCP] No client name set, waiting for initialize request');
      }
    } else {
      // Send the already set client name to Unity
      await this.unityClient.setClientName(this.clientName);
      infoToFile(`[Unity MCP] Client name already set, sending to Unity: ${this.clientName}`);
    }

    // Register reconnect handler to re-send client name after reconnection
    this.unityClient.onReconnect(() => {
      infoToFile(`[Unity MCP] Reconnected - resending client name: ${this.clientName}`);
      void this.unityClient.setClientName(this.clientName);
    });
  }

  /**
   * Fetch command details from Unity
   */
  private async fetchCommandDetailsFromUnity(): Promise<unknown[] | null> {
    // Get detailed command information including schemas
    // Include development-only commands if in development mode
    const params = { IncludeDevelopmentOnly: this.isDevelopment };
    const commandDetailsResponse = await this.unityClient.executeCommand(
      'get-command-details',
      params,
    );

    // Handle new GetCommandDetailsResponse structure
    const commandDetails =
      (commandDetailsResponse as { Commands?: unknown[] })?.Commands || commandDetailsResponse;
    if (!Array.isArray(commandDetails)) {
      errorToFile('[Unity MCP] Invalid command details response:', commandDetailsResponse);
      return null;
    }

    return commandDetails;
  }

  /**
   * Create dynamic tools from Unity command details
   */
  private createDynamicToolsFromCommands(commandDetails: unknown[]): void {
    // Create dynamic tools for each Unity command
    this.dynamicTools.clear();
    const toolContext = { unityClient: this.unityClient };

    for (const commandInfo of commandDetails) {
      const commandName = (commandInfo as { name: string }).name;
      const description =
        (commandInfo as { description?: string }).description ||
        `Execute Unity command: ${commandName}`;
      const parameterSchema = (commandInfo as { parameterSchema?: unknown }).parameterSchema;
      const displayDevelopmentOnly =
        (commandInfo as { displayDevelopmentOnly?: boolean }).displayDevelopmentOnly || false;

      // Skip development-only commands in production mode
      if (displayDevelopmentOnly && !this.isDevelopment) {
        continue;
      }

      const toolName = commandName;

      const dynamicTool = new DynamicUnityCommandTool(
        toolContext,
        commandName,
        description,
        parameterSchema, // Pass schema information
      );

      this.dynamicTools.set(toolName, dynamicTool);
    }
  }

  /**
   * Refresh dynamic tools by re-fetching from Unity
   * This method can be called to update the tool list when Unity commands change
   */
  private async refreshDynamicTools(): Promise<void> {
    await this.initializeDynamicTools();

    // Send tools changed notification to MCP client
    this.sendToolsChangedNotification();
  }

  /**
   * Safe version of refreshDynamicTools that prevents duplicate execution
   */
  private async refreshDynamicToolsSafe(): Promise<void> {
    if (this.isRefreshing) {
      if (this.isDevelopment) {
        debugToFile('[TRACE] refreshDynamicToolsSafe skipped: already in progress');
      }
      return;
    }

    this.isRefreshing = true;
    try {
      if (this.isDevelopment) {
        const stack = new Error().stack;
        const callerLine = stack?.split('\n')[2]?.trim() || 'Unknown caller';
        const timestamp = new Date().toISOString().split('T')[1].slice(0, 12);
        debugToFile(`[TRACE] refreshDynamicToolsSafe called at ${timestamp} from: ${callerLine}`);
      }

      await this.refreshDynamicTools();
    } finally {
      this.isRefreshing = false;
    }
  }

  private setupHandlers(): void {
    // Handle initialize request to get client information
    this.server.setRequestHandler(InitializeRequestSchema, async (request) => {
      const clientInfo = request.params?.clientInfo;
      const clientName = clientInfo?.name || '';

      if (clientName) {
        this.clientName = clientName;
        infoToFile(`[Unity MCP] Client name received: ${this.clientName}`);
      }

      // Initialize Unity connection after receiving client name
      if (!this.isInitialized) {
        this.isInitialized = true;

        if (this.isListChangedUnsupported(clientName)) {
          // list_changed非対応クライアント: Unity接続を待つ
          infoToFile(
            `[Unity MCP] Sync initialization for list_changed unsupported client: ${clientName}`,
          );

          try {
            await this.waitForUnityConnectionWithTimeout(10000);
            const tools = await this.getToolsFromUnity();

            infoToFile(`[Unity MCP] Returning ${tools.length} tools for ${clientName}`);
            return {
              protocolVersion: MCP_PROTOCOL_VERSION,
              capabilities: {
                tools: {
                  listChanged: TOOLS_LIST_CHANGED_CAPABILITY,
                },
              },
              serverInfo: {
                name: MCP_SERVER_NAME,
                version: packageJson.version,
              },
              tools,
            };
          } catch (error) {
            errorToFile(`[Unity MCP] Unity connection timeout for ${clientName}:`, error);
            return {
              protocolVersion: MCP_PROTOCOL_VERSION,
              capabilities: {
                tools: {
                  listChanged: TOOLS_LIST_CHANGED_CAPABILITY,
                },
              },
              serverInfo: {
                name: MCP_SERVER_NAME,
                version: packageJson.version,
              },
              tools: [],
            };
          }
        } else {
          // list_changed対応クライアント: 非同期方式
          infoToFile(
            `[Unity MCP] Async initialization for list_changed supported client: ${clientName}`,
          );

          // Start Unity connection initialization in background
          void this.initializeDynamicTools()
            .then(() => {
              infoToFile('[Unity MCP] Unity connection established successfully');
            })
            .catch((error) => {
              errorToFile('[Unity MCP] Unity connection initialization failed:', error);
              // Start Unity discovery to retry connection (singleton pattern prevents duplicates)
              this.unityDiscovery.start();
            });
        }
      }

      return {
        protocolVersion: MCP_PROTOCOL_VERSION,
        capabilities: {
          tools: {
            listChanged: TOOLS_LIST_CHANGED_CAPABILITY,
          },
        },
        serverInfo: {
          name: MCP_SERVER_NAME,
          version: packageJson.version,
        },
      };
    });

    // Provide tool list
    this.server.setRequestHandler(ListToolsRequestSchema, () => {
      const tools: Tool[] = [];

      // Dynamic Unity command tools
      for (const [toolName, dynamicTool] of this.dynamicTools) {
        tools.push({
          name: toolName,
          description: dynamicTool.description,
          inputSchema: dynamicTool.inputSchema,
        });
      }

      // Add test tools for notification testing
      // Commented out to reduce tool noise during development
      /*
      for (let i = 1; i <= this.toolCount; i++) {
        tools.push({
          name: `test-tool-${i}`,
          description: `Test tool number ${i}`,
          inputSchema: {
            type: 'object',
            properties: {
              message: {
                type: 'string',
                description: 'Test message'
              }
            }
          }
        });
      }
      */

      debugToFile(`Providing ${tools.length} tools`, { toolNames: tools.map((t) => t.name) });
      return { tools };
    });

    // Handle tool execution
    this.server.setRequestHandler(CallToolRequestSchema, async (request) => {
      const { name, arguments: args } = request.params;

      debugToFile(`Tool executed: ${name}`, { args });

      try {
        // Check if it's a dynamic Unity command tool
        if (this.dynamicTools.has(name)) {
          const dynamicTool = this.dynamicTools.get(name)!;
          return await dynamicTool.execute(args);
        }

        // All tools should be handled by dynamic tools
        throw new Error(`Unknown tool: ${name}`);
      } catch (error) {
        return {
          content: [
            {
              type: 'text',
              text: `Error executing ${name}: ${error instanceof Error ? error.message : 'Unknown error'}`,
            },
          ],
          isError: true,
        };
      }
    });
  }

  /**
   * Start the server
   */
  async start(): Promise<void> {
    // Setup Unity event notification listener (will be used after Unity connection)
    this.setupUnityEventListener();

    // Start Unity discovery immediately (singleton pattern prevents duplicates)
    this.unityDiscovery.start();

    if (this.isDevelopment) {
      debugToFile('[Unity MCP] Server starting with unified discovery service');
    }

    // Connect to MCP transport first - wait for client name before connecting to Unity
    const transport = new StdioServerTransport();
    await this.server.connect(transport);
  }

  /**
   * Setup Unity event listener for automatic tool updates
   */
  private setupUnityEventListener(): void {
    // Listen for MCP standard notifications from Unity
    this.unityClient.onNotification('notifications/tools/list_changed', async (params: unknown) => {
      if (this.isDevelopment) {
        const timestamp = new Date().toISOString().split('T')[1].slice(0, 12);
        debugToFile(
          `[TRACE] Unity notification received at ${timestamp}: notifications/tools/list_changed`,
        );
        debugToFile(`[TRACE] Notification params: ${JSON.stringify(params)}`);
      }

      try {
        await this.refreshDynamicToolsSafe();
      } catch (error) {
        errorToFile('[Unity MCP] Failed to update dynamic tools via Unity notification:', error);
      }
    });
  }

  /**
   * Handle Unity discovery and establish connection
   */
  private async handleUnityDiscovered(): Promise<void> {
    try {
      await this.unityClient.ensureConnected();

      // If we have a client name, initialize tools immediately
      if (this.clientName) {
        await this.initializeDynamicTools();
        infoToFile('[Unity MCP] Unity connection established and tools initialized');

        // Send immediate tools changed notification for faster recovery
        this.sendToolsChangedNotification();
      } else {
        infoToFile('[Unity MCP] Unity connection established, waiting for client name');
      }

      this.unityDiscovery.stop();
    } catch (error) {
      errorToFile('[Unity MCP] Failed to establish Unity connection after discovery:', error);
    }
  }

  /**
   * Send tools changed notification (with duplicate prevention)
   */
  private sendToolsChangedNotification(): void {
    if (this.isNotifying) {
      if (this.isDevelopment) {
        debugToFile('[TRACE] sendToolsChangedNotification skipped: already notifying');
      }
      return;
    }

    this.isNotifying = true;
    try {
      this.server.notification({
        method: 'notifications/tools/list_changed',
        params: {},
      });
      if (this.isDevelopment) {
        debugToFile('[TRACE] tools/list_changed notification sent');
      }
    } catch (error) {
      errorToFile('[Unity MCP] Failed to send tools changed notification:', error);
    } finally {
      this.isNotifying = false;
    }
  }

  /**
   * Setup signal handlers for graceful shutdown
   */
  private setupSignalHandlers(): void {
    // Handle Ctrl+C (SIGINT)
    process.on('SIGINT', () => {
      infoToFile('[Unity MCP] Received SIGINT, shutting down...');
      this.gracefulShutdown();
    });

    // Handle kill command (SIGTERM)
    process.on('SIGTERM', () => {
      infoToFile('[Unity MCP] Received SIGTERM, shutting down...');
      this.gracefulShutdown();
    });

    // Handle terminal close (SIGHUP)
    process.on('SIGHUP', () => {
      infoToFile('[Unity MCP] Received SIGHUP, shutting down...');
      this.gracefulShutdown();
    });

    // Handle stdin close (when parent process disconnects)
    // BUG FIX: Added STDIN monitoring to detect when Cursor/parent MCP client disconnects
    // This prevents orphaned Node processes from remaining after IDE shutdown
    process.stdin.on('close', () => {
      infoToFile('[Unity MCP] STDIN closed, shutting down...');
      this.gracefulShutdown();
    });

    process.stdin.on('end', () => {
      infoToFile('[Unity MCP] STDIN ended, shutting down...');
      this.gracefulShutdown();
    });

    // Handle uncaught exceptions
    // BUG FIX: Added comprehensive error handling to prevent hanging processes
    process.on('uncaughtException', (error) => {
      errorToFile('[Unity MCP] Uncaught exception:', error);
      this.gracefulShutdown();
    });

    process.on('unhandledRejection', (reason, promise) => {
      errorToFile('[Unity MCP] Unhandled rejection at:', promise, 'reason:', reason);
      this.gracefulShutdown();
    });
  }

  /**
   * Graceful shutdown with proper cleanup
   * BUG FIX: Enhanced shutdown process to prevent orphaned Node processes
   */
  private gracefulShutdown(): void {
    // Prevent multiple shutdown attempts
    if (this.isShuttingDown) {
      return;
    }

    this.isShuttingDown = true;
    infoToFile('[Unity MCP] Starting graceful shutdown...');

    try {
      // Disconnect from Unity and stop all intervals
      // BUG FIX: Ensure polling intervals are stopped to prevent hanging event loop
      this.unityDiscovery.stop();
      if (this.unityClient) {
        this.unityClient.disconnect();
      }

      // Clear any remaining timers to ensure clean exit
      // BUG FIX: Force garbage collection if available to clean up lingering references
      if (global.gc) {
        global.gc();
      }
    } catch (error) {
      errorToFile('[Unity MCP] Error during cleanup:', error);
    }

    infoToFile('[Unity MCP] Graceful shutdown completed');
    process.exit(0);
  }
}

// Start server
const server = new UnityMcpServer();

server.start().catch((error) => {
  errorToFile('[FATAL] Server startup failed:', error);
  console.error('[FATAL] Unity MCP Server startup failed:');
  console.error('Error details:', error instanceof Error ? error.message : String(error));
  console.error('Stack trace:', error instanceof Error ? error.stack : 'No stack trace available');
  console.error('Make sure Unity is running and the MCP bridge is properly configured.');
  process.exit(1);
});
