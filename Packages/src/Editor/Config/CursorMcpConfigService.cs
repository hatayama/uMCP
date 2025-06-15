using System.Collections.Generic;
using System.Linq;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Cursor MCP設定のビジネスロジックを担当するクラス
    /// 単一責任原則：設定管理のビジネスロジックのみを担当
    /// </summary>
    public class CursorMcpConfigService
    {
        private readonly CursorMcpConfigRepository _repository;

        public CursorMcpConfigService(CursorMcpConfigRepository repository)
        {
            _repository = repository;
        }

        /// <summary>
        /// Cursor設定が存在するかチェック
        /// </summary>
        public bool IsCursorConfigured()
        {
            string configPath = UnityMcpPathResolver.GetMcpConfigPath();
            if (!_repository.Exists(configPath))
            {
                return false;
            }

            CursorMcpConfig config = _repository.Load(configPath);
            // ポート番号付きの設定が存在するかチェック
            return config.mcpServers.Keys.Any(key => key.StartsWith("unity-mcp"));
        }

        /// <summary>
        /// Cursor設定を自動設定
        /// </summary>
        /// <param name="port">使用するポート番号</param>
        public void AutoConfigureCursor(int port)
        {
            string configPath = UnityMcpPathResolver.GetMcpConfigPath();
            
            // .cursorディレクトリを作成
            _repository.CreateConfigDirectory(configPath);

            // 既存設定を読み込み（存在しない場合は新規作成）
            CursorMcpConfig config = _repository.Load(configPath);

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

                CursorMcpConfig updatedConfig = new(updatedServers);
                _repository.Save(configPath, updatedConfig);

                McpLogger.LogInfo($"Cursor configuration updated: {configPath}");
                McpLogger.LogInfo($"Server key: {serverKey}, Port: {port}");
                McpLogger.LogInfo($"Final MCP servers: {string.Join(", ", updatedConfig.mcpServers.Keys)}");
            }
        }
    }
} 