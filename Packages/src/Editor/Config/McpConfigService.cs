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
            try
            {
                string configPath = UnityMcpPathResolver.GetConfigPath(_editorType);
                if (!_repository.Exists(configPath))
                {
                    return false;
                }

                McpConfig config = _repository.Load(configPath);
                // Check if a setting with a port number exists.
                return config.mcpServers.Keys.Any(key => key.StartsWith(McpConstants.PROJECT_NAME));
            }
            catch (System.Exception ex)
            {
                McpLogger.LogError($"Error checking configuration: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Auto-configures the editor settings.
        /// </summary>
        /// <param name="port">The port number to use.</param>
        public void AutoConfigure(int port)
        {
            // Validate configuration parameters before proceeding
            ValidateConfigurationParameters(port);
            
            string configPath = UnityMcpPathResolver.GetConfigPath(_editorType);
            
            // Create the settings directory (only if necessary).
            _repository.CreateConfigDirectory(configPath);

            // Load existing settings (or create new ones if they don't exist).
            McpConfig config = _repository.Load(configPath);


            // Generate a settings key that includes the port number.
            string serverKey = McpServerConfigFactory.CreateUnityMcpServerKey(port);

            // Create new settings.
            string serverPath = UnityMcpPathResolver.GetTypeScriptServerPath();
            McpServerConfigData newConfig = McpServerConfigFactory.CreateUnityMcpConfig(port, serverPath, _editorType);

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
                }
            }

            if (needsUpdate)
            {
                // Add/update Unity MCP settings.
                updatedServers[serverKey] = newConfig;

                McpConfig updatedConfig = new(updatedServers);
                _repository.Save(configPath, updatedConfig);

                string editorName = GetEditorDisplayName(_editorType);
                McpLogger.LogInfo($"{editorName} configuration updated: {configPath}");
            }
        }

        /// <summary>
        /// Updates environment variables for development mode and MCP debug logs.
        /// </summary>
        /// <param name="port">The port number to use.</param>
        /// <param name="developmentMode">Whether to enable development mode.</param>
        /// <param name="enableMcpLogs">Whether to enable MCP debug logs.</param>
        public void UpdateDevelopmentSettings(int port, bool developmentMode, bool enableMcpLogs)
        {
            string configPath = UnityMcpPathResolver.GetConfigPath(_editorType);
            
            // Create the settings directory (only if necessary)
            _repository.CreateConfigDirectory(configPath);

            // Load existing settings (or create new ones if they don't exist)
            McpConfig config = _repository.Load(configPath);

            // Generate settings key that includes the port number
            string serverKey = McpServerConfigFactory.CreateUnityMcpServerKey(port);

            // Check if our server configuration exists
            if (!config.mcpServers.ContainsKey(serverKey))
            {
                // If it doesn't exist, create it with full configuration
                AutoConfigure(port);
                // Then update the development settings
                UpdateDevelopmentSettingsOnly(port, developmentMode, enableMcpLogs);
                return;
            }

            // Update only the development settings
            UpdateDevelopmentSettingsOnly(port, developmentMode, enableMcpLogs);
        }

        /// <summary>
        /// Updates only the environment variables for development mode and MCP debug logs.
        /// </summary>
        /// <param name="port">The port number.</param>
        /// <param name="developmentMode">Whether to enable development mode.</param>
        /// <param name="enableMcpLogs">Whether to enable MCP debug logs.</param>
        private void UpdateDevelopmentSettingsOnly(int port, bool developmentMode, bool enableMcpLogs)
        {
            string configPath = UnityMcpPathResolver.GetConfigPath(_editorType);
            
            // Load existing settings
            McpConfig config = _repository.Load(configPath);
            string serverKey = McpServerConfigFactory.CreateUnityMcpServerKey(port);
            
            if (!config.mcpServers.ContainsKey(serverKey))
            {
                McpLogger.LogError($"Server configuration not found: {serverKey}");
                return;
            }

            // Get existing configuration
            McpServerConfigData existingConfig = config.mcpServers[serverKey];
            
            // Create new environment variables based on existing ones
            Dictionary<string, string> updatedEnv = new(existingConfig.env);
            
            // Remove old development mode environment variables (cleanup legacy settings)
            updatedEnv.Remove(McpConstants.ENV_KEY_UMCP_DEBUG);
            updatedEnv.Remove(McpConstants.ENV_KEY_UMCP_PRODUCTION);
            updatedEnv.Remove(McpConstants.ENV_KEY_NODE_ENV);
            updatedEnv.Remove(McpConstants.ENV_KEY_MCP_DEBUG);
            
            // Remove MCP_CLIENT_NAME - now using clientInfo.name from MCP protocol
            updatedEnv.Remove(McpConstants.ENV_KEY_MCP_CLIENT_NAME);
            
            // Add NODE_ENV for development mode (simplified approach)
            if (developmentMode)
            {
                updatedEnv[McpConstants.ENV_KEY_NODE_ENV] = McpConstants.ENV_VALUE_DEVELOPMENT;
            }
            // For production mode, simply don't set NODE_ENV (default behavior)
            
            // Add MCP_DEBUG for MCP debug logs
            if (enableMcpLogs)
            {
                updatedEnv[McpConstants.ENV_KEY_MCP_DEBUG] = McpConstants.ENV_VALUE_TRUE;
            }
            // For disabled logs, simply don't set MCP_DEBUG (default behavior)
            
            // Create updated configuration (keeping command and args unchanged)
            McpServerConfigData updatedConfig = new McpServerConfigData(
                existingConfig.command,
                existingConfig.args,
                updatedEnv
            );
            
            // Update only this server's configuration
            Dictionary<string, McpServerConfigData> updatedServers = new(config.mcpServers);
            updatedServers[serverKey] = updatedConfig;
            
            // Save the updated configuration
            McpConfig updatedMcpConfig = new(updatedServers);
            _repository.Save(configPath, updatedMcpConfig);
            
            string editorName = GetEditorDisplayName(_editorType);
            McpLogger.LogInfo($"{editorName} development settings updated - Development mode: {developmentMode}, MCP logs: {enableMcpLogs}");
            McpLogger.LogInfo($"Server key: {serverKey}, Configuration file: {configPath}");
            
            // Log environment variables for debugging
            McpLogger.LogInfo($"Environment variables: {string.Join(", ", updatedEnv.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");
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


        /// <summary>
        /// Gets the configured port number from the editor settings.
        /// If server is running, tries to find config matching the current server port.
        /// Otherwise returns the first available configuration.
        /// </summary>
        /// <returns>The configured port number.</returns>
        public int GetConfiguredPort()
        {
            string configPath = UnityMcpPathResolver.GetConfigPath(_editorType);
            
            if (!_repository.Exists(configPath))
            {
                throw new System.InvalidOperationException("Configuration file not found.");
            }
            
            McpConfig config = _repository.Load(configPath);
            
            // If server is running, try to find config matching current server port
            bool serverIsRunning = McpServerController.IsServerRunning;
            int currentServerPort = serverIsRunning ? McpServerController.ServerPort : -1;
            
            // Collect all Unity MCP configurations
            var umcpConfigs = new System.Collections.Generic.List<(string key, int port)>();
            
            foreach (var serverEntry in config.mcpServers)
            {
                if (serverEntry.Key.StartsWith(McpConstants.PROJECT_NAME))
                {
                    if (serverEntry.Value.env.ContainsKey(McpConstants.UNITY_TCP_PORT_ENV_KEY))
                    {
                        string portString = serverEntry.Value.env[McpConstants.UNITY_TCP_PORT_ENV_KEY];
                        if (int.TryParse(portString, out int port))
                        {
                            umcpConfigs.Add((serverEntry.Key, port));
                        }
                    }
                }
            }
            
            if (umcpConfigs.Count == 0)
            {
                throw new System.InvalidOperationException("Unity MCP server configuration not found.");
            }
            
            // If server is running, find config matching current server port
            if (serverIsRunning)
            {
                var matchingConfig = umcpConfigs.FirstOrDefault(c => c.port == currentServerPort);
                if (matchingConfig != default)
                {
                    return matchingConfig.port;
                }
            }
            
            // Otherwise return the first configuration
            return umcpConfigs[0].port;
        }

        /// <summary>
        /// Validates configuration parameters for fail-fast behavior
        /// </summary>
        private void ValidateConfigurationParameters(int port)
        {
            // Validate port number using shared validator
            if (!McpPortValidator.ValidatePort(port, $"for {_editorType} configuration"))
            {
                throw new System.ArgumentOutOfRangeException(nameof(port), 
                    $"Invalid port number for {_editorType} configuration: {port}. Port must be between 1 and 65535.");
            }

            // Validate editor type
            if (!System.Enum.IsDefined(typeof(McpEditorType), _editorType))
            {
                throw new System.InvalidOperationException(
                    $"Cannot configure settings for invalid editor type: {_editorType}");
            }

            McpLogger.LogDebug($"Configuration parameters validated for {_editorType}: port={port}");
        }
    }
} 