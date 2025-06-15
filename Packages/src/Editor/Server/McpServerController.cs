using UnityEditor;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// MCP Serverの状態をSessionStateで管理し、アセンブリリロード時に自動復旧する
    /// </summary>
    [InitializeOnLoad]
    public static class McpServerController
    {
        private const string SESSION_KEY_SERVER_RUNNING = "UnityMCP.ServerRunning";
        private const string SESSION_KEY_SERVER_PORT = "UnityMCP.ServerPort";
        
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
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            
            // アセンブリリロード後の処理
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
            
            // 初期化時にサーバー状態を復旧
            RestoreServerStateIfNeeded();
        }

        /// <summary>
        /// サーバーを開始する
        /// </summary>
        public static void StartServer(int port = McpServerConfig.DEFAULT_PORT)
        {
            if (mcpServer?.IsRunning == true)
            {
                McpLogger.LogWarning("MCP Server is already running");
                return;
            }

            // 既存のサーバーをクリーンアップ
            StopServer();

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
            // サーバーが動作中の場合、状態を保存してサーバーを停止
            if (mcpServer?.IsRunning == true)
            {
                int portToSave = mcpServer.Port;
                
                // SessionState操作を即座に実行（Domain Reload前に確実に保存）
                SessionState.SetBool(SESSION_KEY_SERVER_RUNNING, true);
                SessionState.SetInt(SESSION_KEY_SERVER_PORT, portToSave);
                
                // 通信ログも強制的に保存
                McpCommunicationLogger.SaveToSessionState();
                
                // サーバーを停止（Disposeは呼ばない、リロード後に復旧するため）
                mcpServer?.StopServer();
            }
        }

        /// <summary>
        /// アセンブリリロード後の処理
        /// </summary>
        private static void OnAfterAssemblyReload()
        {
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
            
            if (wasRunning && (mcpServer == null || !mcpServer.IsRunning))
            {
                mcpServer = new McpBridgeServer();
                mcpServer.StartServer(savedPort);
                
                McpLogger.LogInfo($"Unity MCP Server restored on port {savedPort}");
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
    }
} 