using UnityEditor;
using System;
using Newtonsoft.Json;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// 強制再コンパイル時のSessionState管理クラス
    /// アセンブリリロードが発生してもリクエスト情報を保持する
    /// </summary>
    public static class CompileSessionState
    {
        private const string SESSION_KEY_PREFIX = "uMCP.CompileRequest.";
        private const string PENDING_REQUESTS_KEY = "uMCP.PendingCompileRequests";

        /// <summary>
        /// 強制再コンパイルリクエスト情報
        /// </summary>
        [Serializable]
        public class CompileRequestInfo
        {
            public string requestId;
            public bool forceRecompile;
            public string clientEndpoint;
            public DateTime requestTime;
            public bool isCompleted;

            public CompileRequestInfo(string requestId, bool forceRecompile, string clientEndpoint)
            {
                this.requestId = requestId;
                this.forceRecompile = forceRecompile;
                this.clientEndpoint = clientEndpoint;
                this.requestTime = DateTime.Now;
                this.isCompleted = false;
            }
        }

        /// <summary>
        /// 強制再コンパイルリクエストを保存
        /// </summary>
        public static void SaveCompileRequest(string requestId, bool forceRecompile, string clientEndpoint = "unknown")
        {
            if (!forceRecompile) return; // 通常のコンパイルは保存しない

            CompileRequestInfo requestInfo = new CompileRequestInfo(requestId, forceRecompile, clientEndpoint);
            string json = JsonConvert.SerializeObject(requestInfo);
            
            SessionState.SetString(SESSION_KEY_PREFIX + requestId, json);
            
            // 保留中のリクエストリストに追加
            string[] pendingRequests = GetPendingRequestIds();
            string[] newPendingRequests = new string[pendingRequests.Length + 1];
            Array.Copy(pendingRequests, newPendingRequests, pendingRequests.Length);
            newPendingRequests[pendingRequests.Length] = requestId;
            
            SessionState.SetString(PENDING_REQUESTS_KEY, JsonConvert.SerializeObject(newPendingRequests));
            
            McpLogger.LogDebug($"Saved compile request to SessionState: {requestId}");
        }

        /// <summary>
        /// 保留中のリクエストIDを取得
        /// </summary>
        public static string[] GetPendingRequestIds()
        {
            string json = SessionState.GetString(PENDING_REQUESTS_KEY, "[]");
            try
            {
                return JsonConvert.DeserializeObject<string[]>(json) ?? new string[0];
            }
            catch
            {
                return new string[0];
            }
        }

        /// <summary>
        /// 保存されたリクエスト情報を取得
        /// </summary>
        public static CompileRequestInfo GetCompileRequest(string requestId)
        {
            string json = SessionState.GetString(SESSION_KEY_PREFIX + requestId, null);
            if (string.IsNullOrEmpty(json)) return null;

            try
            {
                return JsonConvert.DeserializeObject<CompileRequestInfo>(json);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// リクエストを完了としてマーク
        /// </summary>
        public static void MarkRequestCompleted(string requestId)
        {
            CompileRequestInfo requestInfo = GetCompileRequest(requestId);
            if (requestInfo == null) return;

            requestInfo.isCompleted = true;
            string json = JsonConvert.SerializeObject(requestInfo);
            SessionState.SetString(SESSION_KEY_PREFIX + requestId, json);

            // 保留中のリクエストリストから削除
            RemoveFromPendingRequests(requestId);
            
            McpLogger.LogDebug($"Marked compile request as completed: {requestId}");
        }

        /// <summary>
        /// 保留中のリクエストリストから削除
        /// </summary>
        private static void RemoveFromPendingRequests(string requestId)
        {
            string[] pendingRequests = GetPendingRequestIds();
            string[] newPendingRequests = new string[pendingRequests.Length];
            int newIndex = 0;
            
            for (int i = 0; i < pendingRequests.Length; i++)
            {
                if (pendingRequests[i] != requestId)
                {
                    newPendingRequests[newIndex++] = pendingRequests[i];
                }
            }
            
            // 配列のサイズを調整
            if (newIndex < newPendingRequests.Length)
            {
                string[] trimmedArray = new string[newIndex];
                Array.Copy(newPendingRequests, trimmedArray, newIndex);
                newPendingRequests = trimmedArray;
            }
            
            SessionState.SetString(PENDING_REQUESTS_KEY, JsonConvert.SerializeObject(newPendingRequests));
        }

        /// <summary>
        /// 強制再コンパイルを開始する
        /// </summary>
        public static void StartForceRecompile()
        {
            McpLogger.LogDebug("Starting force recompile via CompilationPipeline");
            UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation(
                UnityEditor.Compilation.RequestScriptCompilationOptions.CleanBuildCache
            );
        }

        /// <summary>
        /// SessionStateをクリア
        /// </summary>
        public static void ClearAll()
        {
            string[] pendingRequests = GetPendingRequestIds();
            foreach (string requestId in pendingRequests)
            {
                SessionState.EraseString(SESSION_KEY_PREFIX + requestId);
            }
            SessionState.EraseString(PENDING_REQUESTS_KEY);
            
            McpLogger.LogDebug("Cleared all compile session state");
        }
    }
} 