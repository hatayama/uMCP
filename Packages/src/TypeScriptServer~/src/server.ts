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
import {
  ENVIRONMENT,
  DEFAULT_MESSAGES,
  MCP_PROTOCOL_VERSION,
  MCP_SERVER_NAME,
  TOOLS_LIST_CHANGED_CAPABILITY,
  DEV_TOOL_PING_NAME,
  DEV_TOOL_PING_DESCRIPTION,
  DEV_TOOL_COMMANDS_NAME,
  DEV_TOOL_COMMANDS_DESCRIPTION,
  DEFAULT_CLIENT_NAME,
} from './constants.js';
import packageJson from '../package.json' assert { type: 'json' };
import { ToolResponse } from './types/tool-types.js';

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

    // Setup polling callback for connection recovery
    this.unityClient.setReconnectedCallback(() => {
      void this.refreshDynamicToolsSafe();
    });

    this.setupHandlers();
    this.setupSignalHandlers();
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
    const commandDetailsResponse = await this.unityClient.executeCommand('get-command-details', {});

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
      if (clientInfo?.name) {
        this.clientName = clientInfo.name;
        infoToFile(`[Unity MCP] Client name received: ${this.clientName}`);
      }

      // Initialize Unity connection after receiving client name
      if (!this.isInitialized) {
        this.isInitialized = true;
        infoToFile(`[Unity MCP] Initializing Unity connection with client name: ${this.clientName}`);
        
        await this.initializeDynamicTools();
        infoToFile('[Unity MCP] Unity connection established successfully');
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

      // Development tools (only in development mode)
      if (this.isDevelopment) {
        tools.push({
          name: DEV_TOOL_PING_NAME,
          description: DEV_TOOL_PING_DESCRIPTION,
          inputSchema: {
            type: 'object',
            properties: {
              message: {
                type: 'string',
                description: 'Test message',
                default: DEFAULT_MESSAGES.PING,
              },
            },
          },
        });

        tools.push({
          name: DEV_TOOL_COMMANDS_NAME,
          description: DEV_TOOL_COMMANDS_DESCRIPTION,
          inputSchema: {
            type: 'object',
            properties: {},
            additionalProperties: false,
          },
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

        switch (name) {
          case DEV_TOOL_PING_NAME:
            if (this.isDevelopment) {
              return await this.handlePing(args as { message?: string });
            }
            throw new Error('Development tool not available in production');
          case DEV_TOOL_COMMANDS_NAME:
            if (this.isDevelopment) {
              return await this.handleGetAvailableCommands();
            }
            throw new Error('Development tool not available in production');
          default:
            // Test tools removed - no longer needed for production
            throw new Error(`Unknown tool: ${name}`);
        }
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
   * Handle TypeScript ping command (dev only)
   */
  private async handlePing(args: { message?: string }): Promise<ToolResponse> {
    const message = args?.message || DEFAULT_MESSAGES.PING;
    return {
      content: [
        {
          type: 'text',
          text: `Unity MCP Server is running! Message: ${message}`,
        },
      ],
    };
  }

  /**
   * Handle get available commands (dev only)
   */
  private async handleGetAvailableCommands(): Promise<ToolResponse> {
    try {
      await this.unityClient.ensureConnected();

      // Get command names only (avoid duplicate getCommandDetails call)
      // BUG FIX: Previously called both getAvailableCommands and getCommandDetails,
      // causing duplicate API calls when multiple MCP clients were connected
      const commandsResponse = await this.unityClient.executeCommand('getAvailableCommands', {});
      const commands = (commandsResponse as { Commands?: unknown })?.Commands || commandsResponse;

      // Use already cached dynamic tools info instead of calling getCommandDetails again
      // BUG FIX: Avoid redundant getCommandDetails call since data is already cached in initializeDynamicTools
      const dynamicToolsInfo = Array.from(this.dynamicTools.entries()).map(([name, tool]) => ({
        name,
        description: tool.description,
        inputSchema: tool.inputSchema,
      }));

      return {
        content: [
          {
            type: 'text',
            text: `Available Unity Commands:\n${JSON.stringify(commands, null, 2)}\n\nCached Dynamic Tools:\n${JSON.stringify(dynamicToolsInfo, null, 2)}`,
          },
        ],
      };
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : 'Unknown error';
      return {
        content: [
          {
            type: 'text',
            text: `Failed to get Unity commands: ${errorMessage}`,
          },
        ],
        isError: true,
      };
    }
  }

  /**
   * Start the server
   */
  async start(): Promise<void> {
    // Setup Unity event notification listener (will be used after Unity connection)
    this.setupUnityEventListener();

    // Connect to MCP transport first - wait for client name before connecting to Unity
    const transport = new StdioServerTransport();
    await this.server.connect(transport);
    
    infoToFile('[Unity MCP] Server started, waiting for client initialization...');
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
   * Send tools changed notification
   */
  private sendToolsChangedNotification(): void {
    try {
      this.server.notification({
        method: 'notifications/tools/list_changed',
        params: {},
      });
    } catch (error) {
      errorToFile('[Unity MCP] Failed to send tools changed notification:', error);
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
