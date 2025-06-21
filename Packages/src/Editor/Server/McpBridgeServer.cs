using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Unity MCP Bridge TCP/IP Server.
    /// Accepts connections from the TypeScript MCP Server and handles JSON-RPC 2.0 communication.
    /// </summary>
    public class McpBridgeServer : IDisposable
    {
        // SessionState key constant.
        private const string SESSION_KEY_DOMAIN_RELOAD = "uMCP.DomainReloadInProgress";
        
        private TcpListener tcpListener;
        private CancellationTokenSource cancellationTokenSource;
        private Task serverTask;
        private bool isRunning = false;
        
        /// <summary>
        /// Whether the server is running.
        /// </summary>
        public bool IsRunning => isRunning;
        
        /// <summary>
        /// The server's port number.
        /// </summary>
        public int Port { get; private set; } = McpServerConfig.DEFAULT_PORT;
        
        /// <summary>
        /// Event on client connection.
        /// </summary>
        public event Action<string> OnClientConnected;
        
        /// <summary>
        /// Event on client disconnection.
        /// </summary>
        public event Action<string> OnClientDisconnected;
        
        /// <summary>
        /// Event on error.
        /// </summary>
        public event Action<string> OnError;

        /// <summary>
        /// Checks if the specified port is in use.
        /// </summary>
        /// <param name="port">The port number to check.</param>
        /// <returns>True if the port is in use.</returns>
        public static bool IsPortInUse(int port)
        {
            TcpListener tcpListener = null;
            try
            {
                tcpListener = new TcpListener(IPAddress.Loopback, port);
                tcpListener.Start();
                return false; // The port is available.
            }
            catch (SocketException ex)
            {
                // If the port is already in use.
                if (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
                {
                    return true;
                }
                // Treat other socket errors as "in use" as well.
                return true;
            }
            catch
            {
                // Treat other exceptions as "in use" as well.
                return true;
            }
            finally
            {
                tcpListener?.Stop();
            }
        }

        /// <summary>
        /// Starts the server.
        /// </summary>
        /// <param name="port">The port number (default: 7400).</param>
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
        /// Stops the server.
        /// </summary>
        public void StopServer()
        {
            if (!isRunning)
            {
                McpLogger.LogWarning("MCP Server is not running");
                return;
            }

            McpLogger.LogInfo("Stopping Unity MCP Server...");
            isRunning = false;
            
            // Request cancellation.
            cancellationTokenSource?.Cancel();
            
            // Stop the TCP listener.
            try
            {
                tcpListener?.Stop();
            }
            catch (Exception ex)
            {
                McpLogger.LogError($"Error stopping TcpListener: {ex.Message}");
            }
            
            // Wait for the server task to complete.
            try
            {
                serverTask?.Wait(TimeSpan.FromSeconds(McpServerConfig.SHUTDOWN_TIMEOUT_SECONDS));
            }
            catch (Exception ex)
            {
                McpLogger.LogError($"Error waiting for server task completion: {ex.Message}");
            }
            
            // Dispose of the cancellation token source.
            try
            {
                cancellationTokenSource?.Dispose();
                cancellationTokenSource = null;
            }
            catch (Exception ex)
            {
                McpLogger.LogError($"Error disposing CancellationTokenSource: {ex.Message}");
            }
            
            // Set the TCP listener to null.
            tcpListener = null;
            serverTask = null;
            
            McpLogger.LogInfo("Unity MCP Server stopped");
        }

        /// <summary>
        /// The server's main loop.
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
                        string clientEndpoint = client.Client.RemoteEndPoint?.ToString() ?? McpServerConfig.UNKNOWN_CLIENT_ENDPOINT;
                        McpLogger.LogClientConnection(clientEndpoint, true);
                        OnClientConnected?.Invoke(clientEndpoint);
                        
                        // Execute client handling in a separate task (fire-and-forget).
                        _ = Task.Run(() => HandleClient(client, cancellationToken));
                    }
                }
                catch (ObjectDisposedException)
                {
                    // Normal exception when stopping the server.
                    break;
                }
                catch (ThreadAbortException ex)
                {
                    // Treat as normal behavior if a domain reload is in progress.
                    if (SessionState.GetBool(SESSION_KEY_DOMAIN_RELOAD, false))
                    {
                        McpLogger.LogInfo("Server thread aborted during domain reload (normal behavior)");
                    }
                    else
                    {
                        McpLogger.LogError($"Unexpected thread abort in server loop: {ex.Message}");
                        OnError?.Invoke($"Unexpected thread abort: {ex.Message}");
                    }
                    break; // Exit the server loop.
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
        /// Asynchronously accepts a client from the TcpListener.
        /// </summary>
        private async Task<TcpClient> AcceptTcpClientAsync(TcpListener listener, CancellationToken cancellationToken)
        {
            try
            {
                return await Task.Run(() => listener.AcceptTcpClient(), cancellationToken);
            }
            catch (ThreadAbortException ex)
            {
                // Treat as normal behavior if a domain reload is in progress.
                if (SessionState.GetBool(SESSION_KEY_DOMAIN_RELOAD, false))
                {
                    McpLogger.LogDebug($"AcceptTcpClientAsync aborted during domain reload (expected): {ex.Message}");
                }
                else
                {
                    McpLogger.LogError($"Unexpected thread abort in AcceptTcpClient: {ex.Message}");
                }
                return null;
            }
            catch (OperationCanceledException)
            {
                return null;
            }
        }

        /// <summary>
        /// Handles communication with the client.
        /// </summary>
        private async Task HandleClient(TcpClient client, CancellationToken cancellationToken)
        {
            string clientEndpoint = client.Client.RemoteEndPoint?.ToString() ?? McpServerConfig.UNKNOWN_CLIENT_ENDPOINT;
            
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
                            break; // Client disconnected.
                        }
                        
                        string receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        
                        // Debug: Check received data in detail.
                        McpLogger.LogDebug($"Received raw from {clientEndpoint}: \"{receivedData.Replace("\n", "\\n")}\"");

                        // Special logging for compile requests.
                        if (receivedData.Contains("\"method\":\"compile\""))
                        {
                             McpLogger.LogInfo($"<<<< Received COMPILE request from {clientEndpoint}");
                        }

                        // Split by newline characters to process multiple requests.
                        string[] requests = receivedData.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                        foreach (string requestJson in requests)
                        {
                            if (string.IsNullOrWhiteSpace(requestJson)) continue;
                            
                            // JSON-RPC processing and response sending.
                            string responseJson = await JsonRpcProcessor.ProcessRequest(requestJson);
                            
                            byte[] responseData = Encoding.UTF8.GetBytes(responseJson + "\n");
                            await stream.WriteAsync(responseData, 0, responseData.Length, cancellationToken);
                        }
                    }
                }
            }
            catch (ThreadAbortException ex)
            {
                // Treat as normal behavior if a domain reload is in progress.
                if (SessionState.GetBool(SESSION_KEY_DOMAIN_RELOAD, false))
                {
                    McpLogger.LogInfo("Client handling thread aborted during domain reload.");
                }
                else
                {
                    McpLogger.LogWarning($"Client handling thread for {clientEndpoint} was aborted.");
                }
            }
            catch (IOException ex)
            {
                McpLogger.LogWarning($"I/O error with client {clientEndpoint}: {ex.Message}");
            }
            catch (Exception ex)
            {
                McpLogger.LogError($"Error handling client {clientEndpoint}: {ex.Message}\n{ex.StackTrace}");
                OnError?.Invoke(ex.Message);
            }
            finally
            {
                client.Close();
                McpLogger.LogClientConnection(clientEndpoint, false);
                OnClientDisconnected?.Invoke(clientEndpoint);
            }
        }

        /// <summary>
        /// Releases resources.
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