/**
 * Unity MCP Server common constants
 * Centralized management of constants used across all files
 */
export declare const SERVER_CONFIG: {
    readonly NAME: "unity-mcp-server";
    readonly VERSION: "0.1.0";
};
export declare const UNITY_CONNECTION: {
    readonly DEFAULT_PORT: "7400";
    readonly DEFAULT_HOST: "localhost";
    readonly CONNECTION_TEST_MESSAGE: "connection_test";
};
export declare const JSONRPC: {
    readonly VERSION: "2.0";
};
export declare const PARAMETER_SCHEMA: {
    readonly TYPE_PROPERTY: "Type";
    readonly DESCRIPTION_PROPERTY: "Description";
    readonly DEFAULT_VALUE_PROPERTY: "DefaultValue";
    readonly ENUM_PROPERTY: "Enum";
    readonly PROPERTIES_PROPERTY: "Properties";
    readonly REQUIRED_PROPERTY: "Required";
};
export declare const TIMEOUTS: {
    readonly PING: 5000;
    readonly COMPILE: 30000;
    readonly GET_LOGS: 10000;
    readonly RUN_TESTS: 60000;
};
export declare const LOG_CONFIG: {
    readonly TYPES: readonly ["Error", "Warning", "Log", "All"];
    readonly DEFAULT_TYPE: "All";
    readonly DEFAULT_MAX_COUNT: 100;
    readonly DEFAULT_SEARCH_TEXT: "";
    readonly DEFAULT_INCLUDE_STACK_TRACE: true;
};
export declare const TEST_CONFIG: {
    readonly FILTER_TYPES: readonly ["all", "fullclassname", "namespace", "testname", "assembly"];
    readonly DEFAULT_FILTER_TYPE: "all";
    readonly DEFAULT_FILTER_VALUE: "";
    readonly DEFAULT_SAVE_XML: false;
};
export declare const COMPILE_CONFIG: {
    readonly DEFAULT_FORCE_RECOMPILE: false;
};
export declare const TOOL_NAMES: {
    readonly MCP_PING: "mcp-ping";
    readonly PING: "ping";
    readonly GET_AVAILABLE_COMMANDS: "get-available-commands";
};
export declare const DEFAULT_MESSAGES: {
    readonly PING: "Hello Unity MCP!";
    readonly UNITY_PING: "Hello from TypeScript MCP Server";
};
export declare const ENVIRONMENT: {
    readonly NODE_ENV_DEVELOPMENT: "development";
    readonly NODE_ENV_PRODUCTION: "production";
};
export declare const ERROR_MESSAGES: {
    readonly NOT_CONNECTED: "Unity MCP Bridge is not connected";
    readonly CONNECTION_FAILED: "Unity connection failed";
    readonly TIMEOUT: "timeout";
    readonly INVALID_RESPONSE: "Invalid response from Unity";
};
export declare const POLLING: {
    readonly INTERVAL_MS: 3000;
    readonly BUFFER_SECONDS: 10;
};
export declare const TEST_TIMEOUTS: {
    readonly INTEGRATION_TEST_MS: 2000;
    readonly TOOLS_LIST_TEST_MS: 3000;
    readonly JEST_DEFAULT_MS: 10000;
};
export declare const LOG_MESSAGES: {
    readonly SERVER_LOG_START_PREFIX: "=== Unity MCP Server Log Started at";
    readonly CONNECTION_RECOVERY_POLLING: "Starting connection recovery polling";
};
//# sourceMappingURL=constants.d.ts.map