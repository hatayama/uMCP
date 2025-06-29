import { BaseTool } from './base-tool.js';
/**
 * MCP Ping tool for Cursor ↔ Node server communication testing
 */
export declare class PingTool extends BaseTool {
    readonly name: "mcp-ping";
    readonly description = "MCP ping test (Cursor \u2194 Node server communication only)";
    readonly inputSchema: {
        type: string;
        properties: {
            message: {
                type: string;
                description: string;
                default: "Hello Unity MCP!";
            };
        };
    };
    protected validateArgs(args: unknown): {
        message: string;
    };
    protected execute(args: {
        message: string;
    }): Promise<string>;
}
//# sourceMappingURL=ping-tool.d.ts.map