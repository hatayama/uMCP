namespace io.github.hatayama.uMCP
{
    public static class McpConstants
    {
        public const string PROJECT_NAME = "uMCP";
        
        // JSON configuration keys
        public const string JSON_KEY_MCP_SERVERS = "mcpServers";
        public const string JSON_KEY_COMMAND = "command";
        public const string JSON_KEY_ARGS = "args";
        public const string JSON_KEY_ENV = "env";
        
        // Editor settings
        public const string SETTINGS_FILE_NAME = "UnityMcpSettings.json";
        public const string USER_SETTINGS_FOLDER = "UserSettings";
        
        // Server configuration
        public const string NODE_COMMAND = "node";
        public const string UNITY_TCP_PORT_ENV_KEY = "UNITY_TCP_PORT";
        
        // Environment variable keys for development mode
        public const string ENV_KEY_UMCP_DEBUG = "UMCP_DEBUG";
        public const string ENV_KEY_UMCP_PRODUCTION = "UMCP_PRODUCTION";
        public const string ENV_KEY_NODE_ENV = "NODE_ENV";
        public const string ENV_KEY_MCP_DEBUG = "MCP_DEBUG";
        
        // Environment variable values
        public const string ENV_VALUE_TRUE = "true";
        public const string ENV_VALUE_DEVELOPMENT = "development";
        
        // Editor settings keys (development mode)
        public const string SETTINGS_KEY_ENABLE_DEVELOPMENT_MODE = "EnableDevelopmentMode";
        
        // SessionState keys
        public const string SESSION_KEY_SERVER_RUNNING = "uMCP.ServerRunning";
        public const string SESSION_KEY_SERVER_PORT = "uMCP.ServerPort";
        public const string SESSION_KEY_AFTER_COMPILE = "uMCP.AfterCompile";
        public const string SESSION_KEY_DOMAIN_RELOAD_IN_PROGRESS = "uMCP.DomainReloadInProgress";
        public const string SESSION_KEY_COMPILE_FROM_MCP = "uMCP.CompileFromMCP";
        public const string SESSION_KEY_SELECTED_EDITOR_TYPE = "uMCP.SelectedEditorType";
        public const string SESSION_KEY_COMMUNICATION_LOG_HEIGHT = "uMCP.CommunicationLogHeight";
        public const string SESSION_KEY_COMMUNICATION_LOGS = "uMCP.CommunicationLogs";
        public const string SESSION_KEY_PENDING_REQUESTS = "uMCP.PendingRequests";
    }
} 