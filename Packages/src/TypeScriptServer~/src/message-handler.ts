import { JSONRPC } from './constants.js';
import { errorToFile, warnToFile } from './utils/log-to-file.js';

// Constants for JSON-RPC error types
const JsonRpcErrorTypes = {
  SECURITY_BLOCKED: 'security_blocked',
  INTERNAL_ERROR: 'internal_error',
} as const;

// Type definitions for JSON-RPC messages
interface JsonRpcNotification {
  method: string;
  params?: unknown;
  jsonrpc?: string;
}

interface JsonRpcResponse {
  id: number;
  result?: unknown;
  error?: {
    message: string;
    data?: {
      command?: string;
      reason?: string;
      message?: string;
      type?: string;
    };
  };
  jsonrpc?: string;
}

// Type guard functions
const isJsonRpcNotification = (msg: unknown): msg is JsonRpcNotification => {
  return (
    typeof msg === 'object' &&
    msg !== null &&
    'method' in msg &&
    typeof (msg as JsonRpcNotification).method === 'string' &&
    !('id' in msg)
  );
};

const isJsonRpcResponse = (msg: unknown): msg is JsonRpcResponse => {
  return (
    typeof msg === 'object' &&
    msg !== null &&
    'id' in msg &&
    typeof (msg as JsonRpcResponse).id === 'number' &&
    !('method' in msg)
  );
};

const hasValidId = (msg: unknown): msg is { id: number } => {
  return (
    typeof msg === 'object' &&
    msg !== null &&
    'id' in msg &&
    typeof (msg as { id: number }).id === 'number'
  );
};

/**
 * Handles JSON-RPC message processing
 * Follows Single Responsibility Principle - only handles message parsing and routing
 *
 * Design document reference: Packages/src/TypeScriptServer~/ARCHITECTURE.md
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
  private partialBuffer: string = '';

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
      console.log(`[DEBUG] MessageHandler received data: ${data.length} chars`);
      
      // Append to partial buffer
      this.partialBuffer += data;
      console.log(`[DEBUG] Buffer now contains: ${this.partialBuffer.length} chars`);
      
      // Check for specific large responses
      if (this.partialBuffer.length > 5000) {
        console.log(`[DEBUG] Large buffer detected (${this.partialBuffer.length} chars), checking for resources/list...`);
        if (this.partialBuffer.includes('"resources":[')) {
          console.log(`[DEBUG] Found resources array in buffer!`);
        }
      }
      
      // Process complete messages (ending with newline)
      let processedAny = false;
      while (true) {
        const newlineIndex = this.partialBuffer.indexOf('\n');
        if (newlineIndex === -1) {
          // No complete message yet
          break;
        }
        
        const completeLine = this.partialBuffer.substring(0, newlineIndex).trim();
        this.partialBuffer = this.partialBuffer.substring(newlineIndex + 1);
        
        if (completeLine.length === 0) {
          continue;
        }
        
        console.log(`[DEBUG] Processing complete line: ${completeLine.substring(0, 100)}... (${completeLine.length} chars)`);
        
        if (completeLine.length > 5000) {
          console.log(`[DEBUG] Large line detected, contains resources: ${completeLine.includes('"resources":[')}`);
        }
        
        try {
          const message: unknown = JSON.parse(completeLine);
          processedAny = true;

          // Check if this is a notification (no id field)
          if (isJsonRpcNotification(message)) {
            console.log(`[DEBUG] Handling notification: ${(message as any).method}`);
            this.handleNotification(message);
          } else if (isJsonRpcResponse(message)) {
            // This is a response to a request
            console.log(`[DEBUG] Handling response for id: ${(message as any).id}`);
            this.handleResponse(message);
          } else if (hasValidId(message)) {
            // Fallback for other messages with valid id
            console.log(`[DEBUG] Handling fallback response for id: ${(message as any).id}`);
            this.handleResponse(message as JsonRpcResponse);
          }
        } catch (parseError) {
          console.error('[MessageHandler] Error parsing line:', parseError);
          console.error('[MessageHandler] Failed line:', completeLine.substring(0, 200));
        }
      }
      
      if (!processedAny && this.partialBuffer.length > 0) {
        console.log(`[DEBUG] Incomplete message in buffer: ${this.partialBuffer.substring(0, 100)}...`);
      }
      
    } catch (error) {
      console.error('[MessageHandler] Error in handleIncomingData:', error);
      errorToFile('[MessageHandler] Error in handleIncomingData:', error);
    }
  }

  /**
   * Handle notification from Unity
   */
  private handleNotification(notification: JsonRpcNotification): void {
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
  private handleResponse(response: JsonRpcResponse): void {
    const { id } = response;
    const pending = this.pendingRequests.get(id);

    if (pending) {
      this.pendingRequests.delete(id);

      if (response.error) {
        let errorMessage = response.error.message || 'Unknown error';

        // If security blocked, provide detailed information
        if (response.error.data?.type === JsonRpcErrorTypes.SECURITY_BLOCKED) {
          const data = response.error.data;
          errorMessage = `${data.reason || errorMessage}`;
          if (data.command) {
            errorMessage += ` (Command: ${data.command})`;
          }
          // Add instruction for enabling the feature
          errorMessage +=
            ' To use this feature, enable the corresponding option in Unity menu: Window > uLoopMCP > Security Settings';
        }

        pending.reject(new Error(errorMessage));
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
    for (const [, pending] of this.pendingRequests) {
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
