using System.Collections.Generic;
using System.Linq;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// MCP設定のビジネスロジックを担当するクラス
    /// 単一責任原則：設定管理のビジネスロジックのみを担当
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
        /// エディタ設定が存在するかチェック
        /// </summary>
        public bool IsConfigured()
        {
            string configPath = UnityMcpPathResolver.GetConfigPath(_editorType);
            if (!_repository.Exists(configPath))
            {
                return false;
            }

            McpConfig config = _repository.Load(configPath);
            // ポート番号付きの設定が存在するかチェック
            return config.mcpServers.Keys.Any(key => key.StartsWith("unity-mcp"));
        }

        /// <summary>
        /// エディタ設定を自動設定
        /// </summary>
        /// <param name="port">使用するポート番号</param>
        public void AutoConfigure(int port)
        {
            string configPath = UnityMcpPathResolver.GetConfigPath(_editorType);
            
            // 設定ディレクトリを作成（必要な場合のみ）
            _repository.CreateConfigDirectory(configPath);

            // 既存設定を読み込み（存在しない場合は新規作成）
            McpConfig config = _repository.Load(configPath);

            // 既存設定の確認ログ
            McpLogger.LogInfo($"Loaded existing MCP servers: {string.Join(", ", config.mcpServers.Keys)}");

            // ポート番号を含む設定キーを生成
            string serverKey = McpServerConfigFactory.CreateUnityMcpServerKey(port);

            // 新しい設定を作成
            string serverPath = UnityMcpPathResolver.GetTypeScriptServerPath();
            McpServerConfigData newConfig = McpServerConfigFactory.CreateUnityMcpConfig(port, serverPath);

            // 既存設定を保持し、新しい設定を追加/更新
            Dictionary<string, McpServerConfigData> updatedServers = new(config.mcpServers);

            // 既存設定と比較して、違いがあれば更新
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
                // Unity MCP設定を追加/更新
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
        /// エディタの表示名を取得
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