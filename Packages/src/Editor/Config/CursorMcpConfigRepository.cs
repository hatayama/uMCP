using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Class responsible for persisting MCP settings.
    /// Single Responsibility Principle: Only responsible for reading and writing configuration files.
    /// </summary>
    public class McpConfigRepository
    {
        private readonly McpEditorType _editorType;

        public McpConfigRepository(McpEditorType editorType = McpEditorType.Cursor)
        {
            _editorType = editorType;
        }

        /// <summary>
        /// Checks if the configuration file exists.
        /// </summary>
        public bool Exists(string configPath)
        {
            return File.Exists(configPath);
        }

        /// <summary>
        /// Checks if the configuration file exists (with automatic editor type resolution).
        /// </summary>
        public bool Exists()
        {
            string configPath = UnityMcpPathResolver.GetConfigPath(_editorType);
            return Exists(configPath);
        }

        /// <summary>
        /// Creates the configuration directory.
        /// </summary>
        public void CreateConfigDirectory(string configPath)
        {
            string configDir = Path.GetDirectoryName(configPath);
            if (!string.IsNullOrEmpty(configDir))
            {
                Directory.CreateDirectory(configDir);
            }
        }

        /// <summary>
        /// Creates the configuration directory (with automatic editor type resolution).
        /// </summary>
        public void CreateConfigDirectory()
        {
            string configDirectory = UnityMcpPathResolver.GetConfigDirectory(_editorType);
            if (!string.IsNullOrEmpty(configDirectory))
            {
                Directory.CreateDirectory(configDirectory);
            }
        }

        /// <summary>
        /// Loads the mcp.json settings.
        /// </summary>
        public McpConfig Load(string configPath)
        {
            if (!File.Exists(configPath))
            {
                return new McpConfig(new Dictionary<string, McpServerConfigData>());
            }

            string jsonContent = File.ReadAllText(configPath);
            
            // First, load the existing JSON as a dictionary.
            Dictionary<string, object> rootObject = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonContent);
            Dictionary<string, McpServerConfigData> servers = new();
            
            // Check if the mcpServers section exists.
            if (rootObject != null && rootObject.ContainsKey("mcpServers"))
            {
                // Get mcpServers as a dictionary.
                string mcpServersJson = JsonConvert.SerializeObject(rootObject["mcpServers"]);
                Dictionary<string, object> mcpServersObject = JsonConvert.DeserializeObject<Dictionary<string, object>>(mcpServersJson);
                
                if (mcpServersObject != null)
                {
                    foreach (KeyValuePair<string, object> serverEntry in mcpServersObject)
                    {
                        string serverName = serverEntry.Key;
                        
                        // Get each server's settings as a dictionary.
                        string serverConfigJson = JsonConvert.SerializeObject(serverEntry.Value);
                        Dictionary<string, object> serverConfigObject = JsonConvert.DeserializeObject<Dictionary<string, object>>(serverConfigJson);
                        
                        if (serverConfigObject != null)
                        {
                            string command = serverConfigObject.ContainsKey("command") ? serverConfigObject["command"]?.ToString() ?? "" : "";
                            
                            string[] args = new string[0];
                            if (serverConfigObject.ContainsKey("args"))
                            {
                                string argsJson = JsonConvert.SerializeObject(serverConfigObject["args"]);
                                args = JsonConvert.DeserializeObject<string[]>(argsJson) ?? new string[0];
                            }
                            
                            Dictionary<string, string> env = new();
                            if (serverConfigObject.ContainsKey("env"))
                            {
                                string envJson = JsonConvert.SerializeObject(serverConfigObject["env"]);
                                env = JsonConvert.DeserializeObject<Dictionary<string, string>>(envJson) ?? new Dictionary<string, string>();
                            }
                            
                            servers[serverName] = new McpServerConfigData(command, args, env);
                        }
                    }
                }
            }
            
            return new McpConfig(servers);
        }

        /// <summary>
        /// Loads the mcp.json settings (with automatic editor type resolution).
        /// </summary>
        public McpConfig Load()
        {
            string configPath = UnityMcpPathResolver.GetConfigPath(_editorType);
            return Load(configPath);
        }

        /// <summary>
        /// Saves the mcp.json settings.
        /// </summary>
        public void Save(string configPath, McpConfig config)
        {
            Dictionary<string, object> jsonStructure;
            
            // If the file exists, retain its existing structure.
            if (File.Exists(configPath))
            {
                string existingContent = File.ReadAllText(configPath);
                jsonStructure = JsonConvert.DeserializeObject<Dictionary<string, object>>(existingContent) ?? new Dictionary<string, object>();
            }
            else
            {
                jsonStructure = new Dictionary<string, object>();
            }
            
            // Update only the mcpServers section.
            jsonStructure["mcpServers"] = config.mcpServers.ToDictionary(
                kvp => kvp.Key,
                kvp => new
                {
                    command = kvp.Value.command,
                    args = kvp.Value.args,
                    env = kvp.Value.env
                }
            );

            string jsonContent = JsonConvert.SerializeObject(jsonStructure, Formatting.Indented);
            File.WriteAllText(configPath, jsonContent);
        }

        /// <summary>
        /// Saves the mcp.json settings (with automatic editor type resolution).
        /// </summary>
        public void Save(McpConfig config)
        {
            string configPath = UnityMcpPathResolver.GetConfigPath(_editorType);
            
            // Create the directory if it's needed.
            CreateConfigDirectory(configPath);
            
            Save(configPath, config);
        }
    }


} 