import * as fs from 'fs';
import * as path from 'path';
import { fileURLToPath } from 'url';
import { dirname } from 'path';

/**
 * File logger utility for debugging MCP communication
 * Supports both console.error output and file logging
 */
export class FileLogger {
  private static instance: FileLogger;
  private logFilePath: string | null = null;
  private isFileLoggingEnabled: boolean = false;
  private isConsoleLoggingEnabled: boolean = true;

  private constructor() {
    // Try to initialize file logging, but don't fail if it doesn't work
    try {
      // ES Module compatible way to get current directory
      const __filename = fileURLToPath(import.meta.url);
      const __dirname = dirname(__filename);
      const logDir = path.join(__dirname, '..', '..', 'logs');
      
      if (!fs.existsSync(logDir)) {
        fs.mkdirSync(logDir, { recursive: true });
      }
      
      const timestamp = new Date().toISOString().replace(/[:.]/g, '-');
      this.logFilePath = path.join(logDir, `mcp-server-${timestamp}.log`);
      
      // Test write to ensure file logging works
      this.writeToFile(`=== Unity MCP Server Log Started at ${new Date().toISOString()} ===\n`);
      this.isFileLoggingEnabled = true;
      
      console.error(`[FILE_LOG] File logging enabled: ${this.logFilePath}`);
    } catch (error) {
      console.error(`[FILE_LOG] File logging failed, using console only:`, error);
      this.isFileLoggingEnabled = false;
    }
  }

  static getInstance(): FileLogger {
    if (!FileLogger.instance) {
      FileLogger.instance = new FileLogger();
    }
    return FileLogger.instance;
  }

  /**
   * Log a message (both console and file if available)
   */
  log(level: 'INFO' | 'ERROR' | 'WARN' | 'DEBUG', message: string, data?: any): void {
    const timestamp = new Date().toISOString();
    
    // Always log to console
    if (this.isConsoleLoggingEnabled) {
      const consoleMessage = `[${level}] ${message}`;
      console.error(consoleMessage, data || '');
    }
    
    // Log to file if available
    if (this.isFileLoggingEnabled && this.logFilePath) {
      let logEntry = `[${timestamp}] [${level}] ${message}`;
      
      if (data !== undefined) {
        logEntry += `\nData: ${JSON.stringify(data, null, 2)}`;
      }
      
      logEntry += '\n';
      this.writeToFile(logEntry);
    }
  }

  /**
   * Log notification sending
   */
  logNotification(method: string, params: any): void {
    this.log('INFO', `Sending notification: ${method}`, params);
  }

  /**
   * Log tool changes
   */
  logToolChange(action: string, toolName?: string, toolCount?: number): void {
    const message = toolName 
      ? `Tool ${action}: ${toolName} (Total: ${toolCount})`
      : `Tools ${action} (Total: ${toolCount})`;
    this.log('INFO', message);
  }

  /**
   * Log periodic test
   */
  logPeriodicTest(testName: string, data: any): void {
    this.log('DEBUG', `Periodic Test: ${testName}`, data);
  }

  /**
   * Get log file path for reference
   */
  getLogFilePath(): string | null {
    return this.logFilePath;
  }

  /**
   * Enable/disable console logging
   */
  setConsoleLogging(enabled: boolean): void {
    this.isConsoleLoggingEnabled = enabled;
  }

  /**
   * Enable/disable file logging
   */
  setFileLogging(enabled: boolean): void {
    this.isFileLoggingEnabled = enabled && this.logFilePath !== null;
  }

  private writeToFile(content: string): void {
    if (!this.logFilePath) return;
    
    try {
      fs.appendFileSync(this.logFilePath, content);
    } catch (error) {
      console.error('Failed to write to log file:', error);
    }
  }
}

// Export singleton instance
export const fileLogger = FileLogger.getInstance(); 