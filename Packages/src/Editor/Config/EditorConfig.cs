namespace io.github.hatayama.uLoopMCP
{
    /// <summary>
    /// Value object that stores editor configuration information.
    /// 
    /// Related classes:
    /// - McpEditorType: Defines supported editor types
    /// - EditorConfigProvider: Visitor pattern implementation that creates instances of this class
    /// - UnityMcpPathResolver: Path resolution logic (functionality to be migrated to this class)
    /// - McpConstants: Client name constants (functionality to be migrated to this class)
    /// </summary>
    public readonly struct EditorConfig
    {
        /// <summary>
        /// Client name of the editor (e.g., "Cursor", "Claude Code")
        /// </summary>
        public string ClientName { get; }

        /// <summary>
        /// Absolute path to the configuration file
        /// </summary>
        public string ConfigPath { get; }

        /// <summary>
        /// Absolute path to the configuration directory (null for project root)
        /// </summary>
        public string ConfigDirectory { get; }

        /// <summary>
        /// Initializes a new instance of the EditorConfig struct.
        /// </summary>
        /// <param name="clientName">Client name of the editor</param>
        /// <param name="configPath">Absolute path to the configuration file</param>
        /// <param name="configDirectory">Absolute path to the configuration directory</param>
        public EditorConfig(string clientName, string configPath, string configDirectory)
        {
            ClientName = clientName;
            ConfigPath = configPath;
            ConfigDirectory = configDirectory;
        }
    }
}