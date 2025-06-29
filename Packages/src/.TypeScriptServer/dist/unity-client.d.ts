/**
 * TCP/IP client for communication with Unity
 */
export declare class UnityClient {
    private socket;
    private _connected;
    private readonly port;
    private readonly host;
    private notificationHandlers;
    private pendingRequests;
    private reconnectHandlers;
    private pollingInterval;
    private onReconnectedCallback;
    constructor();
    get connected(): boolean;
    /**
     * Register notification handler for specific method
     */
    onNotification(method: string, handler: (params: unknown) => void): void;
    /**
     * Remove notification handler
     */
    offNotification(method: string): void;
    /**
     * Register reconnect handler
     */
    onReconnect(handler: () => void): void;
    /**
     * Remove reconnect handler
     */
    offReconnect(handler: () => void): void;
    /**
     * Handle incoming data from Unity
     */
    private handleIncomingData;
    /**
     * Handle notification from Unity
     */
    private handleNotification;
    /**
     * Handle response from Unity
     */
    private handleResponse;
    /**
     * Actually test Unity's connection status
     * Check actual communication possibility, not just socket status
     */
    testConnection(): Promise<boolean>;
    /**
     * Connect to Unity (reconnect if necessary)
     */
    ensureConnected(): Promise<void>;
    /**
     * Connect to Unity
     */
    connect(): Promise<void>;
    /**
     * Send ping to Unity
     */
    ping(message: string): Promise<unknown>;
    /**
     * Get available commands from Unity
     */
    getAvailableCommands(): Promise<string[]>;
    /**
     * Get command details from Unity
     */
    getCommandDetails(): Promise<Array<{
        name: string;
        description: string;
        parameterSchema?: any;
    }>>;
    /**
     * Execute any Unity command dynamically
     */
    executeCommand(commandName: string, params?: Record<string, unknown>): Promise<unknown>;
    /**
     * Get timeout duration for specific command
     * Now supports dynamic TimeoutSeconds parameter for all commands
     */
    private getTimeoutForCommand;
    /**
     * Generate unique request ID
     */
    private generateId;
    /**
     * Send request and wait for response
     */
    private sendRequest;
    /**
     * Disconnect
     */
    disconnect(): void;
    /**
     * Set callback for when connection is restored
     */
    setReconnectedCallback(callback: () => void): void;
    /**
     * Start polling for connection recovery
     */
    private startPolling;
    /**
     * Stop polling
     */
    private stopPolling;
}
//# sourceMappingURL=unity-client.d.ts.map