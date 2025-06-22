import { Server } from '@modelcontextprotocol/sdk/server/index.js';
import { fileLogger } from './file-logger.js';

/**
 * Notification tester for debugging tools/list_changed
 */
export class NotificationTester {
  private server: Server;
  private testInterval: NodeJS.Timeout | null = null;
  private toolCount: number = 5; // Initial tool count
  private isRunning: boolean = false;

  constructor(server: Server) {
    this.server = server;
  }

  /**
   * Start periodic notification test
   */
  startPeriodicTest(intervalMs: number = 10000): void {
    if (this.isRunning) {
      fileLogger.log('WARN', 'Notification test is already running');
      return;
    }

    this.isRunning = true;
    fileLogger.log('INFO', `Starting periodic notification test every ${intervalMs}ms`);
    fileLogger.log('INFO', `Log file location: ${fileLogger.getLogFilePath()}`);

    this.testInterval = setInterval(() => {
      this.performNotificationTest();
    }, intervalMs);

    // Perform initial test
    this.performNotificationTest();
  }

  /**
   * Stop periodic test
   */
  stopPeriodicTest(): void {
    if (this.testInterval) {
      clearInterval(this.testInterval);
      this.testInterval = null;
    }
    this.isRunning = false;
    fileLogger.log('INFO', 'Periodic notification test stopped');
  }

  /**
   * Perform a single notification test
   */
  private performNotificationTest(): void {
    // Toggle tool count between 5 and 8
    this.toolCount = this.toolCount === 5 ? 8 : 5;
    
    const testData = {
      previousCount: this.toolCount === 5 ? 8 : 5,
      newCount: this.toolCount,
      timestamp: new Date().toISOString()
    };

    fileLogger.logPeriodicTest('Tool Count Change', testData);
    
    try {
      // Send tools/list_changed notification
      this.server.notification({
        method: "notifications/tools/list_changed",
        params: {}
      });
      
      fileLogger.logNotification('notifications/tools/list_changed', {
        simulatedToolCount: this.toolCount,
        success: true
      });
      
    } catch (error) {
      fileLogger.log('ERROR', 'Failed to send notification', error);
    }
  }

  /**
   * Get current test status
   */
  getStatus(): { isRunning: boolean; currentToolCount: number; logFile: string } {
    return {
      isRunning: this.isRunning,
      currentToolCount: this.toolCount,
      logFile: fileLogger.getLogFilePath()
    };
  }
} 