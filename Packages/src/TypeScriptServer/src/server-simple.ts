#!/usr/bin/env node

import { Server } from '@modelcontextprotocol/sdk/server/index.js';
import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';
import {
  CallToolRequestSchema,
  ListToolsRequestSchema,
  Tool,
} from '@modelcontextprotocol/sdk/types.js';
import { z } from 'zod';
import { DebugLogger } from './utils/debug-logger.js';
import { UnityClient } from './unity-client.js';
import { DynamicUnityCommandTool } from './tools/dynamic-unity-command-tool.js';
import { mcpDebug, mcpInfo, mcpError } from './utils/mcp-debug.js';

/**
 * Simple Unity MCP Server for testing notifications
 */
class SimpleMcpServer {
  private server: Server;
  private unityClient: UnityClient;
  private toolCount: number = 3;
  private isDevelopment: boolean;
  private dynamicTools: Map<string, DynamicUnityCommandTool> = new Map();
  private availableCommands: string[] = [];

  constructor() {
    // Check if development mode (temporarily forced to true for testing)
    this.isDevelopment = true; // process.env.NODE_ENV === 'development' || process.env.MCP_DEBUG === 'true';
    
    DebugLogger.info('Simple Unity MCP Server Starting');
    DebugLogger.info(`Development mode: ${this.isDevelopment}`);
    
    this.server = new Server(
      {
        name: 'unity-mcp-server-simple',
        version: '0.1.0',
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
    this.setupHandlers();
  }

  /**
   * Initialize dynamic Unity command tools
   */
  private async initializeDynamicTools(): Promise<void> {
    try {
      mcpInfo('[Simple MCP] Fetching available Unity commands...');
      await this.unityClient.ensureConnected();
      
      const response = await this.unityClient.executeCommand('getAvailableCommands', {});
      this.availableCommands = Array.isArray(response) ? response : [];
      
      mcpInfo(`[Simple MCP] Found ${this.availableCommands.length} Unity commands:`, this.availableCommands);
      
      // Create dynamic tools for each Unity command
      this.dynamicTools.clear();
      const toolContext = { unityClient: this.unityClient };
      
      for (const commandName of this.availableCommands) {
        // Skip ping command as we have a dedicated unity-ping tool
        if (commandName === 'ping') {
          continue;
        }
        
        const toolName = commandName;
        const description = `Execute Unity command: ${commandName}`;
        
        const dynamicTool = new DynamicUnityCommandTool(
          toolContext,
          commandName,
          description
        );
        
        this.dynamicTools.set(toolName, dynamicTool);
        mcpDebug(`[Simple MCP] Created dynamic tool: ${toolName}`);
      }
      
      mcpInfo(`[Simple MCP] Initialized ${this.dynamicTools.size} dynamic Unity command tools`);
      
    } catch (error) {
      mcpError('[Simple MCP] Failed to initialize dynamic tools:', error);
      // Continue without dynamic tools
    }
  }

  private setupHandlers(): void {
    // Provide tool list
    this.server.setRequestHandler(ListToolsRequestSchema, async () => {
      const tools: Tool[] = [];
      
      // Base tools (always available)
      tools.push({
        name: 'ping',
        description: 'Test Unity connection',
        inputSchema: {
          type: 'object',
          properties: {
            message: {
              type: 'string',
              description: 'Message to send to Unity',
              default: 'Hello from TypeScript MCP Server'
            }
          }
        }
      });
      
      // Dynamic Unity command tools
      for (const [toolName, dynamicTool] of this.dynamicTools) {
        tools.push({
          name: toolName,
          description: dynamicTool.description,
          inputSchema: dynamicTool.inputSchema
        });
      }
      
      // Development tools (only in development mode)
      if (this.isDevelopment) {
        tools.push({
          name: 'mcp-ping',
          description: 'TypeScript side health check (dev only)',
          inputSchema: {
            type: 'object',
            properties: {
              message: {
                type: 'string',
                description: 'Test message',
                default: 'Hello Unity MCP!'
              }
            }
          }
        });
        
        tools.push({
          name: 'get-available-commands',
          description: 'Get Unity commands list (dev only)',
          inputSchema: {
            type: 'object',
            properties: {
              random_string: {
                type: 'string',
                description: 'Dummy parameter for no-parameter tools'
              }
            },
            required: ['random_string']
          }
        });
      }
      
      // Add test tools for notification testing
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
      
      DebugLogger.debug(`Providing ${tools.length} tools`, { toolNames: tools.map(t => t.name) });
      return { tools };
    });

    // Handle tool execution
    this.server.setRequestHandler(CallToolRequestSchema, async (request) => {
      const { name, arguments: args } = request.params;
      
      DebugLogger.logToolExecution(name, args);
      
      try {
        // Check if it's a dynamic Unity command tool
        if (this.dynamicTools.has(name)) {
          const dynamicTool = this.dynamicTools.get(name)!;
          return await dynamicTool.execute(args);
        }
        
        switch (name) {
          case 'ping':
            return await this.handleUnityPing(args);
          case 'mcp-ping':
            if (this.isDevelopment) {
              return await this.handlePing(args);
            }
            throw new Error('Development tool not available in production');
          case 'get-available-commands':
            if (this.isDevelopment) {
              return await this.handleGetAvailableCommands();
            }
            throw new Error('Development tool not available in production');
          default:
            if (name.startsWith('test-tool-')) {
              return {
                content: [
                  {
                    type: 'text',
                    text: `Tool ${name} executed successfully with args: ${JSON.stringify(args)}`
                  }
                ]
              };
            }
            throw new Error(`Unknown tool: ${name}`);
        }
      } catch (error) {
        return {
          content: [
            {
              type: 'text',
              text: `Error executing ${name}: ${error instanceof Error ? error.message : 'Unknown error'}`
            }
          ],
          isError: true
        };
      }
    });
  }

  /**
   * Handle Unity ping command
   */
  private async handleUnityPing(args: any): Promise<any> {
    const message = args?.message || 'Hello from TypeScript MCP Server';
    
    try {
      await this.unityClient.ensureConnected();
      const response = await this.unityClient.ping(message);
      const port = process.env.UNITY_TCP_PORT || '7400';
      
      return {
        content: [
          {
            type: 'text',
            text: `Unity Ping Success!
Sent: ${message}
Response: ${response}
Connection: TCP/IP established on port ${port}`
          }
        ]
      };
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : 'Unknown error';
      return {
        content: [
          {
            type: 'text',
            text: `Unity Ping Failed!
Error: ${errorMessage}

Make sure Unity MCP Bridge is running (Window > Unity MCP > Start Server)`
          }
        ],
        isError: true
      };
    }
  }

  /**
   * Handle TypeScript ping command (dev only)
   */
  private async handlePing(args: any): Promise<any> {
    const message = args?.message || 'Hello Unity MCP!';
    return {
      content: [
        {
          type: 'text',
          text: `Unity MCP Server is running! Message: ${message}`
        }
      ]
    };
  }

  /**
   * Handle get available commands (dev only)
   */
  private async handleGetAvailableCommands(): Promise<any> {
    try {
      await this.unityClient.ensureConnected();
      const response = await this.unityClient.executeCommand('getAvailableCommands', {});
      
      return {
        content: [
          {
            type: 'text',
            text: `Available Unity Commands:\n${JSON.stringify(response, null, 2)}`
          }
        ]
      };
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : 'Unknown error';
      return {
        content: [
          {
            type: 'text',
            text: `Failed to get Unity commands: ${errorMessage}`
          }
        ],
        isError: true
      };
    }
  }

  /**
   * Start the server
   */
  async start(): Promise<void> {
    const transport = new StdioServerTransport();
    await this.server.connect(transport);
    
    // Initialize dynamic Unity command tools
    await this.initializeDynamicTools();
    
    // Send initial notification to update tool list
    this.sendToolsChangedNotification();
    
    // Send notifications/tools/list_changed every 5 seconds
    setInterval(() => {
      // Toggle between 5 and 8 tools
      this.toolCount = this.toolCount === 5 ? 8 : 5;
      
      this.sendToolsChangedNotification();
    }, 5000);
  }

  /**
   * Send tools changed notification
   */
  private sendToolsChangedNotification(): void {
    DebugLogger.logNotification("notifications/tools/list_changed", { toolCount: this.toolCount });
    
    try {
      this.server.notification({
        method: "notifications/tools/list_changed",
        params: {}
      });
    } catch (error) {
      DebugLogger.error('Failed to send tools changed notification', error);
    }
  }

  /**
   * Send test notification
   */
  private sendTestNotification(): void {
    // Toggle tool count between 3 and 5 (reduced for stability)
    this.toolCount = this.toolCount === 3 ? 5 : 3;
    
    try {
      this.server.notification({
        method: "notifications/tools/list_changed",
        params: {}
      });
    } catch (error) {
      // Silent error handling for MCP
    }
  }
}

// Start server
const server = new SimpleMcpServer();

server.start().catch((error) => {
  process.exit(1);
}); 