/**
 * Safe debug logger for MCP servers
 * Never pollutes stdout (which is used for JSON-RPC communication)
 */
export declare class DebugLogger {
    private static isDebugEnabled;
    /**
     * Log debug message to stderr (safe for MCP)
     */
    static debug(message: string, data?: any): void;
    /**
     * Log info message to stderr
     */
    static info(message: string, data?: any): void;
    /**
     * Log error message to stderr
     */
    static error(message: string, error?: any): void;
    /**
     * Log tool execution
     */
    static logToolExecution(toolName: string, args: any, result?: any): void;
    /**
     * Log notification sending
     */
    static logNotification(method: string, params: any): void;
    /**
     * Enable/disable debug logging
     */
    static setDebugEnabled(enabled: boolean): void;
}
//# sourceMappingURL=debug-logger.d.ts.map