import { z } from 'zod';
import { BaseTool } from './base-tool.js';
import { ToolResponse } from '../types/tool-types.js';

/**
 * Unity側へのPingツール
 */
export class UnityPingTool extends BaseTool {
  readonly name = 'unity.ping';
  readonly description = 'Unity側へのpingテスト（TCP/IP通信確認）';
  readonly inputSchema = {
    type: 'object',
    properties: {
      message: {
        type: 'string',
        description: 'Unity側に送信するメッセージ',
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
    // Unity側に接続
    if (!this.context.unityClient.connected) {
      await this.context.unityClient.connect();
    }

    // Unity側にpingを送信
    const response = await this.context.unityClient.ping(args.message);

    // 環境変数からポート番号を取得
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