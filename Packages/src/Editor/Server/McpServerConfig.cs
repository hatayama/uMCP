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
        
        /// <summary>
        /// ポート番号の最小値
        /// </summary>
        public const int MIN_PORT_NUMBER = 1024;
        
        /// <summary>
        /// ポート番号の最大値
        /// </summary>
        public const int MAX_PORT_NUMBER = 65535;
        
        /// <summary>
        /// 不明なクライアントエンドポイント
        /// </summary>
        public const string UNKNOWN_CLIENT_ENDPOINT = "Unknown";
        
        /// <summary>
        /// デフォルトのログタイプ
        /// </summary>
        public const string DEFAULT_LOG_TYPE = "All";
        
        /// <summary>
        /// デフォルトの最大ログ件数
        /// </summary>
        public const int DEFAULT_MAX_LOG_COUNT = 100;
        
        /// <summary>
        /// デフォルトの検索テキスト
        /// </summary>
        public const string DEFAULT_SEARCH_TEXT = "";
        
        /// <summary>
        /// デフォルトでスタックトレースを含むかどうか
        /// </summary>
        public const bool DEFAULT_INCLUDE_STACK_TRACE = true;
        
        /// <summary>
        /// デフォルトの行番号
        /// </summary>
        public const int DEFAULT_LINE_NUMBER = 0;
        
        /// <summary>
        /// 日時フォーマット文字列
        /// </summary>
        public const string TIMESTAMP_FORMAT = "yyyy-MM-dd HH:mm:ss";
        
        /// <summary>
        /// ISO形式日時フォーマット文字列
        /// </summary>
        public const string ISO_DATETIME_FORMAT = "yyyy-MM-ddTHH:mm:ss.fffZ";
    }
} 