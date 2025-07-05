/**
 * Unity Debug Client用の型定義
 * Unity MCPサーバーのAPIレスポンス型を定義
 */

// コンパイル結果の型定義
export interface CompileIssue {
  file: string;
  line: number;
  column: number;
  message: string;
}

export interface CompileResult {
  Success: boolean;
  ErrorCount: number;
  WarningCount: number;
  CompletedAt: string;
  Errors?: CompileIssue[];
  Warnings?: CompileIssue[];
}

// ログエントリの型定義
export interface LogEntry {
  message: string;
  stackTrace: string;
  logType: string;
  timestamp: string;
}

export interface GetLogsResult {
  logs: LogEntry[];
  totalCount: number;
}

// テスト結果の型定義
export interface TestResult {
  success: boolean;
  testCount: number;
  passedCount: number;
  failedCount: number;
  skippedCount: number;
  executionTime: number;
  results?: any[]; // 詳細なテスト結果
}

// Pingレスポンスの型定義
export interface PingResult {
  message: string;
  timestamp: string;
}

// コマンド詳細の型定義
export interface CommandDetail {
  name: string;
  description: string;
  parameters: any[];
}

export interface GetCommandDetailsResult {
  commands: CommandDetail[];
}

// JSON-RPC関連の型定義
export interface JsonRpcRequest {
  jsonrpc: '2.0';
  id: number;
  method: string;
  params?: any;
}

export interface JsonRpcResponse<T = any> {
  jsonrpc: '2.0';
  id: number;
  result?: T;
  error?: {
    code: number;
    message: string;
    data?: any;
  };
}

export interface JsonRpcNotification {
  jsonrpc: '2.0';
  method: string;
  params?: any;
}