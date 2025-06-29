/**
 * Safe debug logger for MCP servers
 * Never pollutes stdout (which is used for JSON-RPC communication)
 */
export class DebugLogger {
    static isDebugEnabled = process.env.MCP_DEBUG === 'true' || process.env.NODE_ENV === 'development';
    /**
     * Log debug message to stderr (safe for MCP)
     */
    static debug(message, data) {
        if (!this.isDebugEnabled)
            return;
        const timestamp = new Date().toISOString();
        const logMessage = `[${timestamp}] [DEBUG] ${message}`;
        // Use console.error to output to stderr (not stdout)
        if (data !== undefined) {
            console.error(logMessage, data);
        }
        else {
            console.error(logMessage);
        }
    }
    /**
     * Log info message to stderr
     */
    static info(message, data) {
        if (!this.isDebugEnabled)
            return;
        const timestamp = new Date().toISOString();
        const logMessage = `[${timestamp}] [INFO] ${message}`;
        if (data !== undefined) {
            console.error(logMessage, data);
        }
        else {
            console.error(logMessage);
        }
    }
    /**
     * Log error message to stderr
     */
    static error(message, error) {
        const timestamp = new Date().toISOString();
        const logMessage = `[${timestamp}] [ERROR] ${message}`;
        if (error !== undefined) {
            console.error(logMessage, error);
        }
        else {
            console.error(logMessage);
        }
    }
    /**
     * Log tool execution
     */
    static logToolExecution(toolName, args, result) {
        this.debug(`Tool executed: ${toolName}`, { args, result });
    }
    /**
     * Log notification sending
     */
    static logNotification(method, params) {
        this.debug(`Sending notification: ${method}`, params);
    }
    /**
     * Enable/disable debug logging
     */
    static setDebugEnabled(enabled) {
        this.isDebugEnabled = enabled;
    }
}
//# sourceMappingURL=debug-logger.js.map