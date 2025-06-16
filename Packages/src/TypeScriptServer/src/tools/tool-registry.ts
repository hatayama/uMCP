import { ToolHandler, ToolContext, ToolDefinition } from '../types/tool-types.js';
import { PingTool } from './ping-tool.js';
import { UnityPingTool } from './unity-ping-tool.js';
import { CompileTool } from './compile-tool.js';
import { LogsTool } from './logs-tool.js';
import { RunTestsTool } from './run-tests-tool.js';

/**
 * ツールレジストリ
 * ツールの登録・管理・実行を担当
 */
export class ToolRegistry {
  private tools: Map<string, ToolHandler> = new Map();

  constructor(context: ToolContext) {
    this.registerDefaultTools(context);
  }

  /**
   * デフォルトツールの登録
   */
  private registerDefaultTools(context: ToolContext): void {
    // 開発時のみPingToolを登録（NODE_ENV=development または ENABLE_PING_TOOL=true の場合）
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
   * ツールを登録
   */
  register(tool: ToolHandler): void {
    this.tools.set(tool.name, tool);
  }

  /**
   * ツールを取得
   */
  get(name: string): ToolHandler | undefined {
    return this.tools.get(name);
  }

  /**
   * 全ツールの定義を取得
   */
  getAllDefinitions(): ToolDefinition[] {
    return Array.from(this.tools.values()).map(tool => ({
      name: tool.name,
      description: tool.description,
      inputSchema: tool.inputSchema
    }));
  }

  /**
   * ツールを実行
   */
  async execute(name: string, args: unknown) {
    const tool = this.get(name);
    if (!tool) {
      throw new Error(`Unknown tool: ${name}`);
    }
    return await tool.handle(args);
  }

  /**
   * 登録されているツール名の一覧を取得
   */
  getToolNames(): string[] {
    return Array.from(this.tools.keys());
  }
} 