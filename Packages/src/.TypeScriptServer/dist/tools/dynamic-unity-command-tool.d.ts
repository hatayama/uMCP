import { BaseTool } from './base-tool.js';
import { ToolContext } from '../types/tool-types.js';
/**
 * Dynamically generated tool for Unity commands
 */
export declare class DynamicUnityCommandTool extends BaseTool {
    readonly name: string;
    readonly description: string;
    readonly inputSchema: any;
    private readonly commandName;
    constructor(context: ToolContext, commandName: string, description: string, parameterSchema?: any);
    private generateInputSchema;
    private convertType;
    validateArgs(args: unknown): any;
    execute(args: unknown): Promise<any>;
    protected formatErrorResponse(error: unknown): {
        content: {
            type: string;
            text: string;
        }[];
    };
}
//# sourceMappingURL=dynamic-unity-command-tool.d.ts.map