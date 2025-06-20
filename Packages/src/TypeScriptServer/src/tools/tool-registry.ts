import { ToolHandler, ToolContext, ToolDefinition } from '../types/tool-types.js';
import { PingTool } from './ping-tool.js';
import { UnityPingTool } from './unity-ping-tool.js';
import { CompileTool } from './compile-tool.js';
import { LogsTool } from './logs-tool.js';
import { RunTestsTool } from './run-tests-tool.js';

/**
 * Tool registry
 * Responsible for tool registration, management, and execution
 */
export class ToolRegistry {
  private tools: Map<string, ToolHandler> = new Map();

  constructor(context: ToolContext) {
    this.registerDefaultTools(context);
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
    this.register(new CompileTool(context));
    this.register(new LogsTool(context));
    this.register(new RunTestsTool(context));
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
} 