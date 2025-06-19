using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;
using Newtonsoft.Json;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// MCP通信ログのエントリー
    /// Request/Responseがセットになったログ情報
    /// </summary>
    public class McpCommunicationLogEntry
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
    /// ScriptableSingletonで永続化を行う
    /// </summary>
    public static class McpCommunicationLogger
    {
        private static List<McpCommunicationLogEntry> _logs;
        private static Dictionary<string, PendingRequestInfo> _pendingRequests;

        /// <summary>
        /// ログ更新時のイベント（UI更新用）
        /// </summary>
        public static event System.Action OnLogUpdated;


        /// <summary>
        /// ScriptableSingletonからデータを復元
        /// </summary>
        private static void LoadFromScriptableSingleton()
        {
            try
            {
                var logData = McpCommunicationLogData.instance;
                
                // ログの復元
                _logs = logData.GetLogs();
                
                // 保留中リクエストの復元
                _pendingRequests = new Dictionary<string, PendingRequestInfo>();
                foreach (var pending in logData.GetPendingRequests())
                {
                    if (!string.IsNullOrEmpty(pending.requestId))
                    {
                        _pendingRequests[pending.requestId] = pending;
                    }
                }
            }
            catch (System.Exception ex)
            {
                // 初期化失敗時はデフォルト値で初期化
                _logs = new List<McpCommunicationLogEntry>();
                _pendingRequests = new Dictionary<string, PendingRequestInfo>();
                McpLogger.LogError($"Failed to load from ScriptableSingleton: {ex.Message}");
            }
        }

        /// <summary>
        /// 全てのログを取得
        /// </summary>
        public static IReadOnlyList<McpCommunicationLogEntry> GetAllLogs()
        {
            EnsureInitialized();
            return _logs.AsReadOnly();
        }

        /// <summary>
        /// リクエストを記録（レスポンス待ち状態）
        /// </summary>
        public static async Task LogRequestAsync(string requestId, string commandName, string requestJson, CancellationToken cancellationToken = default)
        {
            EnsureInitialized();
            
            var logData = McpCommunicationLogData.instance;
            await logData.AddPendingRequestAsync(requestId, commandName, requestJson, cancellationToken);
            
            var pendingRequest = new PendingRequestInfo(requestId, commandName, requestJson);
            _pendingRequests[requestId] = pendingRequest;
        }

        /// <summary>
        /// レスポンスを記録（リクエストと組み合わせてログエントリー作成）
        /// </summary>
        public static async Task LogResponseAsync(string requestId, string responseJson, bool isError = false, CancellationToken cancellationToken = default)
        {
            EnsureInitialized();
            
            var logData = McpCommunicationLogData.instance;
            await logData.CompletePendingRequestAsync(requestId, responseJson, isError, cancellationToken);
            
            // ローカルの保留中リクエストからも削除
            if (_pendingRequests.ContainsKey(requestId))
            {
                _pendingRequests.Remove(requestId);
            }
            
            // ローカルログを再読み込み
            _logs = logData.GetLogs();
            
            OnLogUpdated?.Invoke();
        }

        /// <summary>
        /// 直接ログエントリーを追加（リクエスト・レスポンスが同時にある場合）
        /// </summary>
        public static async Task LogEntryAsync(string commandName, string requestJson, string responseJson, bool isError = false, CancellationToken cancellationToken = default)
        {
            EnsureInitialized();
            
            var logData = McpCommunicationLogData.instance;
            await logData.AddLogAsync(commandName, DateTime.Now, requestJson, responseJson, isError, cancellationToken);
            
            // ローカルログを再読み込み
            _logs = logData.GetLogs();
            
            OnLogUpdated?.Invoke();
        }

        /// <summary>
        /// ログエントリーの展開状態を変更
        /// </summary>
        public static async Task SetLogExpandedAsync(int index, bool expanded, CancellationToken cancellationToken = default)
        {
            EnsureInitialized();
            
            if (index >= 0 && index < _logs.Count)
            {
                _logs[index].IsExpanded = expanded;
                
                var logData = McpCommunicationLogData.instance;
                await logData.SetLogExpandedAsync(index, expanded, cancellationToken);
            }
        }

        /// <summary>
        /// 通信ログをクリア
        /// </summary>
        public static async Task ClearLogsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var logData = McpCommunicationLogData.instance;
                await logData.ClearAllLogsAsync(cancellationToken);
                
                _logs?.Clear();
                _pendingRequests?.Clear();
                
                OnLogUpdated?.Invoke();
                McpLogger.LogInfo("Cleared communication logs");
            }
            catch (Exception ex)
            {
                McpLogger.LogError($"Failed to clear logs: {ex.Message}");
            }
        }

        /// <summary>
        /// 完了したログのみクリア（保留中は保持）
        /// </summary>
        public static async Task ClearCompletedLogsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var logData = McpCommunicationLogData.instance;
                await logData.ClearCompletedLogsAsync(cancellationToken);
                
                // ローカルログを再読み込み
                _logs = logData.GetLogs();
                
                OnLogUpdated?.Invoke();
                McpLogger.LogInfo("Cleared completed communication logs");
            }
            catch (Exception ex)
            {
                McpLogger.LogError($"Failed to clear completed logs: {ex.Message}");
            }
        }

        /// <summary>
        /// 最大ログ数を設定
        /// </summary>
        public static void SetMaxLogCount(int maxCount)
        {
            var logData = McpCommunicationLogData.instance;
            logData.MaxLogCount = maxCount;
            
            // ローカルログを再読み込み
            _logs = logData.GetLogs();
            
            OnLogUpdated?.Invoke();
        }

        /// <summary>
        /// 現在の最大ログ数を取得
        /// </summary>
        public static int GetMaxLogCount()
        {
            return McpCommunicationLogData.instance.MaxLogCount;
        }

        /// <summary>
        /// 保留中のリクエスト数を取得
        /// </summary>
        public static int GetPendingRequestCount()
        {
            EnsureInitialized();
            return _pendingRequests.Count;
        }

        /// <summary>
        /// デバッグ情報を取得
        /// </summary>
        public static string GetDebugInfo()
        {
            EnsureInitialized();
            var logData = McpCommunicationLogData.instance;
            return $"McpCommunicationLogger: LocalLogs={_logs.Count}, LocalPending={_pendingRequests.Count}, {logData.GetDebugInfo()}";
        }

        /// <summary>
        /// 初期化処理（遅延初期化）
        /// </summary>
        private static void EnsureInitialized()
        {
            if (_logs == null || _pendingRequests == null)
            {
                try
                {
                    LoadFromScriptableSingleton();
                }
                catch (System.Exception ex)
                {
                    // 完全にフォールバック
                    _logs = new List<McpCommunicationLogEntry>();
                    _pendingRequests = new Dictionary<string, PendingRequestInfo>();
                    McpLogger.LogError($"EnsureInitialized failed: {ex.Message}");
                }
            }
        }
    }
}