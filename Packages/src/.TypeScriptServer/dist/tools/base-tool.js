/**
 * Base class for tools
 * Provides common processing and template method pattern
 */
export class BaseTool {
    context;
    constructor(context) {
        this.context = context;
    }
    /**
     * Main method for tool execution
     */
    async handle(args) {
        try {
            const validatedArgs = this.validateArgs(args);
            const result = await this.execute(validatedArgs);
            return this.formatResponse(result);
        }
        catch (error) {
            return this.formatErrorResponse(error);
        }
    }
    /**
     * Format success response (can be overridden in subclass)
     */
    formatResponse(result) {
        return {
            content: [
                {
                    type: 'text',
                    text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
                }
            ]
        };
    }
    /**
     * Format error response
     */
    formatErrorResponse(error) {
        const errorMessage = error instanceof Error ? error.message : 'Unknown error';
        return {
            content: [
                {
                    type: 'text',
                    text: `Error in ${this.name}: ${errorMessage}`
                }
            ]
        };
    }
}
//# sourceMappingURL=base-tool.js.map