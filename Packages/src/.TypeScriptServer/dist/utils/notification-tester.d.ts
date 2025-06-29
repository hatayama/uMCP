import { Server } from '@modelcontextprotocol/sdk/server/index.js';
/**
 * Notification tester for debugging tools/list_changed
 */
export declare class NotificationTester {
    private server;
    private testInterval;
    private toolCount;
    private isRunning;
    constructor(server: Server);
    /**
     * Start periodic notification test
     */
    startPeriodicTest(intervalMs?: number): void;
    /**
     * Stop periodic test
     */
    stopPeriodicTest(): void;
    /**
     * Perform a single notification test
     */
    private performNotificationTest;
    /**
     * Get current test status
     */
    getStatus(): {
        isRunning: boolean;
        currentToolCount: number;
    };
}
//# sourceMappingURL=notification-tester.d.ts.map