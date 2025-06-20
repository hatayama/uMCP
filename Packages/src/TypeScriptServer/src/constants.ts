/**
 * Unity MCP Server common constants
 * Centralized management of constants used across all files
 */

// Server configuration
export const SERVER_CONFIG = {
  NAME: 'unity-mcp-server',
  VERSION: '0.1.0',
} as const;

// Unity connection configuration
export const UNITY_CONNECTION = {
  DEFAULT_PORT: '7400',
  DEFAULT_HOST: 'localhost',
  CONNECTION_TEST_MESSAGE: 'connection_test',
} as const;

// JSON-RPC configuration
export const JSONRPC = {
  VERSION: '2.0',
} as const;

// Timeout configuration (milliseconds)
export const TIMEOUTS = {
  PING: 5000,
  COMPILE: 30000,
  GET_LOGS: 10000,
  RUN_TESTS: 60000,
} as const;

// Log configuration
export const LOG_CONFIG = {
  TYPES: ['Error', 'Warning', 'Log', 'All'] as const,
  DEFAULT_TYPE: 'All',
  DEFAULT_MAX_COUNT: 100,
  DEFAULT_SEARCH_TEXT: '',
  DEFAULT_INCLUDE_STACK_TRACE: true,
} as const;

// Test configuration
export const TEST_CONFIG = {
  FILTER_TYPES: ['all', 'fullclassname', 'namespace', 'testname', 'assembly'] as const,
  DEFAULT_FILTER_TYPE: 'all',
  DEFAULT_FILTER_VALUE: '',
  DEFAULT_SAVE_XML: false,
} as const;

// Compile configuration
export const COMPILE_CONFIG = {
  DEFAULT_FORCE_RECOMPILE: false,
} as const;

// Tool names
export const TOOL_NAMES = {
  PING: 'mcp.ping',
  UNITY_PING: 'unity-ping',
  COMPILE: 'unity-compile',
  GET_LOGS: 'unity-get-logs',
  RUN_TESTS: 'unity-run-tests',
} as const;

// Default messages
export const DEFAULT_MESSAGES = {
  PING: 'Hello Unity MCP!',
  UNITY_PING: 'Hello from TypeScript MCP Server',
} as const;

// Error messages
export const ERROR_MESSAGES = {
  NOT_CONNECTED: 'Unity MCP Bridge is not connected',
  CONNECTION_FAILED: 'Unity connection failed',
  TIMEOUT: 'timeout',
  INVALID_RESPONSE: 'Invalid response from Unity',
} as const;