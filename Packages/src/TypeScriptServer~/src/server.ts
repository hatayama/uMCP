#!/usr/bin/env node

import { Server } from '@modelcontextprotocol/sdk/server/index.js';
import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';
import {
  CallToolRequestSchema,
  ListToolsRequestSchema,
  InitializeRequestSchema,
  ListResourcesRequestSchema,
  ReadResourceRequestSchema,
  SubscribeRequestSchema,
} from '@modelcontextprotocol/sdk/types.js';
import { UnityClient } from './unity-client.js';
import { errorToFile, debugToFile, infoToFile } from './utils/log-to-file.js';
import { UnityDiscovery } from './unity-discovery.js';
import { UnityConnectionManager } from './unity-connection-manager.js';
import { UnityToolManager } from './unity-tool-manager.js';
import { McpClientCompatibility } from './mcp-client-compatibility.js';
import { UnityEventHandler } from './unity-event-handler.js';
import { ToolResponse } from './types/tool-types.js';
import {
  ENVIRONMENT,
  MCP_PROTOCOL_VERSION,
  MCP_SERVER_NAME,
  TOOLS_LIST_CHANGED_CAPABILITY,
} from './constants.js';
import packageJson from '../package.json' assert { type: 'json' };

/**
 * Unity MCP Server - Bridge between MCP protocol and Unity Editor
 *
 * Design document reference: Packages/src/TypeScriptServer~/ARCHITECTURE.md
 *
 * Related classes:
 * - UnityConnectionManager: Manages Unity connection and discovery
 * - UnityToolManager: Manages dynamic tool generation and lifecycle
 * - McpClientCompatibility: Handles client-specific compatibility
 * - UnityEventHandler: Manages events and graceful shutdown
 * - UnityClient: Handles the TCP connection to the Unity Editor
 * - DynamicUnityCommandTool: Dynamically creates tools based on Unity tools
 * - @modelcontextprotocol/sdk/server: The core MCP server implementation
 */
