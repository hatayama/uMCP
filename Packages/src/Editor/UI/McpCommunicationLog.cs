using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEditor;
using Newtonsoft.Json;
using Cysharp.Threading.Tasks;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// MCP通信ログのエントリー
    /// Request/Responseがセットになったログ情報
    /// </summary>
    public record McpCommunicationLogEntry
    {
        public readonly string CommandName;
        public readonly DateTime Timestamp;
        public readonly string Request;
        public readonly string Response;
        public readonly bool IsError;
        public bool IsExpanded { get; set; }

        public string HeaderText => $"[{CommandName}: {Timestamp:HH:mm:ss}]";

        public McpCommunicationLogEntry(string commandName, DateTime timestamp, string request, string response, bool isError, bool isExpanded = false)
        {
            CommandName = commandName;
            Timestamp = timestamp;
            Request = request;
            Response = response;
            IsError = isError;
            IsExpanded = isExpanded;
        }
    }

    /// <summary>
    /// MCP通信ログの管理クラス
    /// </summary>
    public static class McpCommunicationLogger
    {
        private const string LOGS_SESSION_KEY = "UnityPocketMCP.CommunicationLogs";
        private const string PENDING_REQUESTS_SESSION_KEY = "UnityPocketMCP.PendingRequests";

        private static List<McpCommunicationLogEntry> _logs;
        private static Dictionary<string, PendingRequest> _pendingRequests;

        /// <summary>
        /// ログ更新時のイベント（UI更新用）
        /// </summary>
        public static event System.Action OnLogUpdated;

        /// <summary>
        /// 静的コンストラクタ（Domain Reload後に自動実行）
        /// </summary>
        static McpCommunicationLogger()
        {
            LoadFromSessionState();
        }

        /// <summary>
        /// 保留中のリクエスト情報
        /// </summary>
        private record PendingRequest
        {
            public readonly string CommandName;
            public readonly DateTime Timestamp;
            public readonly string RequestJson;

            public PendingRequest(string commandName, DateTime timestamp, string requestJson)
            {
                CommandName = commandName;
                Timestamp = timestamp;
                RequestJson = requestJson;
            }
        }

        /// <summary>
        /// 全てのログを取得
        /// </summary>
        public static IReadOnlyList<McpCommunicationLogEntry> GetAllLogs()
        {
            return _logs.AsReadOnly();
        }

        /// <summary>
        /// リクエストを記録（レスポンス待ち状態）
        /// </summary>
        public static async void LogRequest(string jsonRequest)
        {
            McpLogger.LogDebug($"LogRequest called: {jsonRequest}");

            JObject request = JObject.Parse(jsonRequest);
            string method = request["method"]?.ToString() ?? "unknown";
            string id = request["id"]?.ToString() ?? "unknown";

            McpLogger.LogDebug($"Storing request with ID: '{id}' (Type: {id.GetType().Name}), Method: {method}");

            PendingRequest pendingRequest = new(method, DateTime.Now, jsonRequest);

            _pendingRequests[id] = pendingRequest;

            // メインスレッドに切り替えてSessionState保存とUI更新
            await UniTask.SwitchToMainThread();
            SaveToSessionState();
            OnLogUpdated?.Invoke();

            McpLogger.LogDebug($"Request logged - Method: {method}, ID: {id}");
        }

        /// <summary>
        /// レスポンスを記録（リクエストとセットにしてログに追加）
        /// </summary>
        public static async void LogResponse(string jsonResponse)
        {
            McpLogger.LogDebug($"LogResponse called: {jsonResponse}");

            JObject response = JObject.Parse(jsonResponse);
            string id = response["id"]?.ToString() ?? "unknown";

            McpLogger.LogDebug($"Looking for request with ID: '{id}' (Type: {id.GetType().Name})");
            McpLogger.LogDebug($"Pending requests count: {_pendingRequests.Count}");
            foreach (var kvp in _pendingRequests)
            {
                McpLogger.LogDebug($"- Pending ID: '{kvp.Key}' (Type: {kvp.Key.GetType().Name}), Method: {kvp.Value.CommandName}");
            }

            if (_pendingRequests.TryGetValue(id, out PendingRequest pendingRequest))
            {
                bool isError = response["error"] != null;

                // 既存のログがある場合は全て閉じる（新しいログを追加する前に）
                foreach (McpCommunicationLogEntry existingLog in _logs)
                {
                    existingLog.IsExpanded = false;
                }

                // 新しいログは閉じた状態で作成
                McpCommunicationLogEntry logEntry = new(
                    pendingRequest.CommandName,
                    pendingRequest.Timestamp,
                    pendingRequest.RequestJson,
                    jsonResponse,
                    isError,
                    false // 最初からトグルを閉じた状態にする
                );

                _logs.Add(logEntry);
                _pendingRequests.Remove(id);

                McpLogger.LogDebug($"Response logged - Method: {pendingRequest.CommandName}, Total logs: {_logs.Count}");

                // メインスレッドに切り替えてSessionState保存とUI更新
                await UniTask.SwitchToMainThread();

                // 即座にSessionStateに保存（Domain Reload対策）
                SaveToSessionState();

                OnLogUpdated?.Invoke();
            }
            else
            {
                McpLogger.LogWarning($"No pending request found for response ID: {id}");
            }
        }

        /// <summary>
        /// 全てのログをクリア
        /// </summary>
        public static void ClearLogs()
        {
            _logs.Clear();
            _pendingRequests.Clear();

            // SessionStateも完全に削除してUI更新をメインスレッドで実行
            EditorApplication.delayCall += () =>
            {
                ClearLogSessionState();
                OnLogUpdated?.Invoke();
            };
        }

        /// <summary>
        /// SessionStateからデータを復元
        /// </summary>
        private static void LoadFromSessionState()
        {
            // ログの復元
            string logsJson = SessionState.GetString(LOGS_SESSION_KEY, "[]");
            try
            {
                _logs = JsonConvert.DeserializeObject<List<McpCommunicationLogEntry>>(logsJson) ?? new List<McpCommunicationLogEntry>();
            }
            catch (Exception ex)
            {
                McpLogger.LogWarning($"Failed to deserialize logs: {ex.Message}");
                _logs = new List<McpCommunicationLogEntry>();
            }

            // 保留中リクエストの復元
            string pendingJson = SessionState.GetString(PENDING_REQUESTS_SESSION_KEY, "{}");
            try
            {
                _pendingRequests = JsonConvert.DeserializeObject<Dictionary<string, PendingRequest>>(pendingJson) ?? new Dictionary<string, PendingRequest>();
            }
            catch (Exception ex)
            {
                McpLogger.LogWarning($"Failed to deserialize pending requests: {ex.Message}");
                _pendingRequests = new Dictionary<string, PendingRequest>();
            }
        }

        /// <summary>
        /// SessionStateにデータを保存
        /// </summary>
        public static void SaveToSessionState()
        {
            try
            {
                string logsJson = JsonConvert.SerializeObject(_logs);
                string pendingJson = JsonConvert.SerializeObject(_pendingRequests);

                SessionState.SetString(LOGS_SESSION_KEY, logsJson);
                SessionState.SetString(PENDING_REQUESTS_SESSION_KEY, pendingJson);
            }
            catch (Exception ex)
            {
                McpLogger.LogError($"Failed to save to SessionState: {ex.Message}");
            }
        }

        /// <summary>
        /// 通信ログ関連のSessionStateをクリア（ログ問題修正用）
        /// </summary>
        public static void ClearLogSessionState()
        {
            try
            {
                SessionState.EraseString(LOGS_SESSION_KEY);
                SessionState.EraseString(PENDING_REQUESTS_SESSION_KEY);
                _logs?.Clear();
                _pendingRequests?.Clear();

                // SessionStateクリア完了（ログ出力なし）

                // UIの更新通知
                EditorApplication.delayCall += () => OnLogUpdated?.Invoke();
            }
            catch (Exception ex)
            {
                McpLogger.LogError($"Failed to clear communication log SessionState: {ex.Message}");
            }
        }
    }
}