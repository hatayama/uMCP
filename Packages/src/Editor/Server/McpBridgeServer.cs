using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Unity MCP Bridge TCP/IPサーバー
    /// TypeScript MCP Serverからの接続を受け付け、JSON-RPC 2.0通信を行う
    /// </summary>
    public class McpBridgeServer : IDisposable
    {
        private TcpListener tcpListener;
        private CancellationTokenSource cancellationTokenSource;
        private Task serverTask;
        private bool isRunning = false;
        
        /// <summary>
        /// サーバーが実行中かどうか
        /// </summary>
        public bool IsRunning => isRunning;
        
        /// <summary>
        /// サーバーのポート番号
        /// </summary>
        public int Port { get; private set; } = McpServerConfig.DEFAULT_PORT;
        
        /// <summary>
        /// クライアント接続時のイベント
        /// </summary>
        public event Action<string> OnClientConnected;
        
        /// <summary>
        /// クライアント切断時のイベント
        /// </summary>
        public event Action<string> OnClientDisconnected;
        
        /// <summary>
        /// エラー発生時のイベント
        /// </summary>
        public event Action<string> OnError;
        
        /// <summary>
        /// JSON-RPCリクエスト受信時のイベント
        /// </summary>
        public event Action<string, string> OnJsonRpcReceived;

        /// <summary>
        /// 指定されたポートが使用中かどうかをチェックする
        /// </summary>
        /// <param name="port">チェックするポート番号</param>
        /// <returns>ポートが使用中の場合true</returns>
        public static bool IsPortInUse(int port)
        {
            TcpListener tcpListener = null;
            try
            {
                tcpListener = new TcpListener(IPAddress.Loopback, port);
                tcpListener.Start();
                return false; // ポートは使用可能
            }
            catch (SocketException ex)
            {
                // ポートが既に使用されている場合
                if (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
                {
                    return true;
                }
                // その他のソケットエラーも使用中として扱う
                return true;
            }
            catch
            {
                // その他の例外も使用中として扱う
                return true;
            }
            finally
            {
                tcpListener?.Stop();
            }
        }

        /// <summary>
        /// サーバーを開始する
        /// </summary>
        /// <param name="port">ポート番号（デフォルト: 7400）</param>
        public void StartServer(int port = McpServerConfig.DEFAULT_PORT)
        {
            if (isRunning)
            {
                McpLogger.LogWarning("MCP Server is already running");
                return;
            }

            Port = port;
            cancellationTokenSource = new CancellationTokenSource();
            
            try
            {
                tcpListener = new TcpListener(IPAddress.Loopback, Port);
                tcpListener.Start();
                isRunning = true;
                
                serverTask = Task.Run(() => ServerLoop(cancellationTokenSource.Token));
                
                McpLogger.LogInfo($"Unity MCP Server started on port {Port}");
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
            {
                isRunning = false;
                string errorMessage = $"Port {Port} is already in use. Please choose a different port.";
                McpLogger.LogError(errorMessage);
                OnError?.Invoke(errorMessage);
                throw new InvalidOperationException(errorMessage, ex);
            }
            catch (Exception ex)
            {
                isRunning = false;
                string errorMessage = $"Failed to start MCP Server: {ex.Message}";
                McpLogger.LogError(errorMessage);
                OnError?.Invoke(errorMessage);
                throw;
            }
        }

        /// <summary>
        /// サーバーを停止する
        /// </summary>
        public void StopServer()
        {
            if (!isRunning)
            {
                McpLogger.LogWarning("MCP Server is not running");
                return;
            }

            isRunning = false;
            
            cancellationTokenSource?.Cancel();
            tcpListener?.Stop();
            
            serverTask?.Wait(TimeSpan.FromSeconds(McpServerConfig.SHUTDOWN_TIMEOUT_SECONDS));
            
            McpLogger.LogInfo("Unity MCP Server stopped");
        }

        /// <summary>
        /// サーバーのメインループ
        /// </summary>
        private async Task ServerLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && isRunning)
            {
                try
                {
                    TcpClient client = await AcceptTcpClientAsync(tcpListener, cancellationToken);
                    if (client != null)
                    {
                        string clientEndpoint = client.Client.RemoteEndPoint?.ToString() ?? "Unknown";
                        McpLogger.LogClientConnection(clientEndpoint, true);
                        OnClientConnected?.Invoke(clientEndpoint);
                        
                        // クライアント処理を別タスクで実行（fire-and-forget）
                        _ = Task.Run(() => HandleClient(client, cancellationToken));
                    }
                }
                catch (ObjectDisposedException)
                {
                    // サーバー停止時の正常な例外
                    break;
                }
                catch (Exception ex)
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        string errorMessage = $"Server loop error: {ex.Message}";
                        McpLogger.LogError(errorMessage);
                        OnError?.Invoke(errorMessage);
                    }
                }
            }
        }

        /// <summary>
        /// TcpListenerからクライアントを非同期で受け付ける
        /// </summary>
        private async Task<TcpClient> AcceptTcpClientAsync(TcpListener listener, CancellationToken cancellationToken)
        {
            try
            {
                return await Task.Run(() => listener.AcceptTcpClient(), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return null;
            }
        }

        /// <summary>
        /// クライアントとの通信を処理する
        /// </summary>
        private async Task HandleClient(TcpClient client, CancellationToken cancellationToken)
        {
            string clientEndpoint = client.Client.RemoteEndPoint?.ToString() ?? "Unknown";
            
            try
            {
                using (client)
                using (NetworkStream stream = client.GetStream())
                {
                    byte[] buffer = new byte[McpServerConfig.BUFFER_SIZE];
                    
                    while (!cancellationToken.IsCancellationRequested && client.Connected)
                    {
                        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                        
                        if (bytesRead == 0)
                        {
                            break; // クライアント切断
                        }
                        
                        string jsonRequest = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        
                        // デバッグ：受信データを詳細に確認
                        McpLogger.LogDebug($"Raw data received ({bytesRead} bytes): {jsonRequest}");
                        
                        // compileリクエストの場合は特別にログ出力
                        if (jsonRequest.Contains("\"method\":\"compile\""))
                        {
                            McpLogger.LogInfo($"[COMPILE REQUEST DETECTED] Raw JSON: {jsonRequest}");
                        }
                        
                        // 改行文字で分割して複数のリクエストを処理
                        string[] requests = jsonRequest.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                        
                        McpLogger.LogDebug($"Found {requests.Length} request(s) in buffer");
                        
                        foreach (string request in requests)
                        {
                            if (string.IsNullOrWhiteSpace(request)) continue;
                            
                            McpLogger.LogDebug($"Processing request: {request}");
                            
                            OnJsonRpcReceived?.Invoke(clientEndpoint, request);
                            
                            // JSON-RPC処理とレスポンス送信
                            string response = await JsonRpcProcessor.ProcessRequest(request);
                            byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                            await stream.WriteAsync(responseBytes, 0, responseBytes.Length, cancellationToken);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    McpLogger.LogError($"Client handling error: {ex.Message}");
                }
            }
            finally
            {
                McpLogger.LogClientConnection(clientEndpoint, false);
                OnClientDisconnected?.Invoke(clientEndpoint);
            }
        }

        /// <summary>
        /// リソースを解放する
        /// </summary>
        public void Dispose()
        {
            StopServer();
            cancellationTokenSource?.Dispose();
            tcpListener = null;
            serverTask = null;
        }
    }
} 