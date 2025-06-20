import { z } from 'zod';
import { BaseTool } from './base-tool.js';
import { ToolResponse } from '../types/tool-types.js';
import { TOOL_NAMES, DEFAULT_MESSAGES } from '../constants.js';

/**
 * TypeScript側のPingツール
 */
export class PingTool extends BaseTool {
  readonly name = TOOL_NAMES.PING;
  readonly description = 'Unity MCP Server接続テスト用のpingコマンド（TypeScript側のみ）';
  readonly inputSchema = {
    type: 'object',
    properties: {
      message: {
        type: 'string',
        description: 'テストメッセージ',
        default: DEFAULT_MESSAGES.PING
      }
    }
  };

  protected validateArgs(args: unknown) {
    const schema = z.object({
      message: z.string().default(DEFAULT_MESSAGES.PING)
    });
    return schema.parse(args || {});
  }

  protected async execute(args: { message: string }): Promise<string> {
    return `Unity MCP Server is running! Message: ${args.message}`;
  }
} 