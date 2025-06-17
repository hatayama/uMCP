import { z } from 'zod';
import { BaseTool } from './base-tool.js';
import { ToolResponse } from '../types/tool-types.js';

/**
 * TypeScript側のPingツール
 */
export class PingTool extends BaseTool {
  readonly name = 'mcp.ping';
  readonly description = 'Unity MCP Server接続テスト用のpingコマンド（TypeScript側のみ）';
  readonly inputSchema = {
    type: 'object',
    properties: {
      message: {
        type: 'string',
        description: 'テストメッセージ',
        default: 'Hello Unity MCP!'
      }
    }
  };

  protected validateArgs(args: unknown) {
    const schema = z.object({
      message: z.string().default('Hello Unity MCP!')
    });
    return schema.parse(args || {});
  }

  protected async execute(args: { message: string }): Promise<string> {
    return `Unity MCP Server is running! Message: ${args.message}`;
  }
} 