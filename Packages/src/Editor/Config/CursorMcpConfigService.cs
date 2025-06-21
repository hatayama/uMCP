using System.Collections.Generic;
using System.Linq;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// A class responsible for the business logic of MCP settings.
    /// Single Responsibility Principle: Responsible only for the business logic of settings management.
    /// </summary>
    public class McpConfigService
    {
        private readonly McpConfigRepository _repository;
        private readonly McpEditorType _editorType;

        public McpConfigService(McpConfigRepository repository, McpEditorType editorType)
        {
            _repository = repository;
            _editorType = editorType;
        }

        /// <summary>
        /// Checks if the editor settings exist.
        /// </summary>
        public bool IsConfigured()
        {
            string configPath = UnityMcpPathResolver.GetConfigPath(_editorType);
            if (!_repository.Exists(configPath))
            {
                return false;
            }

            McpConfig config = _repository.Load(configPath);
            // Check if a setting with a port number exists.
            return config.mcpServers.Keys.Any(key => key.StartsWith("unity-mcp"));
        }

        /// <summary>
        /// Auto-configures the editor settings.
        /// </summary>
        /// <param name="port">The port number to use.</param>
        public void AutoConfigure(int port)
        {
            string configPath = UnityMcpPathResolver.GetConfigPath(_editorType);
            
            // Create the settings directory (only if necessary).
            _repository.CreateConfigDirectory(configPath);

            // Load existing settings (or create new ones if they don't exist).
            McpConfig config = _repository.Load(configPath);

            // Log for checking existing settings.
            McpLogger.LogInfo($"Loaded existing MCP servers: {string.Join(", ", config.mcpServers.Keys)}");

            // Generate a settings key that includes the port number.
            string serverKey = McpServerConfigFactory.CreateUnityMcpServerKey(port);

            // Create new settings.
            string serverPath = UnityMcpPathResolver.GetTypeScriptServerPath();
            McpServerConfigData newConfig = McpServerConfigFactory.CreateUnityMcpConfig(port, serverPath);

            // Retain existing settings and add/update new settings.
            Dictionary<string, McpServerConfigData> updatedServers = new(config.mcpServers);

            // Compare with existing settings and update if there are differences.
            bool needsUpdate = true;
            if (updatedServers.ContainsKey(serverKey))
            {
                McpServerConfigData existingConfig = updatedServers[serverKey];
                needsUpdate = !McpServerConfigComparer.AreEqual(existingConfig, newConfig);
                
                if (needsUpdate)
                {
                    McpLogger.LogInfo($"Configuration changed for {serverKey}, updating...");
                    McpLogger.LogInfo($"Old command: {existingConfig.command}");
                    McpLogger.LogInfo($"New command: {newConfig.command}");
                    McpLogger.LogInfo($"Old args: [{string.Join(", ", existingConfig.args)}]");
                    McpLogger.LogInfo($"New args: [{string.Join(", ", newConfig.args)}]");
                }
                else
                {
                    McpLogger.LogInfo($"Configuration unchanged for {serverKey}, skipping update");
                }
            }
            else
            {
                McpLogger.LogInfo($"New configuration for {serverKey}");
            }

            if (needsUpdate)
            {
                // Add/update Unity MCP settings.
                updatedServers[serverKey] = newConfig;

                McpConfig updatedConfig = new(updatedServers);
                _repository.Save(configPath, updatedConfig);

                string editorName = GetEditorDisplayName(_editorType);
                McpLogger.LogInfo($"{editorName} configuration updated: {configPath}");
                McpLogger.LogInfo($"Server key: {serverKey}, Port: {port}");
                McpLogger.LogInfo($"Final MCP servers: {string.Join(", ", updatedConfig.mcpServers.Keys)}");
            }
        }

        /// <summary>
        /// Gets the display name of the editor.
        /// </summary>
        private string GetEditorDisplayName(McpEditorType editorType)
        {
            return editorType switch
            {
                McpEditorType.Cursor => "Cursor",
                McpEditorType.ClaudeCode => "Claude Code",
                _ => editorType.ToString()
            };
        }
    }
} 