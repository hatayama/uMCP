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
import { mcpDebug, mcpInfo, mcpError, mcpWarn } from './utils/mcp-debug.js';
import { ENVIRONMENT, DEFAULT_MESSAGES } from './constants.js';
import packageJson from '../package.json' assert { type: 'json' };

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
    // Simple environment variable check
    this.isDevelopment = process.env.NODE_ENV === ENVIRONMENT.NODE_ENV_DEVELOPMENT;
    
    DebugLogger.info('Simple Unity MCP Server Starting');
    DebugLogger.info(`Environment variable: NODE_ENV=${process.env.NODE_ENV}`);
    DebugLogger.info(`Development mode: ${this.isDevelopment}`);
    
    this.server = new Server(
      {
        name: 'umcp-server',
        version: packageJson.version,
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
      mcpInfo('[Simple MCP] Fetching Unity command details with schemas...');
      await this.unityClient.ensureConnected();
      mcpInfo('[Simple MCP] Unity connection established successfully');
      
      // Get detailed command information including schemas
      const commandDetails = await this.unityClient.executeCommand('getCommandDetails', {});
      mcpInfo('[Simple MCP] Raw command details response:', commandDetails);
      
      if (!Array.isArray(commandDetails)) {
        mcpError('[Simple MCP] Invalid command details response:', commandDetails);
        return;
      }
      
      mcpInfo(`[Simple MCP] Found ${commandDetails.length} Unity commands with schemas`);
      
      // Create dynamic tools for each Unity command
      this.dynamicTools.clear();
      const toolContext = { unityClient: this.unityClient };
      
      for (const commandInfo of commandDetails) {
        const commandName = commandInfo.name;
        const description = commandInfo.description || `Execute Unity command: ${commandName}`;
        const parameterSchema = commandInfo.parameterSchema;
        
        // Skip ping command as we have a dedicated unity-ping tool
        if (commandName === 'ping') {
          continue;
        }
        
        const toolName = commandName;
        
        const dynamicTool = new DynamicUnityCommandTool(
          toolContext,
          commandName,
          description,
          parameterSchema  // スキーマ情報を渡す！
        );
        
        this.dynamicTools.set(toolName, dynamicTool);
        mcpDebug(`[Simple MCP] Created dynamic tool: ${toolName} with schema:`, parameterSchema);
      }
      
      // Extract command names for backward compatibility
      this.availableCommands = commandDetails.map(cmd => cmd.name);
      
      mcpInfo(`[Simple MCP] Initialized ${this.dynamicTools.size} dynamic Unity command tools with schemas`);
      
    } catch (error) {
      mcpError('[Simple MCP] Failed to initialize dynamic tools:', error);
      // Continue without dynamic tools
    }
  }

  /**
   * Refresh dynamic tools by re-fetching from Unity
   * This method can be called to update the tool list when Unity commands change
   */
  private async refreshDynamicTools(): Promise<void> {
    mcpInfo('[Simple MCP] Refreshing dynamic tools...');
    await this.initializeDynamicTools();
    
    // Send tools changed notification to MCP client
    this.sendToolsChangedNotification();
    mcpInfo('[Simple MCP] Dynamic tools refreshed and notification sent');
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
              default: DEFAULT_MESSAGES.UNITY_PING
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
                default: DEFAULT_MESSAGES.PING
              }
            }
          }
        });
        
        tools.push({
          name: 'get-unity-commands',
          description: 'Get Unity commands list (dev only)',
          inputSchema: {
            type: 'object',
            properties: {},
            additionalProperties: false
          }
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
      
      DebugLogger.debug(`Providing ${tools.length} tools`, { toolNames: tools.map(t => t.name) });
      mcpInfo(`[Simple MCP] Providing ${tools.length} tools to MCP client:`, tools.map(t => t.name));
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
          case 'get-unity-commands':
            if (this.isDevelopment) {
              return await this.handleGetAvailableCommands();
            }
            throw new Error('Development tool not available in production');
          default:
            // Commented out test-tool handling
            /*
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
            */
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
    const message = args?.message || DEFAULT_MESSAGES.UNITY_PING;
    
    try {
      await this.unityClient.ensureConnected();
      const response = await this.unityClient.ping(message);
      const port = process.env.UNITY_TCP_PORT || '7400';
      
      // Handle the new BaseCommandResponse format with timing info
      let responseText = '';
      if (typeof response === 'object' && response !== null) {
        const respObj = response as any;
        DebugLogger.debug('[UnityPing] Response object properties:', {
          Message: respObj.Message,
          StartedAt: respObj.StartedAt,
          EndedAt: respObj.EndedAt,
          ExecutionTimeMs: respObj.ExecutionTimeMs
        });
        
        responseText = `Message: ${respObj.Message || 'No message'}`;
        
        // Add timing information if available
        if (respObj.StartedAt && respObj.EndedAt && respObj.ExecutionTimeMs !== undefined) {
          responseText += `
Started: ${respObj.StartedAt}
Ended: ${respObj.EndedAt}
Execution Time: ${respObj.ExecutionTimeMs}ms`;
        }
      } else {
        responseText = String(response);
      }
      
      return {
        content: [
          {
            type: 'text',
            text: `Unity Ping Success!
Sent: ${message}
Response: ${responseText}
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
    const message = args?.message || DEFAULT_MESSAGES.PING;
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
      
      // Get command names
      const commandsResponse = await this.unityClient.executeCommand('getAvailableCommands', {});
      
      // Get command details with schemas
      const detailsResponse = await this.unityClient.executeCommand('getCommandDetails', {});
      
      return {
        content: [
          {
            type: 'text',
            text: `Available Unity Commands:\n${JSON.stringify(commandsResponse, null, 2)}\n\nCommand Details with Schemas:\n${JSON.stringify(detailsResponse, null, 2)}`
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
    
    // Setup Unity event notification listener BEFORE initializing tools
    this.setupUnityEventListener();
    
    // Initialize dynamic Unity command tools
    await this.initializeDynamicTools();
    
    // Send initial notification to update tool list
    this.sendToolsChangedNotification();
    
    mcpInfo('[Simple MCP] Server started with Unity event-based tool updates');
  }

  /**
   * Setup Unity event listener for automatic tool updates
   */
  private setupUnityEventListener(): void {
    // Listen for commandsChanged notifications from Unity
    this.unityClient.onNotification('commandsChanged', async (params: any) => {
      // Force console output for debugging
      console.error('[NOTIFICATION] Received commandsChanged notification from Unity:', JSON.stringify(params));
      mcpInfo('[Simple MCP] Received commandsChanged notification from Unity:', params);
      
      try {
        await this.refreshDynamicTools();
        console.error('[NOTIFICATION] Dynamic tools updated successfully via Unity event');
        mcpInfo('[Simple MCP] Dynamic tools updated successfully via Unity event');
      } catch (error) {
        console.error('[NOTIFICATION] Failed to update dynamic tools via Unity event:', error);
        mcpError('[Simple MCP] Failed to update dynamic tools via Unity event:', error);
      }
    });
    
    // Also listen for the alternative notification method
    this.unityClient.onNotification('notifications/tools/list_changed', async (params: any) => {
      // Force console output for debugging
      console.error('[NOTIFICATION] Received tools/list_changed notification from Unity:', JSON.stringify(params));
      mcpInfo('[Simple MCP] Received tools/list_changed notification from Unity:', params);
      
      try {
        await this.refreshDynamicTools();
        console.error('[NOTIFICATION] Dynamic tools updated successfully via Unity notification');
        mcpInfo('[Simple MCP] Dynamic tools updated successfully via Unity notification');
      } catch (error) {
        console.error('[NOTIFICATION] Failed to update dynamic tools via Unity notification:', error);
        mcpError('[Simple MCP] Failed to update dynamic tools via Unity notification:', error);
      }
    });
    
    console.error('[NOTIFICATION] Unity event listeners setup completed');
    mcpInfo('[Simple MCP] Unity event listeners setup completed');
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