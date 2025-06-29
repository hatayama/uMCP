import { PingTool } from './ping-tool.js';
import { UnityPingTool } from './unity-ping-tool.js';
import { GetAvailableCommandsTool } from './get-available-commands-tool.js';
import { DynamicUnityCommandTool } from './dynamic-unity-command-tool.js';
/**
 * Tool registry
 * Responsible for tool registration, management, and execution
 */
export class ToolRegistry {
    tools = new Map();
    toolsChangedCallback;
    lastKnownCommands = [];
    pollingInterval;
    isEventListenerSetup = false;
    constructor(context) {
        this.registerDefaultTools(context);
        // Setup event-based updates
        this.setupEventListener(context);
        // Setup reconnect handler
        this.setupReconnectHandler(context);
    }
    /**
     * Setup event listener for Unity command change notifications
     */
    setupEventListener(context) {
        if (this.isEventListenerSetup) {
            return;
        }
        // Listen for commandsChanged notifications from Unity
        context.unityClient.onNotification('commandsChanged', async (params) => {
            console.log('[Tool Registry] Received commandsChanged notification from Unity:', params);
            try {
                await this.reloadDynamicTools(context);
                console.log('[Tool Registry] Dynamic tools updated successfully via event notification');
            }
            catch (error) {
                console.error('[Tool Registry] Failed to update dynamic tools via event:', error);
            }
        });
        this.isEventListenerSetup = true;
        console.log('[Tool Registry] Event listener setup completed');
    }
    /**
     * Setup reconnect handler for Unity client reconnections
     */
    setupReconnectHandler(context) {
        context.unityClient.onReconnect(async () => {
            console.log('[Tool Registry] Unity client reconnected, reloading dynamic tools...');
            try {
                await this.reloadDynamicTools(context);
                console.log('[Tool Registry] Dynamic tools reloaded successfully after reconnection');
            }
            catch (error) {
                console.error('[Tool Registry] Failed to reload dynamic tools after reconnection:', error);
            }
        });
        console.log('[Tool Registry] Reconnect handler setup completed');
    }
    /**
     * Initialize dynamic tools (must be called after constructor)
     */
    async initializeDynamicTools(context) {
        await this.loadDynamicTools(context);
        // Temporarily disable polling to avoid connection issues
        // this.startPolling(context);
        console.log('[Tool Registry] Dynamic tools initialized with event-based updates from Unity');
    }
    /**
     * Register default tools
     */
    registerDefaultTools(context) {
        // Register PingTool only during development (when NODE_ENV=development or ENABLE_PING_TOOL=true)
        const isDevelopment = process.env.NODE_ENV === 'development';
        const enablePingTool = process.env.ENABLE_PING_TOOL === 'true';
        if (isDevelopment || enablePingTool) {
            this.register(new PingTool(context));
        }
        this.register(new UnityPingTool(context));
        this.register(new GetAvailableCommandsTool(context));
    }
    /**
     * Load dynamic tools from Unity commands
     */
    async loadDynamicTools(context) {
        try {
            // Wait a bit for Unity to be ready
            await new Promise(resolve => setTimeout(resolve, 1000));
            const commands = await context.unityClient.getCommandDetails();
            // Skip standard commands that are already registered as specific tools
            const standardCommands = ['ping', 'getavailablecommands'];
            for (const command of commands) {
                const commandName = command.name || command.Name;
                const commandDescription = command.description || command.Description;
                const parameterSchema = command.parameterSchema || command.ParameterSchema;
                if (commandName && !standardCommands.includes(commandName.toLowerCase())) {
                    const dynamicTool = new DynamicUnityCommandTool(context, commandName, commandDescription, parameterSchema);
                    this.register(dynamicTool);
                    console.log(`[Tool Registry] Registered dynamic tool: ${commandName} with schema:`, JSON.stringify(parameterSchema, null, 2));
                }
            }
        }
        catch (error) {
            console.warn('Failed to load dynamic tools:', error);
        }
    }
    /**
     * Register a tool
     */
    register(tool) {
        this.tools.set(tool.name, tool);
        this.notifyToolsChanged();
    }
    /**
     * Get a tool
     */
    get(name) {
        return this.tools.get(name);
    }
    /**
     * Get definitions of all tools
     */
    getAllDefinitions() {
        return Array.from(this.tools.values()).map(tool => ({
            name: tool.name,
            description: tool.description,
            inputSchema: tool.inputSchema
        }));
    }
    /**
     * Execute a tool
     */
    async execute(name, args) {
        const tool = this.get(name);
        if (!tool) {
            throw new Error(`Unknown tool: ${name}`);
        }
        return await tool.handle(args);
    }
    /**
     * Get a list of registered tool names
     */
    getToolNames() {
        return Array.from(this.tools.keys());
    }
    /**
     * Reload dynamic tools from Unity
     */
    async reloadDynamicTools(context) {
        // Remove existing dynamic tools (exclude base tools)
        const baseToolNames = ['mcp-ping', 'ping', 'get-available-commands'];
        const toolsToRemove = Array.from(this.tools.keys()).filter(name => !baseToolNames.includes(name));
        for (const toolName of toolsToRemove) {
            this.tools.delete(toolName);
        }
        // Reload dynamic tools
        await this.loadDynamicTools(context);
        this.notifyToolsChanged();
    }
    /**
     * Set callback for tools changed notification
     */
    onToolsChanged(callback) {
        this.toolsChangedCallback = callback;
    }
    /**
     * Notify that tools have changed
     */
    notifyToolsChanged() {
        if (this.toolsChangedCallback) {
            this.toolsChangedCallback();
        }
    }
    /**
     * Stop polling
     */
    stopPolling() {
        if (this.pollingInterval) {
            clearInterval(this.pollingInterval);
            this.pollingInterval = undefined;
        }
    }
    /**
     * Start polling for Unity command changes
     */
    startPolling(context) {
        if (this.pollingInterval) {
            return; // Already polling
        }
        const pollInterval = 5000; // Poll every 5 seconds
        console.log(`[Tool Registry] Starting polling for Unity commands every ${pollInterval}ms`);
        this.pollingInterval = setInterval(async () => {
            try {
                await this.checkForCommandChanges(context);
            }
            catch (error) {
                console.warn('[Tool Registry] Error during polling:', error);
            }
        }, pollInterval);
    }
    /**
     * Check for command changes via polling
     */
    async checkForCommandChanges(context) {
        try {
            const commands = await context.unityClient.getCommandDetails();
            const currentCommandNames = commands.map((cmd) => (cmd.name || cmd.Name || '').toLowerCase()).filter((name) => name).sort();
            // Compare with last known commands
            const lastCommandNames = [...this.lastKnownCommands].sort();
            const hasChanged = JSON.stringify(currentCommandNames) !== JSON.stringify(lastCommandNames);
            if (hasChanged) {
                console.log('[Tool Registry] Command changes detected via polling');
                console.log('[Tool Registry] Previous commands:', lastCommandNames);
                console.log('[Tool Registry] Current commands:', currentCommandNames);
                this.lastKnownCommands = currentCommandNames;
                await this.reloadDynamicTools(context);
                console.log('[Tool Registry] Dynamic tools updated successfully via polling');
            }
        }
        catch (error) {
            console.warn('[Tool Registry] Failed to check for command changes:', error);
        }
    }
}
//# sourceMappingURL=tool-registry.js.map