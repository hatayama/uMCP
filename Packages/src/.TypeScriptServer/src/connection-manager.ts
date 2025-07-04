import * as net from 'net';
import { POLLING } from './constants.js';
import { SafeTimer, safeSetInterval } from './utils/safe-timer.js';
import { errorToFile } from './utils/log-to-file.js';

/**
 * Manages TCP connection with reconnection polling
 * Follows Single Responsibility Principle - only handles connection state and polling
 */
export class ConnectionManager {
  private pollingTimer: SafeTimer | null = null;
  private onReconnectedCallback: (() => void) | null = null;

  /**
   * Set callback for when connection is restored
   */
  setReconnectedCallback(callback: () => void): void {
    this.onReconnectedCallback = callback;
  }

  /**
   * Start polling for connection recovery
   */
  startPolling(connectFn: () => Promise<void>): void {
    if (this.pollingTimer && this.pollingTimer.active) {
      return; // Already polling
    }

    this.pollingTimer = safeSetInterval(async () => {
      try {
        await connectFn();
        this.stopPolling();

        // Notify about reconnection
        if (this.onReconnectedCallback) {
          this.onReconnectedCallback();
        }
      } catch (error) {
        // Silent polling - don't spam logs for expected connection failures
      }
    }, POLLING.INTERVAL_MS);
  }

  /**
   * Stop polling
   */
  stopPolling(): void {
    if (this.pollingTimer) {
      this.pollingTimer.stop();
      this.pollingTimer = null;
    }
  }
}