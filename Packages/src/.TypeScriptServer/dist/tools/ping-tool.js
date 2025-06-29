import { z } from 'zod';
import { BaseTool } from './base-tool.js';
import { TOOL_NAMES, DEFAULT_MESSAGES } from '../constants.js';
/**
 * MCP Ping tool for Cursor ↔ Node server communication testing
 */
export class PingTool extends BaseTool {
    name = TOOL_NAMES.MCP_PING;
    description = 'MCP ping test (Cursor ↔ Node server communication only)';
    inputSchema = {
        type: 'object',
        properties: {
            message: {
                type: 'string',
                description: 'Test message',
                default: DEFAULT_MESSAGES.PING
            }
        }
    };
    validateArgs(args) {
        const schema = z.object({
            message: z.string().default(DEFAULT_MESSAGES.PING)
        });
        return schema.parse(args || {});
    }
    async execute(args) {
        return `Unity MCP Server is running! Message: ${args.message}`;
    }
}
//# sourceMappingURL=ping-tool.js.map