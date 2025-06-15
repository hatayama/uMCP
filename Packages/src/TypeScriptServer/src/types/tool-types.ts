import { z } from 'zod';

/**
 * ツールハンドラーの基底インターフェース
 */
export interface ToolHandler {
  readonly name: string;
  readonly description: string;
  readonly inputSchema: object;
  handle(args: unknown): Promise<ToolResponse>;
}

/**
 * ツールレスポンスの型（MCPサーバーの戻り値に合わせる）
 */
export interface ToolResponse {
  content: Array<{
    type: string;
    text: string;
  }>;
  isError?: boolean;
}

/**
 * ツール実行コンテキスト
 */
export interface ToolContext {
  unityClient: any; // UnityClientの型は後で定義
}

/**
 * ツール定義の型
 */
export interface ToolDefinition {
  name: string;
  description: string;
  inputSchema: object;
} 