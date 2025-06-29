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
        public const string ENV_KEY_MCP_CLIENT_NAME = "MCP_CLIENT_NAME";
        
        // Environment variable values
        public const string ENV_VALUE_TRUE = "true";
        public const string ENV_VALUE_DEVELOPMENT = "development";
        
        // Client names for different editors
        public const string CLIENT_NAME_CURSOR = "Cursor";
        public const string CLIENT_NAME_CLAUDE_CODE = "Claude Code";
        public const string CLIENT_NAME_VSCODE = "VSCode";
        public const string CLIENT_NAME_GEMINI_CLI = "Gemini CLI";
        public const string CLIENT_NAME_WINDSURF = "Windsurf";
#if UMCP_DEBUG
        public const string CLIENT_NAME_MCP_INSPECTOR = "MCP Inspector";
#endif
        public const string UNKNOWN_CLIENT_NAME = "Unknown Client";
        
        // Process ID constants
        public const int UNKNOWN_PROCESS_ID = -1;
        public const int LSOF_PID_COLUMN_INDEX = 1;
        public const int LSOF_PID_ARRAY_MIN_LENGTH = 2;
        
        // System commands and arguments
        public const string LSOF_COMMAND = "lsof";
        public const string LSOF_ARGS_TEMPLATE = "-i :{0}";
        public const string LSOF_HEADER_COMMAND = "COMMAND";
        
        // Command messages
        public const string CLIENT_SUCCESS_MESSAGE_TEMPLATE = "Client name registered successfully: {0}";
        
        // Reconnection settings
        public const int RECONNECTION_TIMEOUT_SECONDS = 10;
        
        // Editor settings keys (development mode)
        public const string SETTINGS_KEY_ENABLE_DEVELOPMENT_MODE = "EnableDevelopmentMode";
        
        // SessionState keys
        public const string SESSION_KEY_SERVER_RUNNING = "uMCP.ServerRunning";
        public const string SESSION_KEY_SERVER_PORT = "uMCP.ServerPort";
        public const string SESSION_KEY_AFTER_COMPILE = "uMCP.AfterCompile";
        public const string SESSION_KEY_DOMAIN_RELOAD_IN_PROGRESS = "uMCP.DomainReloadInProgress";
        public const string SESSION_KEY_SELECTED_EDITOR_TYPE = "uMCP.SelectedEditorType";
        public const string SESSION_KEY_COMMUNICATION_LOG_HEIGHT = "uMCP.CommunicationLogHeight";
        public const string SESSION_KEY_COMMUNICATION_LOGS = "uMCP.CommunicationLogs";
        public const string SESSION_KEY_PENDING_REQUESTS = "uMCP.PendingRequests";
        public const string SESSION_KEY_RECONNECTING = "uMCP.Reconnecting";
        public const string SESSION_KEY_SHOW_RECONNECTING_UI = "uMCP.ShowReconnectingUI";
        
        /// <summary>
        /// Gets the client name for the specified editor type
        /// </summary>
        /// <param name="editorType">The editor type</param>
        /// <returns>The client name</returns>
        public static string GetClientNameForEditor(McpEditorType editorType)
        {
            return editorType switch
            {
                McpEditorType.Cursor => CLIENT_NAME_CURSOR,
                McpEditorType.ClaudeCode => CLIENT_NAME_CLAUDE_CODE,
                McpEditorType.VSCode => CLIENT_NAME_VSCODE,
                McpEditorType.GeminiCLI => CLIENT_NAME_GEMINI_CLI,
                McpEditorType.Windsurf => CLIENT_NAME_WINDSURF,
#if UMCP_DEBUG
                McpEditorType.McpInspector => CLIENT_NAME_MCP_INSPECTOR,
#endif
                _ => UNKNOWN_CLIENT_NAME
            };
        }
    }
} 