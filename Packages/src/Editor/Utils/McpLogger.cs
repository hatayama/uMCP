using UnityEngine;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Unity MCP Server関連のログを統一的に管理するクラス
    /// </summary>
    public static class McpLogger
    {
        private const string LOG_PREFIX = "[Unity MCP]";
        
        /// <summary>
        /// デバッグログを出力するかどうか
        /// </summary>
        public static bool EnableDebugLog { get; set; } = false;
        
        /// <summary>
        /// 情報ログを出力する
        /// </summary>
        public static void LogInfo(string message)
        {
            if (EnableDebugLog)
            {
                Debug.Log($"{LOG_PREFIX} {message}");
            }
        }
        
        /// <summary>
        /// 警告ログを出力する
        /// </summary>
        public static void LogWarning(string message)
        {
            if (EnableDebugLog)
            {
                Debug.LogWarning($"{LOG_PREFIX} {message}");
            }
        }
        
        /// <summary>
        /// エラーログを出力する
        /// </summary>
        public static void LogError(string message)
        {
            if (EnableDebugLog)
            {
                Debug.LogError($"{LOG_PREFIX} {message}");
            }
        }
        
        /// <summary>
        /// デバッグログを出力する（EnableDebugLogがtrueの場合のみ）
        /// </summary>
        public static void LogDebug(string message)
        {
            if (EnableDebugLog)
            {
                Debug.Log($"{LOG_PREFIX} [DEBUG] {message}");
            }
        }
        
        /// <summary>
        /// JSON-RPC通信ログを出力する
        /// </summary>
        public static void LogJsonRpc(string direction, string content)
        {
            if (EnableDebugLog)
            {
                Debug.Log($"{LOG_PREFIX} [JSON-RPC {direction}] {content}");
            }
        }
        
        /// <summary>
        /// クライアント接続ログを出力する
        /// </summary>
        public static void LogClientConnection(string clientEndpoint, bool connected)
        {
            string status = connected ? "connected" : "disconnected";
            LogInfo($"Client {status}: {clientEndpoint}");
        }
    }
} 