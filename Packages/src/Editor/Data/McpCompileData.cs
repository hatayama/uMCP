using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// コンパイル要求情報
    /// </summary>
    [Serializable]
    public class CompileRequestInfo
    {
        public string requestId;
        public bool forceRecompile;
        public string clientEndpoint;
        public string requestTime;
        public string status; // "pending", "completed", "failed"
        
        // コンパイル結果情報
        public bool success;
        public int errorCount;
        public int warningCount;
        public string completedAt;
        public string resultJson; // CompileResultをJSONで保存

        public CompileRequestInfo(string requestId, bool forceRecompile, string clientEndpoint)
        {
            this.requestId = requestId;
            this.forceRecompile = forceRecompile;
            this.clientEndpoint = clientEndpoint;
            this.requestTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            this.status = "pending";
            this.success = false;
            this.errorCount = 0;
            this.warningCount = 0;
            this.completedAt = "";
            this.resultJson = "";
        }
    }

    /// <summary>
    /// MCPコンパイル状態を永続化するScriptableSingleton
    /// Domain Reloadに関係なくコンパイル要求を管理する
    /// </summary>
    [FilePath("Library/UnityMcpCompile.asset", FilePathAttribute.Location.ProjectFolder)]
    public class McpCompileData : ScriptableSingleton<McpCompileData>
    {
        [SerializeField] private bool compileFromMcp = false;
        [SerializeField] private List<CompileRequestInfo> pendingRequests = new List<CompileRequestInfo>();
        [SerializeField] private List<CompileRequestInfo> completedRequests = new List<CompileRequestInfo>();
        [SerializeField] private int maxHistoryCount = 50;

        /// <summary>
        /// MCP経由のコンパイルかどうか
        /// </summary>
        public bool CompileFromMcp
        {
            get => compileFromMcp;
            set
            {
                if (compileFromMcp != value)
                {
                    compileFromMcp = value;
                    SaveSafe();
                }
            }
        }

        /// <summary>
        /// 保留中のコンパイル要求一覧
        /// </summary>
        public List<CompileRequestInfo> PendingRequests => new List<CompileRequestInfo>(pendingRequests);

        /// <summary>
        /// 完了したコンパイル要求一覧
        /// </summary>
        public List<CompileRequestInfo> CompletedRequests => new List<CompileRequestInfo>(completedRequests);

        /// <summary>
        /// コンパイル要求を追加
        /// </summary>
        public void AddCompileRequest(string requestId, bool forceRecompile, string clientEndpoint = "unknown")
        {
            CompileRequestInfo requestInfo = new CompileRequestInfo(requestId, forceRecompile, clientEndpoint);
            pendingRequests.Add(requestInfo);
            
            // 履歴が多すぎる場合は古いものを削除
            if (completedRequests.Count > maxHistoryCount)
            {
                completedRequests.RemoveAt(0);
            }
            
            SaveSafe();
            McpLogger.LogDebug($"Added compile request: {requestId}");
        }

        /// <summary>
        /// コンパイル要求を完了としてマーク
        /// </summary>
        public void MarkRequestCompleted(string requestId)
        {
            for (int i = 0; i < pendingRequests.Count; i++)
            {
                if (pendingRequests[i].requestId == requestId)
                {
                    CompileRequestInfo requestInfo = pendingRequests[i];
                    requestInfo.status = "completed";
                    
                    completedRequests.Add(requestInfo);
                    pendingRequests.RemoveAt(i);
                    
                    SaveSafe();
                    McpLogger.LogDebug($"Marked compile request as completed: {requestId}");
                    return;
                }
            }
        }

        /// <summary>
        /// コンパイル結果を保存してリクエストを完了とする
        /// </summary>
        public void CompleteRequest(string requestId, bool success, int errorCount, int warningCount, string resultJson)
        {
            for (int i = 0; i < pendingRequests.Count; i++)
            {
                if (pendingRequests[i].requestId == requestId)
                {
                    CompileRequestInfo requestInfo = pendingRequests[i];
                    requestInfo.status = "completed";
                    requestInfo.success = success;
                    requestInfo.errorCount = errorCount;
                    requestInfo.warningCount = warningCount;
                    requestInfo.completedAt = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                    requestInfo.resultJson = resultJson;
                    
                    completedRequests.Add(requestInfo);
                    pendingRequests.RemoveAt(i);
                    
                    SaveSafe();
                    McpLogger.LogDebug($"Completed compile request: {requestId} (Success: {success})");
                    return;
                }
            }
        }

        /// <summary>
        /// コンパイル要求を失敗としてマーク
        /// </summary>
        public void FailRequest(string requestId, string errorMessage)
        {
            for (int i = 0; i < pendingRequests.Count; i++)
            {
                if (pendingRequests[i].requestId == requestId)
                {
                    CompileRequestInfo requestInfo = pendingRequests[i];
                    requestInfo.status = "failed";
                    requestInfo.resultJson = $"{{\"error\": \"{errorMessage}\"}}";
                    requestInfo.completedAt = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                    
                    completedRequests.Add(requestInfo);
                    pendingRequests.RemoveAt(i);
                    
                    SaveSafe();
                    McpLogger.LogDebug($"Failed compile request: {requestId} - {errorMessage}");
                    return;
                }
            }
        }

        /// <summary>
        /// 指定したIDのコンパイル要求を取得
        /// </summary>
        public CompileRequestInfo GetCompileRequest(string requestId)
        {
            // 保留中から検索
            foreach (var request in pendingRequests)
            {
                if (request.requestId == requestId)
                {
                    return request;
                }
            }
            
            // 完了済みから検索
            foreach (var request in completedRequests)
            {
                if (request.requestId == requestId)
                {
                    return request;
                }
            }
            
            return null;
        }

        /// <summary>
        /// 保留中の要求IDリストを取得
        /// </summary>
        public string[] GetPendingRequestIds()
        {
            string[] ids = new string[pendingRequests.Count];
            for (int i = 0; i < pendingRequests.Count; i++)
            {
                ids[i] = pendingRequests[i].requestId;
            }
            return ids;
        }

        /// <summary>
        /// 全ての状態をクリア
        /// </summary>
        public void ClearAll()
        {
            compileFromMcp = false;
            pendingRequests.Clear();
            completedRequests.Clear();
            SaveSafe();
            McpLogger.LogDebug("Cleared all compile data");
        }

        /// <summary>
        /// 完了した要求のみクリア
        /// </summary>
        public void ClearCompletedRequests()
        {
            completedRequests.Clear();
            SaveSafe();
            McpLogger.LogDebug("Cleared completed compile requests");
        }

        /// <summary>
        /// デバッグ用の情報を取得
        /// </summary>
        public string GetDebugInfo()
        {
            return $"McpCompileData: CompileFromMcp={compileFromMcp}, Pending={pendingRequests.Count}, Completed={completedRequests.Count}";
        }

        /// <summary>
        /// メインスレッドでセーフにSave()を実行する
        /// </summary>
        private void SaveSafe()
        {
            if (MainThreadSwitcher.IsMainThread)
            {
                Save(true);
            }
            else
            {
                // 別スレッドからの場合はUnitySynchronizationContextを使って同期実行
                bool completed = false;
                System.Exception thrownException = null;
                
                MainThreadSwitcher.UnitySynchronizationContext.Post(_ =>
                {
                    try
                    {
                        Save(true);
                    }
                    catch (System.Exception ex)
                    {
                        thrownException = ex;
                    }
                    finally
                    {
                        completed = true;
                    }
                }, null);
                
                // 完了まで待機
                while (!completed)
                {
                    System.Threading.Thread.Sleep(1);
                }
                
                if (thrownException != null)
                {
                    throw thrownException;
                }
            }
        }
    }
}