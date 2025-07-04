namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Central constants repository for Unity MCP system.
    /// 
    /// Design document reference: Packages/src/Editor/ARCHITECTURE.md
    /// 
    /// Related classes:
    /// - McpConfigService: Uses these constants for configuration management
    /// - McpServerConfigFactory: Uses port and environment variable constants
    /// - McpEditorWindow: Uses SessionState keys for UI state persistence
    /// - McpSessionManager: Uses SessionState keys for connection state management
    /// - EditorConfigProvider: Provides client names via GetClientNameForEditor method
    /// </summary>
    public static class McpConstants
    {
        public const string PROJECT_NAME = "uMCP";

        public const string MCP_DEBUG = "MCP_DEBUG";
        
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
        // MCP_CLIENT_NAME removed - now using clientInfo.name from MCP protocol
        
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
        
        // TypeScript server related constants
        public const string TYPESCRIPT_SERVER_DIR = "TypeScriptServer~";
        public const string DIST_DIR = "dist";
        public const string SERVER_BUNDLE_FILE = "server.bundle.js";
        
        // Package path constants
        public const string PACKAGES_DIR = "Packages";
        public const string SRC_DIR = "src";
        public const string LIBRARY_DIR = "Library";
        public const string PACKAGE_CACHE_DIR = "PackageCache";
        public const string PACKAGE_NAME_PATTERN = "io.github.hatayama.umcp@*";
        
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
            return EditorConfigProvider.GetClientName(editorType);
        }
    }
} 