import { BaseTool } from './base-tool.js';
/**
 * Tool to get available Unity commands
 */
export declare class GetAvailableCommandsTool extends BaseTool {
    name: "get-available-commands";
    description: string;
    inputSchema: {
        type: string;
        properties: {};
        additionalProperties: boolean;
    };
    validateArgs(args: unknown): {};
    execute(args: unknown): Promise<{
        content: {
            type: string;
            text: string;
        }[];
    }>;
    formatErrorResponse(error: unknown): {
        content: {
            type: string;
            text: string;
        }[];
    };
}
//# sourceMappingURL=get-available-commands-tool.d.ts.map