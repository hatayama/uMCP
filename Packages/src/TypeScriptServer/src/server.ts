#!/usr/bin/env node

import { Server } from '@modelcontextprotocol/sdk/server/index.js';
import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';
import {
  CallToolRequestSchema,
  ListToolsRequestSchema,
  Tool,
  ToolListChangedNotificationSchema,
} from '@modelcontextprotocol/sdk/types.js';
import { UnityClient } from './unity-client.js';
import { ToolRegistry } from './tools/tool-registry.js';
import { ToolContext } from './types/tool-types.js';
import { SERVER_CONFIG } from './constants.js';

/**
 * Unity MCP Server
 * MCP server that bridges communication between Cursor and Unity
 */
class McpServer {
  private server: Server;
  private unityClient: UnityClient;
  private toolRegistry: ToolRegistry;
  private isInitialized: boolean = false;

  constructor() {
    console.log('Starting Unity MCP Server...');
    this.server = new Server(
      {
        name: SERVER_CONFIG.NAME,
        version: SERVER_CONFIG.VERSION,
      },
      {
        capabilities: {
          tools: {
            listChanged: true
          },
        },
      }
    );

    this.unityClient = new UnityClient();
    
    // Create tool context
    const context: ToolContext = {
      unityClient: this.unityClient
    };
    
    this.toolRegistry = new ToolRegistry(context);
    
    // Set up tool change notification handler
    this.toolRegistry.onToolsChanged(() => {
      this.notifyToolsChanged();
    });
    
    this.setupHandlers();
  }

  /**
   * Initialize dynamic tools
   */
  async initialize(): Promise<void> {
    if (this.isInitialized) return;
    
    const context: ToolContext = {
      unityClient: this.unityClient
    };
    
    await this.toolRegistry.initializeDynamicTools(context);
    this.isInitialized = true;
  }

  private setupHandlers(): void {
    // Provide tool list
    this.server.setRequestHandler(ListToolsRequestSchema, async () => {
      const toolDefinitions = this.toolRegistry.getAllDefinitions();
      
      return {
        tools: toolDefinitions.map(def => ({
          name: def.name,
          description: def.description,
          inputSchema: def.inputSchema
        } as Tool))
      };
    });

    // Handle tool execution
    this.server.setRequestHandler(CallToolRequestSchema, async (request) => {
      const { name, arguments: args } = request.params;
      const result = await this.toolRegistry.execute(name, args);
      
      // Convert to format expected by MCP SDK
      return {
        content: result.content,
        isError: result.isError || false
      };
    });
  }

  /**
   * Start the server
   */
  async start(): Promise<void> {
    // Initialize dynamic tools before starting the server
    await this.initialize();
    
    const transport = new StdioServerTransport();
    await this.server.connect(transport);
    
    console.error('Unity MCP Server started successfully');
  }

  /**
   * Notify clients that the tools list has changed
   */
  private notifyToolsChanged(): void {
    if (this.isInitialized) {
      console.error('[MCP Server] Sending tools/list_changed notification');
      this.server.notification({
        method: "notifications/tools/list_changed",
        params: {}
      });
    } else {
      console.error('[MCP Server] Skipping notification - server not initialized');
    }
  }

  /**
   * Cleanup
   */
  cleanup(): void {
    this.toolRegistry.stopPolling();
    this.unityClient.disconnect();
  }
}

// Start server
const server = new McpServer();

// Cleanup on process termination
process.on('SIGINT', () => {
  server.cleanup();
  process.exit(0);
});

process.on('SIGTERM', () => {
  server.cleanup();
  process.exit(0);
});

server.start().catch((error) => {
  console.error('Failed to start Unity MCP Server:', error);
  process.exit(1);
}); 