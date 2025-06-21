#!/usr/bin/env node

import { Server } from '@modelcontextprotocol/sdk/server/index.js';
import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';
import {
  CallToolRequestSchema,
  ListToolsRequestSchema,
  Tool,
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

  constructor() {
    console.log('🚀 Unity MCP Server initializing...');
    this.server = new Server(
      {
        name: SERVER_CONFIG.NAME,
        version: SERVER_CONFIG.VERSION,
      },
      {
        capabilities: {
          tools: {},
        },
      }
    );

    this.unityClient = new UnityClient();
    
    // Create tool context
    const context: ToolContext = {
      unityClient: this.unityClient
    };
    
    this.toolRegistry = new ToolRegistry(context);
    this.setupHandlers();
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
    const transport = new StdioServerTransport();
    await this.server.connect(transport);
    
    console.error('Unity MCP Server started successfully');
  }

  /**
   * Cleanup
   */
  cleanup(): void {
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