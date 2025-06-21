import { ToolHandler, ToolContext, ToolDefinition } from '../types/tool-types.js';
import { PingTool } from './ping-tool.js';
import { UnityPingTool } from './unity-ping-tool.js';
import { GetAvailableCommandsTool } from './get-available-commands-tool.js';
import { DynamicUnityCommandTool } from './dynamic-unity-command-tool.js';

/**
 * Tool registry
 * Responsible for tool registration, management, and execution
 */
export class ToolRegistry {
  private tools: Map<string, ToolHandler> = new Map();
  private toolsChangedCallback?: () => void;
  private lastKnownCommands: string[] = [];
  private pollingInterval?: NodeJS.Timeout;

  constructor(context: ToolContext) {
    this.registerDefaultTools(context);
  }

  /**
   * Initialize dynamic tools (must be called after constructor)
   */
  async initializeDynamicTools(context: ToolContext): Promise<void> {
    await this.loadDynamicTools(context);
    this.startPolling(context);
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
      // Wait a bit for Unity to be ready
      await new Promise(resolve => setTimeout(resolve, 1000));
      
      const commands = await context.unityClient.getCommandDetails();
      
      // Skip standard commands that are already registered as specific tools
      const standardCommands = ['ping', 'getavailablecommands'];
      
      for (const command of commands) {
        const commandName = (command as any).Name || (command as any).name;
        const commandDescription = (command as any).Description || (command as any).description;
        
        if (commandName && !standardCommands.includes(commandName.toLowerCase())) {
          const dynamicTool = new DynamicUnityCommandTool(context, commandName, commandDescription);
          this.register(dynamicTool);
        }
      }
      
    } catch (error) {
      console.warn('Failed to load dynamic tools:', error);
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
    // Remove existing dynamic tools
    const toolsToRemove = Array.from(this.tools.keys()).filter(name => name.startsWith('unity-') && 
      !['unity-ping', 'unity-get-available-commands'].includes(name));
    
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
   * Start polling Unity for command changes
   */
  private startPolling(context: ToolContext): void {
    this.pollingInterval = setInterval(async () => {
      try {
        await this.checkForCommandChanges(context);
      } catch (error) {
        console.warn('Error checking for command changes:', error);
      }
    }, 2000); // Poll every 2 seconds for testing
  }

  /**
   * Check if Unity commands have changed
   */
  private async checkForCommandChanges(context: ToolContext): Promise<void> {
    try {
      const commands = await context.unityClient.getCommandDetails();
      const currentCommands = commands.map((cmd: any) => 
        (cmd as any).Name || (cmd as any).name
      ).filter(Boolean).sort();

      // Compare with last known commands
      if (this.lastKnownCommands.length === 0) {
        this.lastKnownCommands = currentCommands;
        return;
      }

      const hasChanged = 
        currentCommands.length !== this.lastKnownCommands.length ||
        !currentCommands.every((cmd: string, index: number) => cmd === this.lastKnownCommands[index]);

      if (hasChanged) {
        console.error('[Tool Registry] Unity commands changed, reloading dynamic tools...');
        console.error(`[Tool Registry] Previous: [${this.lastKnownCommands.join(', ')}]`);
        console.error(`[Tool Registry] Current: [${currentCommands.join(', ')}]`);
        await this.reloadDynamicTools(context);
        this.lastKnownCommands = currentCommands;
      }
    } catch (error) {
      console.warn('Failed to check for command changes:', error);
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
} 