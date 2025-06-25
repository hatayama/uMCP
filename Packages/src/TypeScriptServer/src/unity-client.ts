import * as net from 'net';
import { 
  UNITY_CONNECTION, 
  JSONRPC, 
  TIMEOUTS, 
  LOG_CONFIG, 
  TEST_CONFIG, 
  ERROR_MESSAGES,
  POLLING 
} from './constants.js';
import { mcpError } from './utils/log-to-file.js';

/**
 * TCP/IP client for communication with Unity
 */
export class UnityClient {
  private socket: net.Socket | null = null;
  private _connected: boolean = false;
  private readonly port: number;
  private readonly host: string = UNITY_CONNECTION.DEFAULT_HOST;
  private notificationHandlers: Map<string, (params: unknown) => void> = new Map();
  private pendingRequests: Map<number, { resolve: (value: unknown) => void, reject: (reason: unknown) => void }> = new Map();
  private reconnectHandlers: Set<() => void> = new Set();
  
  // Polling system
  private pollingInterval: NodeJS.Timeout | null = null;
  private onReconnectedCallback: (() => void) | null = null;

  constructor() {
    // Get port number from environment variable UNITY_TCP_PORT, default is 7400
    this.port = parseInt(process.env.UNITY_TCP_PORT || UNITY_CONNECTION.DEFAULT_PORT, 10);
  }

  get connected(): boolean {
    return this._connected && this.socket !== null && !this.socket.destroyed;
  }

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
   * Register reconnect handler
   */
  onReconnect(handler: () => void): void {
    this.reconnectHandlers.add(handler);
  }

  /**
   * Remove reconnect handler
   */
  offReconnect(handler: () => void): void {
    this.reconnectHandlers.delete(handler);
  }

