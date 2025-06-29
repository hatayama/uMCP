/**
 * Contract: MCP Server Connection Validator
 * Ensures that MCP server can be created and configured without errors
 * Fail-fast approach: Any violation immediately throws with descriptive error
 */
export declare class McpConnectionValidator {
    private server;
    /**
     * Contract: Server must be creatable with valid configuration
     * @throws {Error} If server creation fails
     */
    createServer(): void;
    /**
     * Contract: Request handlers must be registerable without errors
     * @throws {Error} If handler registration fails
     */
    registerHandlers(): void;
    /**
     * Contract: Notifications must be sendable without errors
     * @throws {Error} If notification sending fails
     */
    testNotification(): void;
    /**
     * Contract: Server must be able to validate JSON RPC compliance
     * @throws {Error} If JSON output is invalid
     */
    validateJsonRpcCompliance(): void;
    /**
     * Run all validation tests with fail-fast approach
     * @throws {Error} On first validation failure
     */
    runAllValidations(): void;
}
export default McpConnectionValidator;
//# sourceMappingURL=mcp-connection.test.d.ts.map