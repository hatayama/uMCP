using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// 保留中のリクエスト情報
    /// </summary>
    [Serializable]
    public class PendingRequestInfo
    {
        public readonly string requestId;
        public readonly string commandName;
        public readonly string requestTime;
        public readonly string request;

        public PendingRequestInfo(string requestId, string commandName, string request)
        {
            this.requestId = requestId;
            this.commandName = commandName;
            this.request = request;
            this.requestTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        }
    }

    /// <summary>
    /// 通信ログエントリー（シリアライズ可能版）
    /// </summary>
    [Serializable]
    public class SerializableLogEntry
    {
        public readonly string commandName;
        public readonly string timestamp;
        public readonly string request;
        public readonly string response;
        public readonly bool isError;
        public bool isExpanded; // UI状態のためミュータブル

        public SerializableLogEntry(string commandName, DateTime timestamp, string request, string response, bool isError, bool isExpanded = false)
        {
            this.commandName = commandName;
            this.timestamp = timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff");
            this.request = request;
            this.response = response;
            this.isError = isError;
            this.isExpanded = isExpanded;
        }

        /// <summary>
        /// McpCommunicationLogEntryに変換
        /// </summary>
        public McpCommunicationLogEntry ToLogEntry()
        {
            DateTime parsedTime = DateTime.TryParse(timestamp, out DateTime result) ? result : DateTime.Now;
            return new McpCommunicationLogEntry(commandName, parsedTime, request, response, isError, isExpanded);
        }
    }

    /// <summary>
    /// MCP通信ログを永続化するScriptableSingleton
    /// Domain Reloadに関係なく通信ログを管理する
    /// </summary>
    [FilePath("Library/UnityMcpCommunicationLog.asset", FilePathAttribute.Location.ProjectFolder)]
    public class McpCommunicationLogData : ScriptableSingleton<McpCommunicationLogData>
    {
        [SerializeField] private List<SerializableLogEntry> logs = new List<SerializableLogEntry>();
        [SerializeField] private List<PendingRequestInfo> pendingRequests = new List<PendingRequestInfo>();
        [SerializeField] private int maxLogCount = 100;

        /// <summary>
        /// 通信ログの最大保持数
        /// </summary>
        public int MaxLogCount
        {
            get => maxLogCount;
            set
            {
                if (maxLogCount != value && value > 0)
                {
                    maxLogCount = value;
                    // ログ数が上限を超えている場合は古いものを削除
                    while (logs.Count > maxLogCount)
                    {
                        logs.RemoveAt(0);
                    }
                    _ = SaveSafeAsync(); // Fire and forget
                }
            }
        }

        /// <summary>
        /// 通信ログエントリー一覧を取得
        /// </summary>
        public List<McpCommunicationLogEntry> GetLogs()
        {
            List<McpCommunicationLogEntry> result = new List<McpCommunicationLogEntry>();
            foreach (var log in logs)
            {
                result.Add(log.ToLogEntry());
            }
            return result;
        }

        /// <summary>
        /// 保留中のリクエスト一覧を取得
        /// </summary>
        public List<PendingRequestInfo> GetPendingRequests()
        {
            return new List<PendingRequestInfo>(pendingRequests);
        }

        /// <summary>
        /// 通信ログを追加
        /// </summary>
        public async Task AddLogAsync(string commandName, DateTime timestamp, string request, string response, bool isError, CancellationToken cancellationToken = default)
        {
            SerializableLogEntry entry = new SerializableLogEntry(commandName, timestamp, request, response, isError);
            logs.Add(entry);

            // 最大数を超えた場合は古いログを削除
            while (logs.Count > maxLogCount)
            {
                logs.RemoveAt(0);
            }

            await SaveSafeAsync(cancellationToken);
        }

        /// <summary>
        /// 保留中のリクエストを追加
        /// </summary>
        public async Task AddPendingRequestAsync(string requestId, string commandName, string request, CancellationToken cancellationToken = default)
        {
            PendingRequestInfo pendingRequest = new PendingRequestInfo(requestId, commandName, request);
            pendingRequests.Add(pendingRequest);
            await SaveSafeAsync(cancellationToken);
        }

        /// <summary>
        /// 保留中のリクエストを削除してログに追加
        /// </summary>
        public async Task CompletePendingRequestAsync(string requestId, string response, bool isError, CancellationToken cancellationToken = default)
        {
            for (int i = 0; i < pendingRequests.Count; i++)
            {
                if (pendingRequests[i].requestId == requestId)
                {
                    PendingRequestInfo pendingRequest = pendingRequests[i];
                    
                    // ログエントリーとして追加
                    DateTime requestTime = DateTime.TryParse(pendingRequest.requestTime, out DateTime result) ? result : DateTime.Now;
                    await AddLogAsync(pendingRequest.commandName, requestTime, pendingRequest.request, response, isError, cancellationToken);
                    
                    // 保留中リストから削除
                    pendingRequests.RemoveAt(i);
                    await SaveSafeAsync(cancellationToken);
                    return;
                }
            }
        }

        /// <summary>
        /// 指定したログエントリーの展開状態を変更
        /// </summary>
        public async Task SetLogExpandedAsync(int index, bool expanded, CancellationToken cancellationToken = default)
        {
            if (index >= 0 && index < logs.Count)
            {
                logs[index].isExpanded = expanded;
                await SaveSafeAsync(cancellationToken);
            }
        }

        /// <summary>
        /// 全てのログをクリア
        /// </summary>
        public async Task ClearAllLogsAsync(CancellationToken cancellationToken = default)
        {
            logs.Clear();
            pendingRequests.Clear();
            await SaveSafeAsync(cancellationToken);
        }

        /// <summary>
        /// 完了したログのみクリア（保留中は保持）
        /// </summary>
        public async Task ClearCompletedLogsAsync(CancellationToken cancellationToken = default)
        {
            logs.Clear();
            await SaveSafeAsync(cancellationToken);
        }

        /// <summary>
        /// デバッグ用の情報を取得
        /// </summary>
        public string GetDebugInfo()
        {
            return $"McpCommunicationLogData: Logs={logs.Count}, PendingRequests={pendingRequests.Count}, MaxLogCount={maxLogCount}";
        }

        /// <summary>
        /// メインスレッドでセーフにSave()を実行する
        /// </summary>
        private async Task SaveSafeAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            if (MainThreadSwitcher.IsMainThread)
            {
                Save(true);
            }
            else
            {
                // 別スレッドからの場合はMainThreadSwitcherでメインスレッドに切り替え
                await MainThreadSwitcher.SwitchToMainThread();
                cancellationToken.ThrowIfCancellationRequested();
                Save(true);
            }
        }
    }
}