  /**
   * Handle incoming data from Unity
   */
  private handleIncomingData(data: string): void {
    try {
      const lines = data.split('\n').filter(line => line.trim());
      
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
        mcpError('[UnityClient] Error parsing incoming data:', error);
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
        mcpError(`[UnityClient] Error in notification handler for ${method}:`, error);
      }
    } else {
    }
  }

  /**
   * Handle response from Unity
   */
  private handleResponse(response: { id: number; error?: { message: string }; result?: unknown }): void {
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
      mcpWarn(`[UnityClient] Received response for unknown request ID: ${id}`);
    }
  }

  /**
   * Actually test Unity's connection status
   * Check actual communication possibility, not just socket status
   */
  async testConnection(): Promise<boolean> {
    if (!this._connected || this.socket === null || this.socket.destroyed) {
      return false;
    }

    // Send a simple ping to test if actual communication is possible
    await this.ping(UNITY_CONNECTION.CONNECTION_TEST_MESSAGE);
    return true;
  }

  /**
   * Connect to Unity (reconnect if necessary)
   */
  async ensureConnected(): Promise<void> {
    // Test if already connected and actual communication is possible
    try {
      if (await this.testConnection()) {
        return;
      }
    } catch (error) {
      this._connected = false;
    }

    // Reconnect if connection is lost
    this.disconnect();
    await this.connect();
  }

  /**
   * Connect to Unity
   */
  async connect(): Promise<void> {
    return new Promise((resolve, reject) => {
      this.socket = new net.Socket();
      
      this.socket.connect(this.port, this.host, () => {
        this._connected = true;
        
        // Notify reconnect handlers
        this.reconnectHandlers.forEach(handler => {
          try {
            handler();
          } catch (error) {
            mcpError('[UnityClient] Error in reconnect handler:', error);
          }
        });
        
        resolve();
      });

      this.socket.on('error', (error) => {
        this._connected = false;
        reject(new Error(`Unity connection failed: ${error.message}`));
      });

      this.socket.on('close', () => {
        this._connected = false;
        this.startPolling();
      });

      // Handle incoming data (both notifications and responses)
      this.socket.on('data', (data) => {
        this.handleIncomingData(data.toString());
      });
    });
  }

  /**
   * Send ping to Unity
   */
  async ping(message: string): Promise<unknown> {
    if (!this.connected) {
      throw new Error('Not connected to Unity');
    }

    const request = {
      jsonrpc: JSONRPC.VERSION,
      id: this.generateId(),
      method: 'ping',
      params: {
        Message: message  // Updated to match PingSchema property name
      }
    };

    const response = await this.sendRequest(request);
    
    if (response.error) {
      throw new Error(`Unity error: ${response.error.message}`);
    }

    // Return the full response object (now includes timing information)
    return response.result || { Message: 'Unity pong' };
  }

  /**
   * Get available commands from Unity
   */
  async getAvailableCommands(): Promise<string[]> {
    await this.ensureConnected();
    
    const request = {
      jsonrpc: JSONRPC.VERSION,
      id: this.generateId(),
      method: "getAvailableCommands",
      params: {}
    };

    const response = await this.sendRequest(request);
    
    if (response.error) {
      throw new Error(`Failed to get available commands: ${response.error.message}`);
    }

    return (response.result as string[]) || [];
  }

  /**
   * Get command details from Unity
   */
  async getCommandDetails(): Promise<Array<{name: string, description: string, parameterSchema?: any}>> {
    await this.ensureConnected();
    
    const request = {
      jsonrpc: JSONRPC.VERSION,
      id: this.generateId(),
      method: "getCommandDetails",
      params: {}
    };

    const response = await this.sendRequest(request);
    
    if (response.error) {
      throw new Error(`Failed to get command details: ${response.error.message}`);
    }

    return (response.result as Array<{name: string, description: string, parameterSchema?: any}>) || [];
  }

  /**
   * Execute any Unity command dynamically
   */
  async executeCommand(commandName: string, params: Record<string, unknown> = {}): Promise<unknown> {
    await this.ensureConnected();
    
    
    const request = {
      jsonrpc: JSONRPC.VERSION,
      id: this.generateId(),
      method: commandName,
      params: params
    };


    // Determine timeout based on command type and parameters
    let timeoutMs = this.getTimeoutForCommand(commandName, params);
    
    const response = await this.sendRequest(request, timeoutMs);
    
    
    if (response.error) {
      throw new Error(`Failed to execute command '${commandName}': ${response.error.message}`);
    }

    return response.result;
  }

  /**
   * Get timeout duration for specific command
   * Now supports dynamic TimeoutSeconds parameter for all commands
   */
  private getTimeoutForCommand(commandName: string, params: Record<string, unknown>): number {
    // Check if TimeoutSeconds parameter is provided (from BaseCommandSchema)
    if (params?.TimeoutSeconds && typeof params.TimeoutSeconds === 'number' && params.TimeoutSeconds > 0) {
      // Add buffer to Unity timeout to ensure Unity timeout triggers first
      const calculatedTimeout = (params.TimeoutSeconds + POLLING.BUFFER_SECONDS) * 1000;
      return calculatedTimeout;
    }

    // Fallback to command-specific defaults
    switch (commandName) {
      case 'runtests':
        const defaultTimeout = (TIMEOUTS.RUN_TESTS / 1000 + POLLING.BUFFER_SECONDS) * 1000;
        return defaultTimeout;
      case 'compile':
        return TIMEOUTS.COMPILE;
      case 'getlogs':
        return TIMEOUTS.GET_LOGS;
      case 'ping':
        return TIMEOUTS.PING;
      default:
        return TIMEOUTS.PING;
    }
  }

  /**
   * Generate unique request ID
   */
  private generateId(): number {
    return Date.now();
  }

  /**
   * Send request and wait for response
   */
  private async sendRequest(request: { id: number; method: string; [key: string]: unknown }, timeoutMs?: number): Promise<{ id: number; error?: { message: string }; result?: unknown }> {
    return new Promise((resolve, reject) => {
      // Use provided timeout or default to PING timeout
      const timeout_duration = timeoutMs || TIMEOUTS.PING;
      

      const timeout = setTimeout(() => {
        this.pendingRequests.delete(request.id);
        reject(new Error(`Request ${ERROR_MESSAGES.TIMEOUT}`));
      }, timeout_duration);

      // Store the pending request
      this.pendingRequests.set(request.id, {
        resolve: (response) => {
          clearTimeout(timeout);
          resolve(response as { id: number; error?: { message: string }; result?: unknown });
        },
        reject: (error) => {
          clearTimeout(timeout);
          reject(error);
        }
      });

      // Send the request
      this.socket!.write(JSON.stringify(request) + '\n');
    });
  }

  /**
   * Disconnect
   */
  disconnect(): void {
    this.stopPolling(); // Stop polling when manually disconnecting
    if (this.socket) {
      this.socket.destroy();
      this.socket = null;
    }
    this._connected = false;
  }

  /**
   * Set callback for when connection is restored
   */
  setReconnectedCallback(callback: () => void): void {
    this.onReconnectedCallback = callback;
  }

  /**
   * Start polling for connection recovery
   */
  private startPolling(): void {
    if (this.pollingInterval) return; // Already polling
    
    
    this.pollingInterval = setInterval(async () => {
      try {
        await this.connect();
        this.stopPolling();
        
        // Notify about reconnection
        if (this.onReconnectedCallback) {
          this.onReconnectedCallback();
        }
      } catch (error) {
      }
    }, POLLING.INTERVAL_MS);
  }

  /**
   * Stop polling
   */
  private stopPolling(): void {
    if (this.pollingInterval) {
      clearInterval(this.pollingInterval);
      this.pollingInterval = null;
    }
  }
} 