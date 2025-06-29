import * as net from 'net';
import { UNITY_CONNECTION, JSONRPC, TIMEOUTS, ERROR_MESSAGES, POLLING } from './constants.js';
import { mcpDebug, mcpInfo, mcpWarn, mcpError } from './utils/mcp-debug.js';
/**
 * TCP/IP client for communication with Unity
 */
export class UnityClient {
    socket = null;
    _connected = false;
    port;
    host = UNITY_CONNECTION.DEFAULT_HOST;
    notificationHandlers = new Map();
    pendingRequests = new Map();
    reconnectHandlers = new Set();
    // Polling system
    pollingInterval = null;
    onReconnectedCallback = null;
    constructor() {
        // Get port number from environment variable UNITY_TCP_PORT, default is 7400
        this.port = parseInt(process.env.UNITY_TCP_PORT || UNITY_CONNECTION.DEFAULT_PORT, 10);
    }
    get connected() {
        return this._connected && this.socket !== null && !this.socket.destroyed;
    }
    /**
     * Register notification handler for specific method
     */
    onNotification(method, handler) {
        this.notificationHandlers.set(method, handler);
        mcpDebug(`[UnityClient] Registered notification handler for method: ${method}`);
    }
    /**
     * Remove notification handler
     */
    offNotification(method) {
        this.notificationHandlers.delete(method);
        // console.log(`[UnityClient] Removed notification handler for method: ${method}`);
    }
    /**
     * Register reconnect handler
     */
    onReconnect(handler) {
        this.reconnectHandlers.add(handler);
        // console.log(`[UnityClient] Registered reconnect handler`);
    }
    /**
     * Remove reconnect handler
     */
    offReconnect(handler) {
        this.reconnectHandlers.delete(handler);
        // console.log(`[UnityClient] Removed reconnect handler`);
    }
    /**
     * Handle incoming data from Unity
     */
    handleIncomingData(data) {
        try {
            const lines = data.split('\n').filter(line => line.trim());
            for (const line of lines) {
                const message = JSON.parse(line);
                // Check if this is a notification (no id field)
                if (message.method && !message.hasOwnProperty('id')) {
                    this.handleNotification(message);
                }
                else if (message.id) {
                    // This is a response to a request
                    this.handleResponse(message);
                }
            }
        }
        catch (error) {
            mcpError('[UnityClient] Error parsing incoming data:', error);
        }
    }
    /**
     * Handle notification from Unity
     */
    handleNotification(notification) {
        const { method, params } = notification;
        mcpDebug(`[UnityClient] Received notification: ${method}`, params);
        const handler = this.notificationHandlers.get(method);
        if (handler) {
            try {
                handler(params);
            }
            catch (error) {
                mcpError(`[UnityClient] Error in notification handler for ${method}:`, error);
            }
        }
        else {
            mcpDebug(`[UnityClient] No handler registered for notification: ${method}`);
        }
    }
    /**
     * Handle response from Unity
     */
    handleResponse(response) {
        const { id } = response;
        const pending = this.pendingRequests.get(id);
        if (pending) {
            this.pendingRequests.delete(id);
            if (response.error) {
                pending.reject(new Error(response.error.message || 'Unknown error'));
            }
            else {
                pending.resolve(response);
            }
        }
        else {
            mcpWarn(`[UnityClient] Received response for unknown request ID: ${id}`);
        }
    }
    /**
     * Actually test Unity's connection status
     * Check actual communication possibility, not just socket status
     */
    async testConnection() {
        if (!this._connected || this.socket === null || this.socket.destroyed) {
            mcpWarn('[UnityClient] Connection test failed: socket not connected or destroyed');
            return false;
        }
        // Send a simple ping to test if actual communication is possible
        await this.ping(UNITY_CONNECTION.CONNECTION_TEST_MESSAGE);
        return true;
    }
    /**
     * Connect to Unity (reconnect if necessary)
     */
    async ensureConnected() {
        // Test if already connected and actual communication is possible
        try {
            if (await this.testConnection()) {
                return;
            }
        }
        catch (error) {
            mcpWarn('[UnityClient] Connection test failed during ensureConnected:', error);
            this._connected = false;
        }
        // Reconnect if connection is lost
        this.disconnect();
        await this.connect();
    }
    /**
     * Connect to Unity
     */
    async connect() {
        return new Promise((resolve, reject) => {
            this.socket = new net.Socket();
            this.socket.connect(this.port, this.host, () => {
                this._connected = true;
                // Notify reconnect handlers
                this.reconnectHandlers.forEach(handler => {
                    try {
                        handler();
                    }
                    catch (error) {
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
                mcpInfo('[UnityClient] Connection lost, starting polling...');
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
    async ping(message) {
        if (!this.connected) {
            throw new Error('Not connected to Unity');
        }
        const request = {
            jsonrpc: JSONRPC.VERSION,
            id: this.generateId(),
            method: 'ping',
            params: {
                Message: message // Updated to match PingSchema property name
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
    async getAvailableCommands() {
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
        return response.result || [];
    }
    /**
     * Get command details from Unity
     */
    async getCommandDetails() {
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
        return response.result || [];
    }
    /**
     * Execute any Unity command dynamically
     */
    async executeCommand(commandName, params = {}) {
        await this.ensureConnected();
        // Add detailed logging for debugging
        mcpDebug(`[UnityClient] Executing command: "${commandName}" with params:`, params);
        const request = {
            jsonrpc: JSONRPC.VERSION,
            id: this.generateId(),
            method: commandName,
            params: params
        };
        mcpDebug(`[UnityClient] Sending request:`, request);
        // Determine timeout based on command type and parameters
        let timeoutMs = this.getTimeoutForCommand(commandName, params);
        const response = await this.sendRequest(request, timeoutMs);
        mcpDebug(`[UnityClient] Received response:`, response);
        if (response.error) {
            throw new Error(`Failed to execute command '${commandName}': ${response.error.message}`);
        }
        return response.result;
    }
    /**
     * Get timeout duration for specific command
     * Now supports dynamic TimeoutSeconds parameter for all commands
     */
    getTimeoutForCommand(commandName, params) {
        // Check if TimeoutSeconds parameter is provided (from BaseCommandSchema)
        if (params?.TimeoutSeconds && typeof params.TimeoutSeconds === 'number' && params.TimeoutSeconds > 0) {
            // Add buffer to Unity timeout to ensure Unity timeout triggers first
            const calculatedTimeout = (params.TimeoutSeconds + POLLING.BUFFER_SECONDS) * 1000;
            mcpDebug(`[UnityClient] Using dynamic timeout for ${commandName}: params.TimeoutSeconds=${params.TimeoutSeconds}s, final=${calculatedTimeout}ms`);
            return calculatedTimeout;
        }
        // Fallback to command-specific defaults
        switch (commandName) {
            case 'runtests':
                const defaultTimeout = (TIMEOUTS.RUN_TESTS / 1000 + POLLING.BUFFER_SECONDS) * 1000;
                mcpDebug(`[UnityClient] Using default timeout for runtests: ${defaultTimeout}ms`);
                return defaultTimeout;
            case 'compile':
                mcpDebug(`[UnityClient] Using default timeout for compile: ${TIMEOUTS.COMPILE}ms`);
                return TIMEOUTS.COMPILE;
            case 'getlogs':
                mcpDebug(`[UnityClient] Using default timeout for getlogs: ${TIMEOUTS.GET_LOGS}ms`);
                return TIMEOUTS.GET_LOGS;
            case 'ping':
                mcpDebug(`[UnityClient] Using default timeout for ping: ${TIMEOUTS.PING}ms`);
                return TIMEOUTS.PING;
            default:
                mcpDebug(`[UnityClient] Using default timeout for ${commandName}: ${TIMEOUTS.PING}ms`);
                return TIMEOUTS.PING;
        }
    }
    /**
     * Generate unique request ID
     */
    generateId() {
        return Date.now();
    }
    /**
     * Send request and wait for response
     */
    async sendRequest(request, timeoutMs) {
        return new Promise((resolve, reject) => {
            // Use provided timeout or default to PING timeout
            const timeout_duration = timeoutMs || TIMEOUTS.PING;
            mcpDebug(`[UnityClient] Setting timeout for request ${request.id} (${request.method}): ${timeout_duration}ms`);
            const timeout = setTimeout(() => {
                this.pendingRequests.delete(request.id);
                mcpDebug(`[UnityClient] Request ${request.id} (${request.method}) timed out after ${timeout_duration}ms`);
                reject(new Error(`Request ${ERROR_MESSAGES.TIMEOUT}`));
            }, timeout_duration);
            // Store the pending request
            this.pendingRequests.set(request.id, {
                resolve: (response) => {
                    clearTimeout(timeout);
                    resolve(response);
                },
                reject: (error) => {
                    clearTimeout(timeout);
                    reject(error);
                }
            });
            // Send the request
            this.socket.write(JSON.stringify(request) + '\n');
        });
    }
    /**
     * Disconnect
     */
    disconnect() {
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
    setReconnectedCallback(callback) {
        this.onReconnectedCallback = callback;
    }
    /**
     * Start polling for connection recovery
     */
    startPolling() {
        if (this.pollingInterval)
            return; // Already polling
        mcpInfo(`[UnityClient] Starting connection recovery polling (${POLLING.INTERVAL_MS}ms interval)`);
        this.pollingInterval = setInterval(async () => {
            try {
                await this.connect();
                mcpInfo('[UnityClient] Connection recovered! Stopping polling');
                this.stopPolling();
                // Notify about reconnection
                if (this.onReconnectedCallback) {
                    this.onReconnectedCallback();
                }
            }
            catch (error) {
                mcpDebug(`[UnityClient] Polling retry failed (connection still down):`, error);
            }
        }, POLLING.INTERVAL_MS);
    }
    /**
     * Stop polling
     */
    stopPolling() {
        if (this.pollingInterval) {
            clearInterval(this.pollingInterval);
            this.pollingInterval = null;
        }
    }
}
//# sourceMappingURL=unity-client.js.map