class UnityMcpServer {
  private server: Server;
  private unityClient: UnityClient;
  private readonly isDevelopment: boolean;
  private isInitialized: boolean = false;
  private unityDiscovery: UnityDiscovery;
  private connectionManager: UnityConnectionManager;
  private toolManager: UnityToolManager;
  private clientCompatibility: McpClientCompatibility;
  private eventHandler: UnityEventHandler;

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
          resources: {
            subscribe: true,
            listChanged: true,
          },
        },
      },
    );

    this.unityClient = new UnityClient();

    // Initialize Unity connection manager
    this.connectionManager = new UnityConnectionManager(this.unityClient);
    this.unityDiscovery = this.connectionManager.getUnityDiscovery();

    // Initialize Unity tool manager
    this.toolManager = new UnityToolManager(this.unityClient);

    // Initialize MCP client compatibility manager
    this.clientCompatibility = new McpClientCompatibility(this.unityClient);

    // Initialize Unity event handler
    this.eventHandler = new UnityEventHandler(
      this.server,
      this.unityClient,
      this.connectionManager,
    );

    // Setup reconnection callback for tool refresh
    this.connectionManager.setupReconnectionCallback(async () => {
      await this.toolManager.refreshDynamicToolsSafe(() => {
        this.eventHandler.sendToolsChangedNotification();
      });
    });

    this.setupHandlers();
    this.eventHandler.setupSignalHandlers();
  }

  private setupHandlers(): void {
    // Handle initialize request to get client information
    this.server.setRequestHandler(InitializeRequestSchema, async (request) => {
      const clientInfo = request.params?.clientInfo;
      const clientName = clientInfo?.name || '';

      if (clientName) {
        this.clientCompatibility.setClientName(clientName);
        this.clientCompatibility.logClientCompatibility(clientName);
        infoToFile(`[Unity MCP] Client name received: ${clientName}`);
      }

      // Initialize Unity connection after receiving client name
      if (!this.isInitialized) {
        this.isInitialized = true;

        if (this.clientCompatibility.isListChangedUnsupported(clientName)) {
          // list_changed unsupported client: wait for Unity connection
          infoToFile(
            `[Unity MCP] Sync initialization for list_changed unsupported client: ${clientName}`,
          );

          try {
            await this.clientCompatibility.initializeClient(clientName);
            this.toolManager.setClientName(clientName);
            await this.connectionManager.waitForUnityConnectionWithTimeout(10000);
            const tools = await this.toolManager.getToolsFromUnity();

            infoToFile(`[Unity MCP] Returning ${tools.length} tools for ${clientName}`);
            return {
              protocolVersion: MCP_PROTOCOL_VERSION,
              capabilities: {
                tools: {
                  listChanged: TOOLS_LIST_CHANGED_CAPABILITY,
                },
                resources: {
                  subscribe: true,
                  listChanged: true,
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
                resources: {
                  subscribe: true,
                  listChanged: true,
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
          // list_changed supported client: asynchronous approach
          infoToFile(
            `[Unity MCP] Async initialization for list_changed supported client: ${clientName}`,
          );

          // Start Unity connection initialization in background
          void this.clientCompatibility.initializeClient(clientName);
          this.toolManager.setClientName(clientName);
          void this.toolManager
            .initializeDynamicTools()
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
          resources: {
            subscribe: true,
            listChanged: true,
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
      const tools = this.toolManager.getAllTools();

      debugToFile(`Providing ${tools.length} tools`, { toolNames: tools.map((t) => t.name) });
      return { tools };
    });

    // Handle tool execution
    this.server.setRequestHandler(CallToolRequestSchema, async (request) => {
      const { name, arguments: args } = request.params;

      debugToFile(`Tool executed: ${name}`, { args });

      try {
        // Check if it's a dynamic Unity tool
        if (this.toolManager.hasTool(name)) {
          const dynamicTool = this.toolManager.getTool(name);
          if (!dynamicTool) {
            throw new Error(`Tool ${name} is not available`);
          }
          const result: ToolResponse = await dynamicTool.execute(args);
          return result;
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

    // Handle resources/list request
    this.server.setRequestHandler(ListResourcesRequestSchema, async (request) => {
      console.log('[DEBUG] Resources list requested', { cursor: request.params?.cursor });
      debugToFile('Resources list requested', { cursor: request.params?.cursor });
      
      try {
        // Forward to Unity
        console.log('[DEBUG] Sending resources/list to Unity...', { params: request.params || {} });
        debugToFile('Sending resources/list to Unity...', { params: request.params || {} });
        const response = await this.unityClient.executeTool('resources/list', request.params || {});
        console.log('[DEBUG] Resources list response from Unity', { 
          resourceCount: response.resources?.length,
          fullResponse: response,
          responseType: typeof response,
          responseKeys: Object.keys(response || {}),
          finalResources: response.resources,
          finalNextCursor: response.nextCursor
        });
        debugToFile('Resources list response from Unity', { 
          resourceCount: response.resources?.length,
          fullResponse: response,
          responseType: typeof response,
          responseKeys: Object.keys(response || {}),
          finalResources: response.resources,
          finalNextCursor: response.nextCursor
        });
        
        const finalResponse = {
          resources: response.resources || [],
          nextCursor: response.nextCursor,
        };
        
        console.log('[DEBUG] Final MCP response being returned', {
          finalResourceCount: finalResponse.resources?.length,
          finalResponse: finalResponse
        });
        debugToFile('Final MCP response being returned', {
          finalResourceCount: finalResponse.resources?.length,
          finalResponse: finalResponse
        });
        
        return finalResponse;
      } catch (error) {
        console.error('[ERROR] Error listing resources:', error);
        errorToFile('Error listing resources:', error);
        return {
          resources: [],
        };
      }
    });

    // Handle resources/read request
    this.server.setRequestHandler(ReadResourceRequestSchema, async (request) => {
      const { uri } = request.params;
      debugToFile(`Resource read requested: ${uri}`);
      
      try {
        // Forward to Unity
        const response = await this.unityClient.executeTool('resources/read', { uri });
        debugToFile(`Resource read response from Unity for ${uri}`, { contentLength: response.contents?.length });
        return {
          contents: response.contents || [],
        };
      } catch (error) {
        errorToFile(`Error reading resource ${uri}:`, error);
        throw error;
      }
    });

    // Handle resources/subscribe request
    this.server.setRequestHandler(SubscribeRequestSchema, async (request) => {
      const { uri } = request.params;
      debugToFile(`Resource subscription requested: ${uri}`);
      
      try {
        // For now, just acknowledge the subscription
        // Unity side doesn't implement subscription yet, but we accept it
        infoToFile(`Resource subscription accepted for: ${uri}`);
        return {};
      } catch (error) {
        errorToFile(`Error subscribing to resource ${uri}:`, error);
        throw error;
      }
    });
  }

  /**
   * Start the server
   */
  async start(): Promise<void> {
    // Setup Unity event notification listener (will be used after Unity connection)
    this.eventHandler.setupUnityEventListener(async () => {
      await this.toolManager.refreshDynamicToolsSafe(() => {
        this.eventHandler.sendToolsChangedNotification();
      });
    });

    // Initialize connection manager with callback for tool initialization
    this.connectionManager.initialize(async () => {
      // If we have a client name, initialize tools immediately
      const clientName = this.clientCompatibility.getClientName();
      if (clientName) {
        this.toolManager.setClientName(clientName);
        await this.toolManager.initializeDynamicTools();
        infoToFile('[Unity MCP] Unity connection established and tools initialized');

        // Send immediate tools changed notification for faster recovery
        this.eventHandler.sendToolsChangedNotification();
      } else {
        infoToFile('[Unity MCP] Unity connection established, waiting for client name');
      }
    });

    if (this.isDevelopment) {
      debugToFile('[Unity MCP] Server starting with unified discovery service');
    }

    // Connect to MCP transport first - wait for client name before connecting to Unity
    const transport = new StdioServerTransport();
    await this.server.connect(transport);
  }
}

// Start server
const server = new UnityMcpServer();

server.start().catch((error) => {
  errorToFile('[FATAL] Server startup failed:', error);
  errorToFile('[FATAL] Unity MCP Server startup failed:');
  errorToFile('Error details:', error instanceof Error ? error.message : String(error));
  errorToFile('Stack trace:', error instanceof Error ? error.stack : 'No stack trace available');
  errorToFile('Make sure Unity is running and the MCP bridge is properly configured.');
  process.exit(1);
});
