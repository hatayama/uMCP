using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEditor;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// MCP Serverの状態をScriptableSingletonで管理し、Domain Reloadに透明的に対応する
    /// 従来の複雑なSessionState管理とAssemblyReloadEvents処理を完全に排除
    /// </summary>
    public static class McpServerController
    {
        private static McpBridgeServer mcpServer;
        
        /// <summary>
        /// 現在のMCPサーバーインスタンス
        /// </summary>
        public static McpBridgeServer CurrentServer => mcpServer;
        
        /// <summary>
        /// サーバーが実行中かどうか
        /// </summary>
        public static bool IsServerRunning => mcpServer?.IsRunning ?? false;
        
        /// <summary>
        /// サーバーのポート番号
        /// </summary>
        public static int ServerPort => mcpServer?.Port ?? McpServerData.instance.ServerPort;

        /// <summary>
        /// 初期化処理
        /// Domain Reload後に自動的に呼ばれる
        /// </summary>
        [InitializeOnLoadMethod]
        static void Initialize()
        {
            // Unity終了時のクリーンアップ登録
            EditorApplication.quitting += OnEditorQuitting;
            
            // ScriptableSingletonからサーバー状態を復旧
            RestoreServerIfNeeded();
        }

        /// <summary>
        /// 必要に応じてサーバーを復旧する
        /// ScriptableSingletonの状態に基づいて自動判断
        /// </summary>
        private static void RestoreServerIfNeeded()
        {
            var serverData = McpServerData.instance;
            
            if (serverData.IsServerRunning)
            {
                McpLogger.LogInfo($"Restoring MCP Server on port {serverData.ServerPort}");
                StartServer(serverData.ServerPort);
            }
            
            // 未完了のコンパイル要求があるかチェック
            ProcessPendingCompileRequests();
        }

        /// <summary>
        /// 未完了のコンパイル要求を処理する
        /// Domain Reload後に呼び出される
        /// </summary>
        private static void ProcessPendingCompileRequests()
        {
            var compileData = McpCompileData.instance;
            string[] pendingRequestIds = compileData.GetPendingRequestIds();
            
            if (pendingRequestIds.Length > 0)
            {
                McpLogger.LogInfo($"Found {pendingRequestIds.Length} pending compile request(s), processing...");
                
                // バックグラウンドで処理（UIブロックを避ける）
                EditorApplication.delayCall += () => ProcessPendingCompileRequestsAsync();
            }
        }

        /// <summary>
        /// 未完了のコンパイル要求を非同期で処理する
        /// </summary>
        private static async void ProcessPendingCompileRequestsAsync()
        {
            var compileData = McpCompileData.instance;
            var pendingRequests = compileData.PendingRequests;
            
            foreach (CompileRequestInfo request in pendingRequests)
            {
                McpLogger.LogInfo($"Processing pending compile request: {request.requestId}");
                
                try
                {
                    // Domain Reload後は既にコンパイルが完了してるので、結果を成功として設定
                    // 実際のエラーチェックはUnityのコンソールやログで確認可能
                    bool compilationSuccess = !EditorUtility.scriptCompilationFailed;
                    
                    var resultObject = new
                    {
                        success = compilationSuccess,
                        errorCount = compilationSuccess ? 0 : 1,
                        warningCount = 0,
                        completedAt = System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                        errors = compilationSuccess ? new object[0] : new[] { new { message = "Compilation failed", file = "", line = 0, column = 0, type = "Error" } },
                        warnings = new object[0]
                    };
                    
                    string resultJson = JsonConvert.SerializeObject(resultObject);
                    compileData.CompleteRequest(request.requestId, compilationSuccess, compilationSuccess ? 0 : 1, 0, resultJson);
                    
                    McpLogger.LogInfo($"Completed pending compile request: {request.requestId} (Success: {compilationSuccess})");
                }
                catch (System.Exception ex)
                {
                    McpLogger.LogError($"Failed to process pending compile request {request.requestId}: {ex.Message}");
                    compileData.FailRequest(request.requestId, ex.Message);
                }
            }
            
            // CompileFromMcpフラグをクリア
            compileData.CompileFromMcp = false;
        }


        /// <summary>
        /// サーバーを開始する
        /// </summary>
        public static void StartServer(int port = McpServerConfig.DEFAULT_PORT)
        {
            // 既存のサーバーを停止
            if (mcpServer != null)
            {
                McpLogger.LogInfo("Stopping existing server before starting new one...");
                StopServer();
                
                // TCP接続が確実に解放されるよう少し待機
                System.Threading.Thread.Sleep(200);
            }

            try
            {
                mcpServer = new McpBridgeServer();
                mcpServer.StartServer(port);
                
                // ScriptableSingletonに状態を保存
                var serverData = McpServerData.instance;
                serverData.ServerPort = port;
                serverData.IsServerRunning = true;
                
                McpLogger.LogInfo($"Unity MCP Server started on port {port}");
            }
            catch (System.Exception ex)
            {
                McpLogger.LogError($"Failed to start MCP Server: {ex.Message}");
                
                // 失敗時は状態をクリア
                var serverData = McpServerData.instance;
                serverData.IsServerRunning = false;
                
                throw;
            }
        }

        /// <summary>
        /// サーバーを停止する
        /// </summary>
        public static void StopServer()
        {
            if (mcpServer != null)
            {
                try
                {
                    mcpServer.Dispose();
                    McpLogger.LogInfo("Unity MCP Server stopped");
                }
                catch (System.Exception ex)
                {
                    McpLogger.LogError($"Error stopping MCP Server: {ex.Message}");
                }
                finally
                {
                    mcpServer = null;
                }
            }
            
            // ScriptableSingletonの状態を更新
            var serverData = McpServerData.instance;
            serverData.IsServerRunning = false;
        }

        /// <summary>
        /// サーバーを再起動する
        /// </summary>
        public static void RestartServer()
        {
            var serverData = McpServerData.instance;
            int currentPort = IsServerRunning ? ServerPort : serverData.ServerPort;
            
            StopServer();
            StartServer(currentPort);
        }

        /// <summary>
        /// 自動起動設定を変更
        /// </summary>
        public static void SetAutoStart(bool enabled)
        {
            var serverData = McpServerData.instance;
            serverData.AutoStartEnabled = enabled;
            
            McpLogger.LogInfo($"Auto start {(enabled ? "enabled" : "disabled")}");
        }

        /// <summary>
        /// 自動起動が有効かどうか
        /// </summary>
        public static bool IsAutoStartEnabled()
        {
            return McpServerData.instance.AutoStartEnabled;
        }

        /// <summary>
        /// サーバー状態のデバッグ情報を取得
        /// </summary>
        public static string GetDebugInfo()
        {
            var serverData = McpServerData.instance;
            return $"McpServerController: " +
                   $"ActualRunning={IsServerRunning}, " +
                   $"DataState={serverData.GetDebugInfo()}";
        }

        /// <summary>
        /// サーバー状態を取得（McpEditorWindow互換）
        /// </summary>
        public static (bool isRunning, int port, bool wasRestored) GetServerStatus()
        {
            var serverData = McpServerData.instance;
            return (IsServerRunning, ServerPort, serverData.IsServerRunning);
        }

        /// <summary>
        /// 詳細なサーバー状態を取得（McpEditorWindow互換）
        /// </summary>
        public static string GetDetailedServerStatus()
        {
            return GetDebugInfo();
        }

        /// <summary>
        /// Unity終了時の処理
        /// </summary>
        private static void OnEditorQuitting()
        {
            McpLogger.LogInfo("Unity is quitting, stopping MCP Server...");
            StopServer();
        }
    }
}