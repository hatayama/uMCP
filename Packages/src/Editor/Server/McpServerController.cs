using UnityEditor;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// MCP Serverの状態をSessionStateで管理し、アセンブリリロード時に自動復旧する
    /// </summary>
    [InitializeOnLoad]
    public static class McpServerController
    {
        private const string SESSION_KEY_SERVER_RUNNING = "uMCP.ServerRunning";
        private const string SESSION_KEY_SERVER_PORT = "uMCP.ServerPort";
        private const string SESSION_KEY_AFTER_COMPILE = "uMCP.AfterCompile";
        
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
        public static int ServerPort => mcpServer?.Port ?? McpServerConfig.DEFAULT_PORT;

        static McpServerController()
        {
            // Unity終了時のクリーンアップ登録
            EditorApplication.quitting += OnEditorQuitting;
            
            // アセンブリリロード前の処理
            // AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            
            // アセンブリリロード後の処理
            // AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
            
            // 初期化時にサーバー状態を復旧
            RestoreServerStateIfNeeded();
        }

        /// <summary>
        /// サーバーを開始する
        /// </summary>
        public static void StartServer(int port = McpServerConfig.DEFAULT_PORT)
        {
            // 既存のサーバーを必ず停止する（ポートを解放するため）
            if (mcpServer != null)
            {
                McpLogger.LogInfo($"Stopping existing server before starting new one...");
                StopServer();
                
                // TCP接続が確実に解放されるよう少し待機
                System.Threading.Thread.Sleep(200);
            }

            mcpServer = new McpBridgeServer();
            mcpServer.StartServer(port);
            
            // SessionStateに状態を保存
            SessionState.SetBool(SESSION_KEY_SERVER_RUNNING, true);
            SessionState.SetInt(SESSION_KEY_SERVER_PORT, port);
            
            McpLogger.LogInfo($"Unity MCP Server started on port {port}");
        }

        /// <summary>
        /// サーバーを停止する
        /// </summary>
        public static void StopServer()
        {
            if (mcpServer != null)
            {
                mcpServer.Dispose();
                mcpServer = null;
            }
            
            // SessionStateから状態を削除
            SessionState.EraseBool(SESSION_KEY_SERVER_RUNNING);
            SessionState.EraseInt(SESSION_KEY_SERVER_PORT);
            
            McpLogger.LogInfo("Unity MCP Server stopped");
        }

        /// <summary>
        /// アセンブリリロード前の処理
        /// </summary>
        private static void OnBeforeAssemblyReload()
        {
            McpLogger.LogInfo($"McpServerController.OnBeforeAssemblyReload: IsServerRunning={mcpServer?.IsRunning}");
            
            // サーバーが動作中の場合、状態を保存してサーバーを停止
            if (mcpServer?.IsRunning == true)
            {
                int portToSave = mcpServer.Port;
                
                // SessionState操作を即座に実行（Domain Reload前に確実に保存）
                SessionState.SetBool(SESSION_KEY_SERVER_RUNNING, true);
                SessionState.SetInt(SESSION_KEY_SERVER_PORT, portToSave);
                SessionState.SetBool(SESSION_KEY_AFTER_COMPILE, true); // コンパイル後フラグを設定
                
                McpLogger.LogInfo($"McpServerController.OnBeforeAssemblyReload: Saved server state - port={portToSave}");
                
                // 通信ログも強制的に保存
                McpCommunicationLogger.SaveToSessionState();
                
                // サーバーを完全に停止（TCP接続を確実に解放するためDisposeを使用）
                try
                {
                    mcpServer.Dispose();
                    mcpServer = null;
                    
                    // 少し待機してTCP接続が確実に解放されるようにする
                    System.Threading.Thread.Sleep(100);
                }
                catch (System.Exception ex)
                {
                    McpLogger.LogError($"Error during server shutdown before assembly reload: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// アセンブリリロード後の処理
        /// </summary>
        private static void OnAfterAssemblyReload()
        {
            // MCP経由コンパイルフラグをクリア（念のため）
            SessionState.EraseBool("uMCP.CompileFromMCP");
            
            // サーバー状態を復旧
            RestoreServerStateIfNeeded();
            
            // 保留中のコンパイルリクエストを処理
            ProcessPendingCompileRequests();
        }

        /// <summary>
        /// 必要に応じてサーバー状態を復旧する
        /// </summary>
        private static void RestoreServerStateIfNeeded()
        {
            bool wasRunning = SessionState.GetBool(SESSION_KEY_SERVER_RUNNING, false);
            int savedPort = SessionState.GetInt(SESSION_KEY_SERVER_PORT, McpServerConfig.DEFAULT_PORT);
            bool isAfterCompile = SessionState.GetBool(SESSION_KEY_AFTER_COMPILE, false);
            
            // サーバーが既に起動している場合（McpEditorWindowで起動済みなど）
            if (mcpServer?.IsRunning == true)
            {
                // コンパイル後フラグだけクリアして終了
                if (isAfterCompile)
                {
                    SessionState.EraseBool(SESSION_KEY_AFTER_COMPILE);
                    McpLogger.LogInfo("Server already running. Clearing post-compile flag.");
                }
                return;
            }
            
            // コンパイル後フラグをクリア
            if (isAfterCompile)
            {
                SessionState.EraseBool(SESSION_KEY_AFTER_COMPILE);
            }
            
            if (wasRunning && (mcpServer == null || !mcpServer.IsRunning))
            {
                // コンパイル後の場合は即座に再起動（Auto Start Serverの設定に関わらず）
                if (isAfterCompile)
                {
                    McpLogger.LogInfo("Detected post-compile state. Restoring server immediately...");
                    
                    // 少しだけ待機してからすぐに再起動（TCP解放のため）
                    EditorApplication.delayCall += () =>
                    {
                        TryRestoreServerWithRetry(savedPort, 0);
                    };
                }
                else
                {
                    // Unity起動時など、コンパイル以外の場合
                    // Auto Start Serverの設定を確認
                    bool autoStartEnabled = McpEditorSettings.GetAutoStartServer();
                    
                    if (autoStartEnabled)
                    {
                        McpLogger.LogInfo("Auto Start Server is enabled. Restoring server with delay...");
                        
                        // Auto Start Serverがonの場合は従来通りの遅延処理
                        EditorApplication.delayCall += () =>
                        {
                            // さらに遅延を追加（合計で約200-300ms待機）
                            EditorApplication.delayCall += () =>
                            {
                                TryRestoreServerWithRetry(savedPort, 0);
                            };
                        };
                    }
                    else
                    {
                        // Auto Start Serverがoffの場合はサーバーを起動しない
                        McpLogger.LogInfo("Auto Start Server is disabled. Server will not be restored automatically.");
                        
                        // SessionStateをクリア（手動でサーバーを起動するまで待つ）
                        SessionState.EraseBool(SESSION_KEY_SERVER_RUNNING);
                        SessionState.EraseInt(SESSION_KEY_SERVER_PORT);
                    }
                }
            }
        }

        /// <summary>
        /// サーバーの復旧を再試行付きで実行する
        /// </summary>
        private static void TryRestoreServerWithRetry(int port, int retryCount)
        {
            const int maxRetries = 3;
            
            try
            {
                // 既存のサーバーインスタンスがある場合は確実に停止
                if (mcpServer != null)
                {
                    mcpServer.Dispose();
                    mcpServer = null;
                    System.Threading.Thread.Sleep(200);
                }
                
                mcpServer = new McpBridgeServer();
                mcpServer.StartServer(port);
                
                McpLogger.LogInfo($"Unity MCP Server restored on port {port}");
            }
            catch (System.Exception ex)
            {
                McpLogger.LogError($"Failed to restore MCP Server on port {port} (attempt {retryCount + 1}): {ex.Message}");
                
                // 最大リトライ回数に達していない場合は再試行
                if (retryCount < maxRetries)
                {
                    EditorApplication.delayCall += () =>
                    {
                        // ポート番号は変更せず、同じポートで再試行
                        TryRestoreServerWithRetry(port, retryCount + 1);
                    };
                }
                else
                {
                    // 最終的に失敗した場合はSessionStateをクリア
                    McpLogger.LogError($"Failed to restore MCP Server on port {port} after {maxRetries + 1} attempts. Clearing session state.");
                    SessionState.EraseBool(SESSION_KEY_SERVER_RUNNING);
                    SessionState.EraseInt(SESSION_KEY_SERVER_PORT);
                }
            }
        }

        /// <summary>
        /// Unity終了時のクリーンアップ
        /// </summary>
        private static void OnEditorQuitting()
        {
            StopServer();
        }

        /// <summary>
        /// 保留中のコンパイルリクエストを処理する
        /// </summary>
        private static void ProcessPendingCompileRequests()
        {
            // SessionState操作によるメインスレッドエラーを回避するため一時的に無効化
            // TODO: メインスレッド問題を解決後に再有効化
        }

        /// <summary>
        /// サーバーの状態情報を取得する
        /// </summary>
        public static (bool isRunning, int port, bool wasRestoredFromSession) GetServerStatus()
        {
            bool wasRestored = SessionState.GetBool(SESSION_KEY_SERVER_RUNNING, false);
            return (IsServerRunning, ServerPort, wasRestored);
        }

        /// <summary>
        /// デバッグ用：詳細なサーバー状態を取得する
        /// </summary>
        public static string GetDetailedServerStatus()
        {
            bool sessionWasRunning = SessionState.GetBool(SESSION_KEY_SERVER_RUNNING, false);
            int sessionPort = SessionState.GetInt(SESSION_KEY_SERVER_PORT, McpServerConfig.DEFAULT_PORT);
            bool serverInstanceExists = mcpServer != null;
            bool serverInstanceRunning = mcpServer?.IsRunning ?? false;
            int serverInstancePort = mcpServer?.Port ?? -1;
            
            return $"SessionState: wasRunning={sessionWasRunning}, port={sessionPort}\n" +
                   $"ServerInstance: exists={serverInstanceExists}, running={serverInstanceRunning}, port={serverInstancePort}\n" +
                   $"IsServerRunning={IsServerRunning}, ServerPort={ServerPort}";
        }
    }
} 