import { Server } from '@modelcontextprotocol/sdk/server/index.js';
import { infoToFile, warnToFile, errorToFile, debugToFile } from './log-to-file.js';
import { NOTIFICATION_METHODS } from '../constants.js';

/**
 * Development/Debug utility for testing MCP notification system
 *
 * Purpose:
 * This class is used to test if MCP clients (like Cursor, Claude Desktop, etc.) properly
 * receive and handle the "notifications/tools/list_changed" notification. This notification
 * tells the client that the available tools have changed and they should refresh their tool list.
 *
 * When to use:
 * - Testing if your MCP client properly receives notifications
 * - Debugging tool list refresh issues in IDEs
 * - Verifying that dynamic tool registration works correctly
 * - Troubleshooting when tools don't appear in the client after Unity tool changes
 *
 * How it works:
 * 1. Simulates tool count changes by toggling between 5 and 8 tools
 * 2. Sends "notifications/tools/list_changed" at regular intervals (default: 10 seconds)
 * 3. Logs all notification attempts for debugging
 *
 * Usage example:
 * ```typescript
 * // In your server initialization:
 * const tester = new NotificationTester(server);
 * tester.startPeriodicTest(5000); // Send notification every 5 seconds
 *
 * // To stop:
 * tester.stopPeriodicTest();
 * ```
 *
 * Note: This is NOT used in production. It's purely for development/debugging purposes.
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
      warnToFile('Notification test is already running');
      return;
    }

    this.isRunning = true;
    infoToFile(`Starting periodic notification test every ${intervalMs}ms`);

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
    infoToFile('Periodic notification test stopped');
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
      timestamp: new Date().toISOString(),
    };

    debugToFile('Periodic Test: Tool Count Change', testData);

    try {
      // Send tools/list_changed notification
      void this.server.notification({
        method: NOTIFICATION_METHODS.TOOLS_LIST_CHANGED,
        params: {},
      });

      infoToFile('Sending notification: notifications/tools/list_changed', {
        simulatedToolCount: this.toolCount,
        success: true,
      });
    } catch (error) {
      errorToFile('Failed to send notification', error);
    }
  }

  /**
   * Get current test status
   */
  getStatus(): { isRunning: boolean; currentToolCount: number } {
    return {
      isRunning: this.isRunning,
      currentToolCount: this.toolCount,
    };
  }
}
