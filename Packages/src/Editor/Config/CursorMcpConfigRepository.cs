using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// MCP設定の永続化を担当するクラス
    /// 単一責任原則：設定ファイルの読み書きのみを担当
    /// </summary>
    public class McpConfigRepository
    {
        private readonly McpEditorType _editorType;

        public McpConfigRepository(McpEditorType editorType = McpEditorType.Cursor)
        {
            _editorType = editorType;
        }

        /// <summary>
        /// 設定ファイルが存在するかチェック
        /// </summary>
        public bool Exists(string configPath)
        {
            return File.Exists(configPath);
        }

        /// <summary>
        /// 設定ファイルが存在するかチェック（エディタタイプ自動解決版）
        /// </summary>
        public bool Exists()
        {
            string configPath = UnityMcpPathResolver.GetConfigPath(_editorType);
            return Exists(configPath);
        }

        /// <summary>
        /// 設定ディレクトリを作成
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
        /// 設定ディレクトリを作成（エディタタイプ自動解決版）
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
        /// mcp.json設定を読み込み
        /// </summary>
        public McpConfig Load(string configPath)
        {
            if (!File.Exists(configPath))
            {
                return new McpConfig(new Dictionary<string, McpServerConfigData>());
            }

            string jsonContent = File.ReadAllText(configPath);
            
            // まず、既存のJSONを辞書として読み込み
            Dictionary<string, object> rootObject = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonContent);
            Dictionary<string, McpServerConfigData> servers = new();
            
            // mcpServersセクションが存在するかチェック
            if (rootObject != null && rootObject.ContainsKey("mcpServers"))
            {
                // mcpServersを辞書として取得
                string mcpServersJson = JsonConvert.SerializeObject(rootObject["mcpServers"]);
                Dictionary<string, object> mcpServersObject = JsonConvert.DeserializeObject<Dictionary<string, object>>(mcpServersJson);
                
                if (mcpServersObject != null)
                {
                    foreach (KeyValuePair<string, object> serverEntry in mcpServersObject)
                    {
                        string serverName = serverEntry.Key;
                        
                        // 各サーバー設定を辞書として取得
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
        /// mcp.json設定を読み込み（エディタタイプ自動解決版）
        /// </summary>
        public McpConfig Load()
        {
            string configPath = UnityMcpPathResolver.GetConfigPath(_editorType);
            return Load(configPath);
        }

        /// <summary>
        /// mcp.json設定を保存
        /// </summary>
        public void Save(string configPath, McpConfig config)
        {
            Dictionary<string, object> jsonStructure;
            
            // 既存ファイルがある場合は、既存の構造を保持
            if (File.Exists(configPath))
            {
                string existingContent = File.ReadAllText(configPath);
                jsonStructure = JsonConvert.DeserializeObject<Dictionary<string, object>>(existingContent) ?? new Dictionary<string, object>();
            }
            else
            {
                jsonStructure = new Dictionary<string, object>();
            }
            
            // mcpServersセクションのみを更新
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
        /// mcp.json設定を保存（エディタタイプ自動解決版）
        /// </summary>
        public void Save(McpConfig config)
        {
            string configPath = UnityMcpPathResolver.GetConfigPath(_editorType);
            
            // ディレクトリが必要な場合は作成
            CreateConfigDirectory(configPath);
            
            Save(configPath, config);
        }
    }

    /// <summary>
    /// 後方互換性のためのエイリアス
    /// </summary>
    [System.Obsolete("Use McpConfigRepository instead")]
    public class CursorMcpConfigRepository : McpConfigRepository
    {
        public CursorMcpConfigRepository() : base(McpEditorType.Cursor)
        {
        }
    }
} 