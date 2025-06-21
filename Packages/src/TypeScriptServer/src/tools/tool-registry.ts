import { ToolHandler, ToolContext, ToolDefinition } from '../types/tool-types.js';
import { PingTool } from './ping-tool.js';
import { UnityPingTool } from './unity-ping-tool.js';
import { CompileTool } from './compile-tool.js';
import { LogsTool } from './logs-tool.js';
import { RunTestsTool } from './run-tests-tool.js';
import { GetAvailableCommandsTool } from './get-available-commands-tool.js';
import { DynamicUnityCommandTool } from './dynamic-unity-command-tool.js';

/**
 * Tool registry
 * Responsible for tool registration, management, and execution
 */
export class ToolRegistry {
  private tools: Map<string, ToolHandler> = new Map();

  constructor(context: ToolContext) {
    console.error('=== ToolRegistry Debug: Initialization ===');
    this.registerDefaultTools(context);
    console.error(`After registerDefaultTools: ${this.tools.size} tools registered`);
    console.error(`Tool names: ${Array.from(this.tools.keys()).join(', ')}`);
    console.error('=== End ToolRegistry Debug ===');
  }

  /**
   * Initialize dynamic tools (must be called after constructor)
   */
  async initializeDynamicTools(context: ToolContext): Promise<void> {
    await this.loadDynamicTools(context);
  }

  /**
   * Register default tools
   */
  private registerDefaultTools(context: ToolContext): void {
    // Register PingTool only during development (when NODE_ENV=development or ENABLE_PING_TOOL=true)
    const isDevelopment = process.env.NODE_ENV === 'development';
    const enablePingTool = process.env.ENABLE_PING_TOOL === 'true';
    
    console.error(`=== Default Tools Registration Debug ===`);
    console.error(`isDevelopment: ${isDevelopment}, enablePingTool: ${enablePingTool}`);
    
    if (isDevelopment || enablePingTool) {
      this.register(new PingTool(context));
      console.error('Registered: PingTool');
    }
    
    this.register(new UnityPingTool(context));
    console.error('Registered: UnityPingTool');
    this.register(new CompileTool(context));
    console.error('Registered: CompileTool');
    this.register(new LogsTool(context));
    console.error('Registered: LogsTool');
    this.register(new RunTestsTool(context));
    console.error('Registered: RunTestsTool');
    this.register(new GetAvailableCommandsTool(context));
    console.error('Registered: GetAvailableCommandsTool');
    console.error(`=== End Default Tools Registration Debug ===`);
  }

  /**
   * Load dynamic tools from Unity commands
   */
  private async loadDynamicTools(context: ToolContext): Promise<void> {
    console.error('=== Dynamic Tools Loading Debug ===');
    try {
      // Wait a bit for Unity to be ready
      await new Promise(resolve => setTimeout(resolve, 1000));
      console.error('Attempting to get command details from Unity...');
      
      const commands = await context.unityClient.getCommandDetails();
      console.error(`Received ${commands.length} commands from Unity:`, commands);
      
      // Skip standard commands that are already registered as specific tools
      const standardCommands = ['ping', 'compile', 'getlogs', 'runtests', 'getversion', 'getavailablecommands'];
      
      let dynamicToolsCount = 0;
      for (const command of commands) {
        const commandName = (command as any).Name || (command as any).name;
        const commandDescription = (command as any).Description || (command as any).description;
        
        console.error(`Processing command: ${commandName}, isStandard: ${standardCommands.includes(commandName?.toLowerCase())}`);
        
        if (commandName && !standardCommands.includes(commandName.toLowerCase())) {
          const dynamicTool = new DynamicUnityCommandTool(context, commandName, commandDescription);
          this.register(dynamicTool);
          dynamicToolsCount++;
          console.error(`Registered dynamic tool: ${commandName}`);
        }
      }
      
      console.error(`Total dynamic tools registered: ${dynamicToolsCount}`);
      console.error(`Total tools after dynamic loading: ${this.tools.size}`);
      
    } catch (error) {
      console.warn('Failed to load dynamic tools:', error);
    }
    console.error('=== End Dynamic Tools Loading Debug ===');
  }

  /**
   * Register a tool
   */
  register(tool: ToolHandler): void {
    this.tools.set(tool.name, tool);
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
      !['unity-ping', 'unity-compile', 'unity-getlogs', 'unity-runtests', 'unity-get-available-commands'].includes(name));
    
    for (const toolName of toolsToRemove) {
      this.tools.delete(toolName);
    }

    // Reload dynamic tools
    await this.loadDynamicTools(context);
  }
} 