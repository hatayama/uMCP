using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using Newtonsoft.Json;

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
        
        // Client management for broadcasting notifications
        private readonly ConcurrentDictionary<string, NetworkStream> connectedClients = new ConcurrentDictionary<string, NetworkStream>();
        
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
                    // Add client to connected clients for notification broadcasting
                    connectedClients.TryAdd(clientEndpoint, stream);
                    McpLogger.LogDebug($"Client {clientEndpoint} added to notification list");
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
            catch (ThreadAbortException)
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
                // Remove client from connected clients list
                connectedClients.TryRemove(clientEndpoint, out _);
                McpLogger.LogDebug($"Client {clientEndpoint} removed from notification list");
                
                client.Close();
                McpLogger.LogClientConnection(clientEndpoint, false);
                OnClientDisconnected?.Invoke(clientEndpoint);
            }
        }

        /// <summary>
        /// Sends a JSON-RPC notification to all connected clients.
        /// </summary>
        /// <param name="method">The notification method name</param>
        /// <param name="parameters">The notification parameters (optional)</param>
        public async void SendNotificationToClients(string method, object parameters = null)
        {
            if (connectedClients.IsEmpty)
            {
                McpLogger.LogDebug($"No connected clients to send notification: {method}");
                return;
            }

            // Create JSON-RPC notification
            object notification = new
            {
                jsonrpc = "2.0",
                method = method,
                @params = parameters
            };

            string notificationJson = JsonConvert.SerializeObject(notification) + "\n";
            byte[] notificationData = Encoding.UTF8.GetBytes(notificationJson);

            McpLogger.LogInfo($"Broadcasting notification '{method}' to {connectedClients.Count} clients");

            await SendNotificationData(notificationData);
        }

        /// <summary>
        /// Sends a pre-formatted JSON-RPC notification to all connected clients.
        /// </summary>
        /// <param name="notificationJson">The complete JSON-RPC notification string</param>
        public async void SendNotificationToClients(string notificationJson)
        {
            McpLogger.LogInfo($"[DEBUG] SendNotificationToClients called with {connectedClients.Count} clients");
            
            if (connectedClients.IsEmpty)
            {
                McpLogger.LogDebug("No connected clients to send notification");
                McpLogger.LogInfo("[DEBUG] connectedClients is empty - no clients to notify");
                return;
            }

            // Log details of connected clients
            McpLogger.LogInfo($"[DEBUG] Connected clients details:");
            foreach (KeyValuePair<string, NetworkStream> client in connectedClients)
            {
                bool canWrite = client.Value?.CanWrite ?? false;
                McpLogger.LogInfo($"[DEBUG] - Client: {client.Key}, CanWrite: {canWrite}");
            }

            // Ensure the JSON ends with a newline
            if (!notificationJson.EndsWith("\n"))
            {
                notificationJson += "\n";
            }

            byte[] notificationData = Encoding.UTF8.GetBytes(notificationJson);
            McpLogger.LogInfo($"Broadcasting pre-formatted notification to {connectedClients.Count} clients");
            McpLogger.LogInfo($"[DEBUG] Notification data size: {notificationData.Length} bytes");

            await SendNotificationData(notificationData);
        }

        /// <summary>
        /// Common method to send notification data to all connected clients
        /// </summary>
        private async Task SendNotificationData(byte[] notificationData)
        {
            McpLogger.LogInfo($"[DEBUG] SendNotificationData called - sending to {connectedClients.Count} clients");
            
            // Send to all connected clients
            List<string> clientsToRemove = new List<string>();
            int successCount = 0;
            int failureCount = 0;
            
            foreach (KeyValuePair<string, NetworkStream> client in connectedClients)
            {
                try
                {
                    if (client.Value.CanWrite)
                    {
                        await client.Value.WriteAsync(notificationData, 0, notificationData.Length);
                        McpLogger.LogDebug($"Notification sent to client: {client.Key}");
                        McpLogger.LogInfo($"[DEBUG] Successfully sent notification to client: {client.Key}");
                        successCount++;
                    }
                    else
                    {
                        McpLogger.LogInfo($"[DEBUG] Client {client.Key} cannot write - marking for removal");
                        clientsToRemove.Add(client.Key);
                        failureCount++;
                    }
                }
                catch (Exception ex)
                {
                    McpLogger.LogWarning($"Failed to send notification to client {client.Key}: {ex.Message}");
                    McpLogger.LogInfo($"[DEBUG] Exception sending to client {client.Key}: {ex}");
                    clientsToRemove.Add(client.Key);
                    failureCount++;
                }
            }

            McpLogger.LogInfo($"[DEBUG] Notification sending completed - Success: {successCount}, Failures: {failureCount}");

            // Remove disconnected clients
            foreach (string clientKey in clientsToRemove)
            {
                connectedClients.TryRemove(clientKey, out _);
                McpLogger.LogDebug($"Removed disconnected client: {clientKey}");
                McpLogger.LogInfo($"[DEBUG] Removed disconnected client: {clientKey}");
            }
            
            McpLogger.LogInfo($"[DEBUG] SendNotificationData completed - Remaining clients: {connectedClients.Count}");
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