using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
    /// Immutable connected client information
    /// </summary>
    public readonly struct ConnectedClient
    {
        public readonly string Endpoint;
        public readonly string ClientName; 
        public readonly DateTime ConnectedAt;
        public readonly NetworkStream Stream;
        public readonly int ProcessId;

        public ConnectedClient(string endpoint, NetworkStream stream, int processId, string clientName = "Unknown Client")
        {
            Endpoint = endpoint;
            Stream = stream;
            ProcessId = processId;
            ClientName = clientName;
            ConnectedAt = DateTime.Now;
        }
        
        public ConnectedClient WithClientName(string clientName)
        {
            return new ConnectedClient(Endpoint, Stream, ProcessId, clientName);
        }
    }

    /// <summary>
    /// Immutable JSON-RPC notification structure
    /// </summary>
    internal class JsonRpcNotification
    {
        public readonly string JsonRpc;
        public readonly string Method;
        public readonly object Params;
        
        public JsonRpcNotification(string jsonRpc, string method, object parameters)
        {
            JsonRpc = jsonRpc;
            Method = method;
            Params = parameters;
        }
    }

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
        private readonly ConcurrentDictionary<string, ConnectedClient> connectedClients = new ConcurrentDictionary<string, ConnectedClient>();
        
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
        /// Get list of connected clients
        /// </summary>
        public IReadOnlyCollection<ConnectedClient> GetConnectedClients()
        {
            return connectedClients.Values.ToArray();
        }

        /// <summary>
        /// Update client name for a connected client
        /// </summary>
        public void UpdateClientName(string clientEndpoint, string clientName)
        {
            if (connectedClients.TryGetValue(clientEndpoint, out ConnectedClient existingClient))
            {
                ConnectedClient updatedClient = existingClient.WithClientName(clientName);
                connectedClients.TryUpdate(clientEndpoint, updatedClient, existingClient);
                McpLogger.LogInfo($"Updated client name for {clientEndpoint}: {clientName}");
            }
        }

        /// <summary>
        /// Get the process ID of the client connected to this socket
        /// </summary>
        private int GetClientProcessId(Socket clientSocket)
        {
            if (clientSocket?.RemoteEndPoint is not IPEndPoint remoteEndPoint)
            {
                return -1;
            }
            
            int remotePort = remoteEndPoint.Port;
            
            // Use lsof command on macOS/Linux to find the process ID
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "lsof",
                Arguments = $"-i :{remotePort}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            
            using (Process process = Process.Start(startInfo))
            {
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                
                string[] lines = output.Split('\n');
                foreach (string line in lines)
                {
                    if (line.Contains($":{remotePort}") && !line.StartsWith("COMMAND"))
                    {
                        string[] parts = line.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2 && int.TryParse(parts[1], out int pid))
                        {
                            return pid;
                        }
                    }
                }
            }
            
            return -1; // Process ID not found
        }

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
                    if (!SessionState.GetBool(SESSION_KEY_DOMAIN_RELOAD, false))
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
                if (!SessionState.GetBool(SESSION_KEY_DOMAIN_RELOAD, false))
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
                    // Get client process ID from remote endpoint
                    int processId = GetClientProcessId(client.Client);
                    
                    // Add client to connected clients for notification broadcasting
                    ConnectedClient connectedClient = new ConnectedClient(clientEndpoint, stream, processId);
                    connectedClients.TryAdd(clientEndpoint, connectedClient);
                    byte[] buffer = new byte[McpServerConfig.BUFFER_SIZE];
                    string incompleteJson = string.Empty; // Buffer for incomplete JSON
                    
                    while (!cancellationToken.IsCancellationRequested && client.Connected)
                    {
                        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                        
                        if (bytesRead == 0)
                        {
                            break; // Client disconnected.
                        }
                        
                        string receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        


                        // Combine with any incomplete JSON from previous buffer
                        string dataToProcess = incompleteJson + receivedData;
                        incompleteJson = string.Empty;

                        // Extract complete JSON messages
                        string[] completeJsonMessages = ExtractCompleteJsonMessages(dataToProcess, out incompleteJson);

                        foreach (string requestJson in completeJsonMessages)
                        {
                            if (string.IsNullOrWhiteSpace(requestJson)) continue;
                            
                            // JSON-RPC processing and response sending.
                            string responseJson = await JsonRpcProcessor.ProcessRequest(requestJson);
                            
                            // Only send response if it's not null (notifications return null)
                            if (!string.IsNullOrEmpty(responseJson))
                            {
                                byte[] responseData = Encoding.UTF8.GetBytes(responseJson + "\n");
                                await stream.WriteAsync(responseData, 0, responseData.Length, cancellationToken);
                            }
                        }
                    }
                }
            }
            catch (ThreadAbortException)
            {
                // Treat as normal behavior if a domain reload is in progress.
                // No need to log thread aborts during domain reload
            }
            catch (IOException ex)
            {
                // I/O errors are usually normal disconnections - only log warnings for unexpected errors
                if (!ex.Message.Contains("Connection reset by peer") && !ex.Message.Contains("socket has been shut down"))
                {
                    McpLogger.LogWarning($"I/O error with client {clientEndpoint}: {ex.Message}");
                }
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
                
                client.Close();
                OnClientDisconnected?.Invoke(clientEndpoint);
            }
        }

        /// <summary>
        /// Sends a JSON-RPC notification to all connected clients.
        /// </summary>
        /// <param name="method">The notification method name</param>
        /// <param name="parameters">The notification parameters (optional)</param>
        public async Task SendNotificationToClients(string method, object parameters = null)
        {
            if (connectedClients.IsEmpty)
            {
                return;
            }

            // Create JSON-RPC notification
            JsonRpcNotification notification = new JsonRpcNotification("2.0", method, parameters);

            string notificationJson = JsonConvert.SerializeObject(notification) + "\n";
            byte[] notificationData = Encoding.UTF8.GetBytes(notificationJson);


            await SendNotificationData(notificationData);
        }

        /// <summary>
        /// Sends a pre-formatted JSON-RPC notification to all connected clients.
        /// </summary>
        /// <param name="notificationJson">The complete JSON-RPC notification string</param>
        public async Task SendNotificationToClients(string notificationJson)
        {
            if (connectedClients.IsEmpty)
            {
                return;
            }

            // Ensure the JSON ends with a newline
            if (!notificationJson.EndsWith("\n"))
            {
                notificationJson += "\n";
            }

            byte[] notificationData = Encoding.UTF8.GetBytes(notificationJson);

            await SendNotificationData(notificationData);
        }

        /// <summary>
        /// Send notification data to all connected clients
        /// </summary>
        private async Task SendNotificationData(byte[] notificationData)
        {
            List<string> clientsToRemove = new List<string>();
            
            foreach (KeyValuePair<string, ConnectedClient> client in connectedClients)
            {
                try
                {
                    if (client.Value.Stream?.CanWrite == true)
                    {
                        await client.Value.Stream.WriteAsync(notificationData, 0, notificationData.Length);
                    }
                    else
                    {
                        clientsToRemove.Add(client.Key);
                    }
                }
                catch
                {
                    // Any error means client should be removed
                    clientsToRemove.Add(client.Key);
                }
            }
            
            // Remove disconnected clients
            foreach (string clientKey in clientsToRemove)
            {
                connectedClients.TryRemove(clientKey, out _);
            }
        }

        /// <summary>
        /// Extract complete JSON messages from received data using JsonTextReader
        /// Handles both objects and arrays, with proper Unicode and escape sequence support
        /// </summary>
        private string[] ExtractCompleteJsonMessages(string data, out string remainingIncomplete)
        {
            List<string> completeMessages = new List<string>();
            remainingIncomplete = string.Empty;
            
            if (string.IsNullOrEmpty(data))
            {
                return completeMessages.ToArray();
            }
            
            // Split by lines first, then process each potential JSON
            string[] lines = data.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            StringBuilder accumulatedJson = new StringBuilder();
            
            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine))
                    continue;
                
                accumulatedJson.AppendLine(trimmedLine);
                string jsonCandidate = accumulatedJson.ToString().Trim();
                
                // Try to parse as complete JSON using Newtonsoft
                if (IsCompleteJson(jsonCandidate))
                {
                    completeMessages.Add(jsonCandidate);
                    accumulatedJson.Clear();
                }
            }
            
            // Any remaining accumulated data is incomplete
            if (accumulatedJson.Length > 0)
            {
                remainingIncomplete = accumulatedJson.ToString().Trim();
            }
            
            return completeMessages.ToArray();
        }
        
        /// <summary>
        /// Check if a string contains a complete, valid JSON structure
        /// </summary>
        private bool IsCompleteJson(string jsonString)
        {
            if (string.IsNullOrWhiteSpace(jsonString))
                return false;
                
            try
            {
                using (var stringReader = new StringReader(jsonString))
                using (var jsonReader = new JsonTextReader(stringReader))
                {
                    // Try to parse the entire string
                    while (jsonReader.Read())
                    {
                        // JsonTextReader will throw if JSON is malformed or incomplete
                    }
                    return true;
                }
            }
            catch (JsonReaderException)
            {
                // Incomplete or malformed JSON
                return false;
            }
            catch (Exception)
            {
                // Unexpected error validating JSON
                return false;
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