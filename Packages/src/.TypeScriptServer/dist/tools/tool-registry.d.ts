import { ToolHandler, ToolContext, ToolDefinition } from '../types/tool-types.js';
/**
 * Tool registry
 * Responsible for tool registration, management, and execution
 */
export declare class ToolRegistry {
    private tools;
    private toolsChangedCallback?;
    private lastKnownCommands;
    private pollingInterval?;
    private isEventListenerSetup;
    constructor(context: ToolContext);
    /**
     * Setup event listener for Unity command change notifications
     */
    private setupEventListener;
    /**
     * Setup reconnect handler for Unity client reconnections
     */
    private setupReconnectHandler;
    /**
     * Initialize dynamic tools (must be called after constructor)
     */
    initializeDynamicTools(context: ToolContext): Promise<void>;
    /**
     * Register default tools
     */
    private registerDefaultTools;
    /**
     * Load dynamic tools from Unity commands
     */
    private loadDynamicTools;
    /**
     * Register a tool
     */
    register(tool: ToolHandler): void;
    /**
     * Get a tool
     */
    get(name: string): ToolHandler | undefined;
    /**
     * Get definitions of all tools
     */
    getAllDefinitions(): ToolDefinition[];
    /**
     * Execute a tool
     */
    execute(name: string, args: unknown): Promise<import("../types/tool-types.js").ToolResponse>;
    /**
     * Get a list of registered tool names
     */
    getToolNames(): string[];
    /**
     * Reload dynamic tools from Unity
     */
    reloadDynamicTools(context: ToolContext): Promise<void>;
    /**
     * Set callback for tools changed notification
     */
    onToolsChanged(callback: () => void): void;
    /**
     * Notify that tools have changed
     */
    private notifyToolsChanged;
    /**
     * Stop polling
     */
    stopPolling(): void;
    /**
     * Start polling for Unity command changes
     */
    private startPolling;
    /**
     * Check for command changes via polling
     */
    private checkForCommandChanges;
}
//# sourceMappingURL=tool-registry.d.ts.map