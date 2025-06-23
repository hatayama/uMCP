import { z } from 'zod';
import { BaseTool } from './base-tool.js';
import { ToolResponse } from '../types/tool-types.js';
import { TOOL_NAMES, DEFAULT_MESSAGES } from '../constants.js';
import { DebugLogger } from '../utils/debug-logger.js';

/**
 * Unity ping tool for full MCP system communication testing
 */
export class UnityPingTool extends BaseTool {
  readonly name = TOOL_NAMES.PING;
  readonly description = 'Full MCP ping test (Cursor ↔ Node server ↔ Unity communication)';
  readonly inputSchema = {
    type: 'object',
    properties: {
      message: {
        type: 'string',
        description: 'Message to send to Unity',
        default: DEFAULT_MESSAGES.UNITY_PING
      }
    }
  };

  protected validateArgs(args: unknown) {
    const schema = z.object({
      message: z.string().default(DEFAULT_MESSAGES.UNITY_PING)
    });
    return schema.parse(args || {});
  }

  protected async execute(args: { message: string }): Promise<string> {
    // Connect to Unity (reconnect if necessary)
    await this.context.unityClient.ensureConnected();

    const response = await this.context.unityClient.ping(args.message);
    const port = process.env.UNITY_TCP_PORT || '7400';
    
    // Debug: Output response object details
    DebugLogger.debug('[UnityPingTool] Raw response:', response);
    DebugLogger.debug('[UnityPingTool] Response type:', typeof response);
    DebugLogger.debug('[UnityPingTool] Response keys:', response ? Object.keys(response) : 'null');
    
    // Handle the new BaseCommandResponse format with timing info
    let responseText = '';
    if (typeof response === 'object' && response !== null) {
      const respObj = response as any;
      DebugLogger.debug('[UnityPingTool] Response object properties:', {
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
    
    return `Unity Ping Success!
Sent: ${args.message}
Response: ${responseText}
Connection: TCP/IP established on port ${port}`;
  }

  protected formatErrorResponse(error: unknown): ToolResponse {
    const errorMessage = error instanceof Error ? error.message : 'Unknown error';
    return {
      content: [
        {
          type: 'text',
          text: `Unity Ping Failed!
Error: ${errorMessage}

Make sure Unity MCP Bridge is running (Window > Unity MCP > Start Server)`
        }
      ]
    };
  }
} 