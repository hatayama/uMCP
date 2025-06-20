/**
 * Unity MCP Server 共通定数
 * 全ファイルで使用する定数を一元管理
 */

// サーバー設定
export const SERVER_CONFIG = {
  NAME: 'unity-mcp-server',
  VERSION: '0.1.0',
} as const;

// Unity接続設定
export const UNITY_CONNECTION = {
  DEFAULT_PORT: '7400',
  DEFAULT_HOST: 'localhost',
  CONNECTION_TEST_MESSAGE: 'connection_test',
} as const;

// JSON-RPC設定
export const JSONRPC = {
  VERSION: '2.0',
} as const;

// タイムアウト設定（ミリ秒）
export const TIMEOUTS = {
  PING: 5000,
  COMPILE: 30000,
  GET_LOGS: 10000,
  RUN_TESTS: 60000,
} as const;

// ログ設定
export const LOG_CONFIG = {
  TYPES: ['Error', 'Warning', 'Log', 'All'] as const,
  DEFAULT_TYPE: 'All',
  DEFAULT_MAX_COUNT: 100,
  DEFAULT_SEARCH_TEXT: '',
  DEFAULT_INCLUDE_STACK_TRACE: true,
} as const;

// テスト設定
export const TEST_CONFIG = {
  FILTER_TYPES: ['all', 'fullclassname', 'namespace', 'testname', 'assembly'] as const,
  DEFAULT_FILTER_TYPE: 'all',
  DEFAULT_FILTER_VALUE: '',
  DEFAULT_SAVE_XML: false,
} as const;

// コンパイル設定
export const COMPILE_CONFIG = {
  DEFAULT_FORCE_RECOMPILE: false,
} as const;

// ツール名
export const TOOL_NAMES = {
  PING: 'mcp.ping',
  UNITY_PING: 'unity-ping',
  COMPILE: 'unity-compile',
  GET_LOGS: 'unity-get-logs',
  RUN_TESTS: 'unity-run-tests',
} as const;

// デフォルトメッセージ
export const DEFAULT_MESSAGES = {
  PING: 'Hello Unity MCP!',
  UNITY_PING: 'Hello from TypeScript MCP Server',
} as const;

// エラーメッセージ
export const ERROR_MESSAGES = {
  NOT_CONNECTED: 'Unity MCP Bridge is not connected',
  CONNECTION_FAILED: 'Unity connection failed',
  TIMEOUT: 'timeout',
  INVALID_RESPONSE: 'Invalid response from Unity',
} as const;