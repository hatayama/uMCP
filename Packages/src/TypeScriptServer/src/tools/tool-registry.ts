import { ToolHandler, ToolContext, ToolDefinition } from '../types/tool-types.js';
import { PingTool } from './ping-tool.js';
import { UnityPingTool } from './unity-ping-tool.js';
import { GetAvailableCommandsTool } from './get-available-commands-tool.js';
import { DynamicUnityCommandTool } from './dynamic-unity-command-tool.js';
import { mcpDebug } from '../utils/log-to-file.js';

/**
 * Tool registry
 * Responsible for tool registration, management, and execution
 */
export class ToolRegistry {
  private tools: Map<string, ToolHandler> = new Map();
  private toolsChangedCallback?: () => void;
  private lastKnownCommands: string[] = [];
  private pollingInterval?: NodeJS.Timeout;
  private isEventListenerSetup: boolean = false;

  constructor(context: ToolContext) {
    this.registerDefaultTools(context);
    
    // Setup event-based updates
    this.setupEventListener(context);
    
    // Setup reconnect handler
    this.setupReconnectHandler(context);
  }

  /**
   * Setup event listener for Unity command change notifications
   */
  private setupEventListener(context: ToolContext): void {
    if (this.isEventListenerSetup) {
      return;
    }
    
    // Listen for commandsChanged notifications from Unity
    context.unityClient.onNotification('commandsChanged', async (params: any) => {
      
      try {
        await this.reloadDynamicTools(context);
      } catch (error) {
      }
    });
    
    this.isEventListenerSetup = true;
  }

  /**
   * Setup reconnect handler for Unity client reconnections
   */
  private setupReconnectHandler(context: ToolContext): void {
    context.unityClient.onReconnect(async () => {
      
      try {
        await this.reloadDynamicTools(context);
      } catch (error) {
      }
    });
    
  }

  /**
   * Initialize dynamic tools (must be called after constructor)
   */
  async initializeDynamicTools(context: ToolContext): Promise<void> {
    await this.loadDynamicTools(context);
    
    // Temporarily disable polling to avoid connection issues
    // this.startPolling(context);
  }

  /**
   * Register default tools
   */
  private registerDefaultTools(context: ToolContext): void {
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
  private async loadDynamicTools(context: ToolContext): Promise<void> {
    try {
      mcpDebug('loadDynamicTools called');
      // Wait a bit for Unity to be ready
      await new Promise(resolve => setTimeout(resolve, 1000));
      
      const commands = await context.unityClient.getCommandDetails();
      mcpDebug('Got commands from Unity:', commands.length, commands);
      
      // Skip standard commands that are already registered as specific tools
      const standardCommands = ['ping', 'getavailablecommands'];
      
      for (const command of commands) {
        const commandName = (command as any).name || (command as any).Name;
        const commandDescription = (command as any).description || (command as any).Description;
        const parameterSchema = (command as any).parameterSchema || (command as any).ParameterSchema;
        const displayDevelopmentOnly = (command as any).displayDevelopmentOnly || false;
        
        if (commandName && !standardCommands.includes(commandName.toLowerCase())) {
          // Check if this tool should only be displayed in development mode
          const isDevelopment = process.env.NODE_ENV === 'development';
          
          // Debug logging
          mcpDebug(`Command: ${commandName}, displayDevelopmentOnly: ${displayDevelopmentOnly}, NODE_ENV: ${process.env.NODE_ENV}, isDevelopment: ${isDevelopment}`);
          
          if (displayDevelopmentOnly && !isDevelopment) {
            mcpDebug(`Skipping ${commandName} in production mode`);
            continue; // Skip this tool in production mode
          }
          
          const dynamicTool = new DynamicUnityCommandTool(context, commandName, commandDescription, parameterSchema);
          this.register(dynamicTool);
          
        }
      }
      
    } catch (error) {
    }
  }

  /**
   * Register a tool
   */
  register(tool: ToolHandler): void {
    this.tools.set(tool.name, tool);
    this.notifyToolsChanged();
  }

  /**
   * Get a tool
   */
  get(name: string): ToolHandler | undefined {
    return this.tools.get(name);
  }

  /**
   * Get definitions of all tools
   */
  getAllDefinitions(): ToolDefinition[] {
    return Array.from(this.tools.values()).map(tool => ({
      name: tool.name,
      description: tool.description,
      inputSchema: tool.inputSchema
    }));
  }

  /**
   * Execute a tool
   */
  async execute(name: string, args: unknown) {
    const tool = this.get(name);
    if (!tool) {
      throw new Error(`Unknown tool: ${name}`);
    }
    return await tool.handle(args);
  }

  /**
   * Get a list of registered tool names
   */
  getToolNames(): string[] {
    return Array.from(this.tools.keys());
  }

  /**
   * Reload dynamic tools from Unity
   */
  async reloadDynamicTools(context: ToolContext): Promise<void> {
    // Remove existing dynamic tools (exclude base tools)
    const baseToolNames = ['mcp-ping', 'ping', 'get-available-commands'];
    const toolsToRemove = Array.from(this.tools.keys()).filter(name => 
      !baseToolNames.includes(name));
    
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
  onToolsChanged(callback: () => void): void {
    this.toolsChangedCallback = callback;
  }

  /**
   * Notify that tools have changed
   */
  private notifyToolsChanged(): void {
    if (this.toolsChangedCallback) {
      this.toolsChangedCallback();
    }
  }

  /**
   * Stop polling
   */
  stopPolling(): void {
    if (this.pollingInterval) {
      clearInterval(this.pollingInterval);
      this.pollingInterval = undefined;
    }
  }

  /**
   * Start polling for Unity command changes
   */
  private startPolling(context: ToolContext): void {
    if (this.pollingInterval) {
      return; // Already polling
    }
    
    const pollInterval = 5000; // Poll every 5 seconds
    
    this.pollingInterval = setInterval(async () => {
      try {
        await this.checkForCommandChanges(context);
      } catch (error) {
      }
    }, pollInterval);
  }

  /**
   * Check for command changes via polling
   */
  private async checkForCommandChanges(context: ToolContext): Promise<void> {
    try {
      const commands = await context.unityClient.getCommandDetails();
      const currentCommandNames = commands.map((cmd: any) => 
        (cmd.name || cmd.Name || '').toLowerCase()
      ).filter((name: string) => name).sort();
      
      // Compare with last known commands
      const lastCommandNames = [...this.lastKnownCommands].sort();
      const hasChanged = JSON.stringify(currentCommandNames) !== JSON.stringify(lastCommandNames);
      
      if (hasChanged) {
        
        this.lastKnownCommands = currentCommandNames;
        await this.reloadDynamicTools(context);
      }
    } catch (error) {
    }
  }
} 