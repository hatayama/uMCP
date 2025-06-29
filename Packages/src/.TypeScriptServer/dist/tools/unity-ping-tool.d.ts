import { BaseTool } from './base-tool.js';
import { ToolResponse } from '../types/tool-types.js';
/**
 * Unity ping tool for full MCP system communication testing
 */
export declare class UnityPingTool extends BaseTool {
    readonly name: "ping";
    readonly description = "Full MCP ping test (Cursor \u2194 Node server \u2194 Unity communication)";
    readonly inputSchema: {
        type: string;
        properties: {
            message: {
                type: string;
                description: string;
                default: "Hello from TypeScript MCP Server";
            };
        };
    };
    protected validateArgs(args: unknown): {
        message: string;
    };
    protected execute(args: {
        message: string;
    }): Promise<string>;
    protected formatErrorResponse(error: unknown): ToolResponse;
}
//# sourceMappingURL=unity-ping-tool.d.ts.map