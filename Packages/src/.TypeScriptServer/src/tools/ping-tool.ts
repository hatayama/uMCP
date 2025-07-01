import { z } from 'zod';
import { BaseTool } from './base-tool.js';
import { ToolResponse } from '../types/tool-types.js';
import { TOOL_NAMES, DEFAULT_MESSAGES } from '../constants.js';

/**
 * MCP Ping tool for Cursor ↔ Node server communication testing
 */
export class PingTool extends BaseTool {
  readonly name = TOOL_NAMES.MCP_PING;
  readonly description = 'MCP ping test (Cursor ↔ Node server communication only)';
  readonly inputSchema = {
    type: 'object',
    properties: {
      message: {
        type: 'string',
        description: 'Test message',
        default: DEFAULT_MESSAGES.PING,
      },
    },
  };

  protected validateArgs(args: unknown) {
    const schema = z.object({
      message: z.string().default(DEFAULT_MESSAGES.PING),
    });
    return schema.parse(args || {});
  }

  protected async execute(args: { message: string }): Promise<string> {
    return `Unity MCP Server is running! Message: ${args.message}`;
  }
}
