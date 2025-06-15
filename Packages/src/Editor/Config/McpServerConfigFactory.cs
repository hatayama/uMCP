using System.Collections.Generic;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// MCP サーバー設定オブジェクトの生成を担当するクラス
    /// 単一責任原則：設定オブジェクトの生成のみを担当
    /// </summary>
    public static class McpServerConfigFactory
    {
        /// <summary>
        /// Unity MCP用の設定を作成
        /// </summary>
        /// <param name="port">使用するポート番号</param>
        /// <param name="serverPath">TypeScriptサーバーのパス</param>
        /// <returns>Unity MCP用の設定データ</returns>
        public static McpServerConfigData CreateUnityMcpConfig(int port, string serverPath)
        {
            return new McpServerConfigData(
                command: "node",
                args: new[] { serverPath },
                env: new Dictionary<string, string> { { "UNITY_TCP_PORT", port.ToString() } }
            );
        }

        /// <summary>
        /// Unity MCP設定のサーバーキーを生成
        /// </summary>
        /// <param name="port">ポート番号</param>
        /// <returns>サーバーキー</returns>
        public static string CreateUnityMcpServerKey(int port)
        {
            return $"unity-mcp-{port}";
        }
    }
} 