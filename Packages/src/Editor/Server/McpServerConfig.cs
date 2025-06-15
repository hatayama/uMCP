namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Unity MCP Server関連の設定を管理するクラス
    /// </summary>
    public static class McpServerConfig
    {
        /// <summary>
        /// デフォルトのポート番号
        /// </summary>
        public const int DEFAULT_PORT = 7400;
        
        /// <summary>
        /// TCP/IP通信のバッファサイズ
        /// </summary>
        public const int BUFFER_SIZE = 4096;
        
        /// <summary>
        /// サーバー停止時の待機タイムアウト（秒）
        /// </summary>
        public const int SHUTDOWN_TIMEOUT_SECONDS = 5;
        
        /// <summary>
        /// JSON-RPC 2.0のバージョン文字列
        /// </summary>
        public const string JSONRPC_VERSION = "2.0";
        
        /// <summary>
        /// 内部エラーコード
        /// </summary>
        public const int INTERNAL_ERROR_CODE = -32603;
    }
} 