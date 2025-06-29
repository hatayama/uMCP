import { ToolHandler, ToolResponse, ToolContext } from '../types/tool-types.js';
/**
 * Base class for tools
 * Provides common processing and template method pattern
 */
export declare abstract class BaseTool implements ToolHandler {
    abstract readonly name: string;
    abstract readonly description: string;
    abstract readonly inputSchema: object;
    protected context: ToolContext;
    constructor(context: ToolContext);
    /**
     * Main method for tool execution
     */
    handle(args: unknown): Promise<ToolResponse>;
    /**
     * Argument validation (implemented in subclass)
     */
    protected abstract validateArgs(args: unknown): any;
    /**
     * Actual processing (implemented in subclass)
     */
    protected abstract execute(args: any): Promise<any>;
    /**
     * Format success response (can be overridden in subclass)
     */
    protected formatResponse(result: any): ToolResponse;
    /**
     * Format error response
     */
    protected formatErrorResponse(error: unknown): ToolResponse;
}
//# sourceMappingURL=base-tool.d.ts.map