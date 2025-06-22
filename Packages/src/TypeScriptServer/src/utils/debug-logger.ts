/**
 * Safe debug logger for MCP servers
 * Never pollutes stdout (which is used for JSON-RPC communication)
 */
export class DebugLogger {
  private static isDebugEnabled = process.env.MCP_DEBUG === 'true' || process.env.NODE_ENV === 'development';
  
  /**
   * Log debug message to stderr (safe for MCP)
   */
  static debug(message: string, data?: any): void {
    if (!this.isDebugEnabled) return;
    
    const timestamp = new Date().toISOString();
    const logMessage = `[${timestamp}] [DEBUG] ${message}`;
    
    // Use console.error to output to stderr (not stdout)
    if (data !== undefined) {
      console.error(logMessage, data);
    } else {
      console.error(logMessage);
    }
  }
  
  /**
   * Log info message to stderr
   */
  static info(message: string, data?: any): void {
    if (!this.isDebugEnabled) return;
    
    const timestamp = new Date().toISOString();
    const logMessage = `[${timestamp}] [INFO] ${message}`;
    
    if (data !== undefined) {
      console.error(logMessage, data);
    } else {
      console.error(logMessage);
    }
  }
  
  /**
   * Log error message to stderr
   */
  static error(message: string, error?: any): void {
    const timestamp = new Date().toISOString();
    const logMessage = `[${timestamp}] [ERROR] ${message}`;
    
    if (error !== undefined) {
      console.error(logMessage, error);
    } else {
      console.error(logMessage);
    }
  }
  
  /**
   * Log tool execution
   */
  static logToolExecution(toolName: string, args: any, result?: any): void {
    this.debug(`Tool executed: ${toolName}`, { args, result });
  }
  
  /**
   * Log notification sending
   */
  static logNotification(method: string, params: any): void {
    this.debug(`Sending notification: ${method}`, params);
  }
  
  /**
   * Enable/disable debug logging
   */
  static setDebugEnabled(enabled: boolean): void {
    this.isDebugEnabled = enabled;
  }
} 