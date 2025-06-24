using UnityEngine;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Class for unified management of Unity MCP Server related logs
    /// </summary>
    public static class McpLogger
    {
        private const string LOG_PREFIX = "[Unity MCP]";
        
        /// <summary>
        /// Whether to output debug logs
        /// </summary>
        public static bool EnableDebugLog { get; set; } = false;
        
        /// <summary>
        /// Output information log
        /// </summary>
        public static void LogInfo(string message)
        {
            if (EnableDebugLog)
            {
                Debug.Log($"{LOG_PREFIX} {message}");
            }
        }
        
        /// <summary>
        /// Output warning log
        /// </summary>
        public static void LogWarning(string message)
        {
            if (EnableDebugLog)
            {
                Debug.LogWarning($"{LOG_PREFIX} {message}");
            }
        }
        
        /// <summary>
        /// Output error log
        /// </summary>
        public static void LogError(string message)
        {
            if (EnableDebugLog)
            {
                Debug.LogError($"{LOG_PREFIX} {message}");
            }
        }
        
        /// <summary>
        /// Output debug log (only when EnableDebugLog is true)
        /// </summary>
        public static void LogDebug(string message)
        {
            if (EnableDebugLog)
            {
                Debug.Log($"{LOG_PREFIX} [DEBUG] {message}");
            }
        }
        
        /// <summary>
        /// Output JSON-RPC communication log
        /// </summary>
        public static void LogJsonRpc(string direction, string content)
        {
            if (EnableDebugLog)
            {
                Debug.Log($"{LOG_PREFIX} [JSON-RPC {direction}] {content}");
            }
        }
        
        /// <summary>
        /// Output client connection log
        /// </summary>
        public static void LogClientConnection(string clientEndpoint, bool connected)
        {
            string status = connected ? "connected" : "disconnected";
            LogInfo($"Client {status}: {clientEndpoint}");
        }
    }
} 