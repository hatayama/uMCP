import { z } from 'zod';
import { ToolHandler, ToolResponse, ToolContext } from '../types/tool-types.js';

/**
 * ツールの基底クラス
 * 共通処理とテンプレートメソッドパターンを提供
 */
export abstract class BaseTool implements ToolHandler {
  abstract readonly name: string;
  abstract readonly description: string;
  abstract readonly inputSchema: object;

  protected context: ToolContext;

  constructor(context: ToolContext) {
    this.context = context;
  }

  /**
   * ツール実行のメインメソッド
   */
  async handle(args: unknown): Promise<ToolResponse> {
    try {
      const validatedArgs = this.validateArgs(args);
      const result = await this.execute(validatedArgs);
      return this.formatResponse(result);
    } catch (error) {
      return this.formatErrorResponse(error);
    }
  }

  /**
   * 引数のバリデーション（サブクラスで実装）
   */
  protected abstract validateArgs(args: unknown): any;

  /**
   * 実際の処理（サブクラスで実装）
   */
  protected abstract execute(args: any): Promise<any>;

  /**
   * 成功レスポンスのフォーマット（サブクラスでオーバーライド可能）
   */
  protected formatResponse(result: any): ToolResponse {
    return {
      content: [
        {
          type: 'text',
          text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
        }
      ]
    };
  }

  /**
   * エラーレスポンスのフォーマット
   */
  protected formatErrorResponse(error: unknown): ToolResponse {
    const errorMessage = error instanceof Error ? error.message : 'Unknown error';
    return {
      content: [
        {
          type: 'text',
          text: `Error in ${this.name}: ${errorMessage}`
        }
      ]
    };
  }
} 