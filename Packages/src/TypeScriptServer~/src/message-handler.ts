import { JSONRPC } from './constants.js';
import { errorToFile, warnToFile } from './utils/log-to-file.js';

/**
 * Handles JSON-RPC message processing
 * Follows Single Responsibility Principle - only handles message parsing and routing
 *
 * Design document reference: Packages/src/Editor/ARCHITECTURE.md
 *
 * Related classes:
 * - UnityClient: Uses this class for JSON-RPC message handling
 * - UnityMcpServer: Indirectly uses via UnityClient for Unity communication
 */
export class MessageHandler {
  private notificationHandlers: Map<string, (params: unknown) => void> = new Map();
  private pendingRequests: Map<
    number,
    { resolve: (value: unknown) => void; reject: (reason: unknown) => void }
  > = new Map();

  /**
   * Register notification handler for specific method
   */
  onNotification(method: string, handler: (params: unknown) => void): void {
    this.notificationHandlers.set(method, handler);
  }

  /**
   * Remove notification handler
   */
  offNotification(method: string): void {
    this.notificationHandlers.delete(method);
  }

  /**
   * Register a pending request
   */
  registerPendingRequest(
    id: number,
    resolve: (value: unknown) => void,
    reject: (reason: unknown) => void,
  ): void {
    this.pendingRequests.set(id, { resolve, reject });
  }

  /**
   * Handle incoming data from Unity
   */
  handleIncomingData(data: string): void {
    try {
      const lines = data.split('\n').filter((line) => line.trim());

      for (const line of lines) {
        const message = JSON.parse(line);

        // Check if this is a notification (no id field)
        if (message.method && !message.hasOwnProperty('id')) {
          this.handleNotification(message);
        } else if (message.id) {
          // This is a response to a request
          this.handleResponse(message);
        }
      }
    } catch (error) {
      errorToFile('[MessageHandler] Error parsing incoming data:', error);
    }
  }

  /**
   * Handle notification from Unity
   */
  private handleNotification(notification: { method: string; params: unknown }): void {
    const { method, params } = notification;

    const handler = this.notificationHandlers.get(method);
    if (handler) {
      try {
        handler(params);
      } catch (error) {
        errorToFile(`[MessageHandler] Error in notification handler for ${method}:`, error);
      }
    }
  }

  /**
   * Handle response from Unity
   */
  private handleResponse(response: {
    id: number;
    error?: { message: string };
    result?: unknown;
  }): void {
    const { id } = response;
    const pending = this.pendingRequests.get(id);

    if (pending) {
      this.pendingRequests.delete(id);

      if (response.error) {
        pending.reject(new Error(response.error.message || 'Unknown error'));
      } else {
        pending.resolve(response);
      }
    } else {
      warnToFile(`[MessageHandler] Received response for unknown request ID: ${id}`);
    }
  }

  /**
   * Clear all pending requests (used during disconnect)
   */
  clearPendingRequests(reason: string): void {
    for (const [id, pending] of this.pendingRequests) {
      pending.reject(new Error(reason));
    }
    this.pendingRequests.clear();
  }

  /**
   * Create JSON-RPC request
   */
  createRequest(method: string, params: Record<string, unknown>, id: number): string {
    const request = {
      jsonrpc: JSONRPC.VERSION,
      id,
      method,
      params,
    };
    return JSON.stringify(request) + '\n';
  }
}
