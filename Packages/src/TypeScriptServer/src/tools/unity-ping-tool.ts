import { z } from 'zod';
import { BaseTool } from './base-tool.js';
import { ToolResponse } from '../types/tool-types.js';

/**
 * Ping tool for Unity
 */
export class UnityPingTool extends BaseTool {
  readonly name = 'unity-ping';
  readonly description = 'Ping test to Unity (TCP/IP communication verification)';
  readonly inputSchema = {
    type: 'object',
    properties: {
      message: {
        type: 'string',
        description: 'Message to send to Unity',
        default: 'Hello from TypeScript MCP Server'
      }
    }
  };

  protected validateArgs(args: unknown) {
    const schema = z.object({
      message: z.string().default('Hello from TypeScript MCP Server')
    });
    return schema.parse(args || {});
  }

  protected async execute(args: { message: string }): Promise<string> {
    // Connect to Unity (reconnect if necessary)
    await this.context.unityClient.ensureConnected();

    const response = await this.context.unityClient.ping(args.message);
    const port = process.env.UNITY_TCP_PORT || '7400';
    
    return `Unity Ping Success!
Sent: ${args.message}
Response: ${response}
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