using UnityEditor;
using Newtonsoft.Json;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Manages the state of the MCP Server with SessionState and automatically restores it on assembly reload.
    /// </summary>
    [InitializeOnLoad]
    public static class McpServerController
    {
        private static readonly int[] CommonSystemPorts = { 80, 443, 21, 22, 23, 25, 53, 110, 143, 993, 995, 3389 };
        
        private static McpBridgeServer mcpServer;
        
        /// <summary>
        /// The current MCP server instance.
        /// </summary>
        public static McpBridgeServer CurrentServer => mcpServer;
        
        /// <summary>
        /// Whether the server is running.
        /// </summary>
        public static bool IsServerRunning => mcpServer?.IsRunning ?? false;
        
        /// <summary>
        /// The server's port number.
        /// </summary>
        public static int ServerPort => mcpServer?.Port ?? McpServerConfig.DEFAULT_PORT;

        static McpServerController()
        {
            McpLogger.LogInfo("McpServerController static constructor called");
            
            // Register cleanup for when Unity exits.
            EditorApplication.quitting += OnEditorQuitting;
            
            // Processing before assembly reload.
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            
            // Processing after assembly reload.
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
            
            // Restore server state on initialization.
            RestoreServerStateIfNeeded();
        }

        /// <summary>
        /// Starts the server.
        /// </summary>
        public static void StartServer(int port = McpServerConfig.DEFAULT_PORT)
        {
            // Validate server configuration before starting
            ValidateServerConfiguration(port);
            
            // Always stop the existing server (to release the port).
            if (mcpServer != null)
            {
                McpLogger.LogInfo($"Stopping existing server before starting new one...");
                StopServer();
                
                // Wait a moment to ensure the TCP connection is properly released.
                System.Threading.Thread.Sleep(200);
            }

            mcpServer = new McpBridgeServer();
            mcpServer.StartServer(port);
            
            // Save the state to SessionState.
            SessionState.SetBool(McpConstants.SESSION_KEY_SERVER_RUNNING, true);
            SessionState.SetInt(McpConstants.SESSION_KEY_SERVER_PORT, port);
            
            McpLogger.LogInfo($"Unity MCP Server started on port {port}");
        }

        /// <summary>
        /// Stops the server.
        /// </summary>
        public static void StopServer()
        {
            if (mcpServer != null)
            {
                mcpServer.Dispose();
                mcpServer = null;
            }
            
            // Delete the state from SessionState.
            SessionState.EraseBool(McpConstants.SESSION_KEY_SERVER_RUNNING);
            SessionState.EraseInt(McpConstants.SESSION_KEY_SERVER_PORT);
            
            McpLogger.LogInfo("Unity MCP Server stopped");
        }

        /// <summary>
        /// Processing before assembly reload.
        /// </summary>
        private static void OnBeforeAssemblyReload()
        {
            McpLogger.LogInfo($"McpServerController.OnBeforeAssemblyReload: IsServerRunning={mcpServer?.IsRunning}");
            
            // Set the domain reload start flag.
            SessionState.SetBool(McpConstants.SESSION_KEY_DOMAIN_RELOAD_IN_PROGRESS, true);
            McpLogger.LogInfo("Domain reload in progress flag set to true");
            
            // If the server is running, save its state and stop it.
            if (mcpServer?.IsRunning == true)
            {
                int portToSave = mcpServer.Port;
                
                // Execute SessionState operations immediately (to ensure they are saved before a domain reload).
                SessionState.SetBool(McpConstants.SESSION_KEY_SERVER_RUNNING, true);
                SessionState.SetInt(McpConstants.SESSION_KEY_SERVER_PORT, portToSave);
                SessionState.SetBool(McpConstants.SESSION_KEY_AFTER_COMPILE, true); // Set the post-compilation flag.
                
                McpLogger.LogInfo($"McpServerController.OnBeforeAssemblyReload: Saved server state - port={portToSave}");
                
                // Force-save the communication log as well.
                McpCommunicationLogger.SaveToSessionState();
                
                // Stop the server completely (using Dispose to ensure the TCP connection is released).
                try
                {
                    mcpServer.Dispose();
                    mcpServer = null;
                    
                    // Wait a moment to ensure the TCP connection is properly released.
                    System.Threading.Thread.Sleep(100);
                }
                catch (System.Exception ex)
                {
                    McpLogger.LogError($"Critical error during server shutdown before assembly reload: {ex.Message}");
                    // Don't suppress this exception - server shutdown failure could leave ports locked
                    // and cause startup issues after domain reload
                    throw new System.InvalidOperationException(
                        $"Failed to properly shutdown MCP server before assembly reload. This may cause port conflicts on restart.", ex);
                }
            }
        }

        /// <summary>
        /// Processing after assembly reload.
        /// </summary>
        private static void OnAfterAssemblyReload()
        {
            // Clear the compile-via-MCP flag (just in case).
            SessionState.EraseBool(McpConstants.SESSION_KEY_COMPILE_FROM_MCP);
            
            // Clear the domain reload completion flag.
            SessionState.EraseBool(McpConstants.SESSION_KEY_DOMAIN_RELOAD_IN_PROGRESS);
            McpLogger.LogInfo("Domain reload in progress flag cleared");
            
            // Restore server state.
            RestoreServerStateIfNeeded();
            
            // Process pending compile requests.
            ProcessPendingCompileRequests();
            
            // Always send command change notification after compilation
            // This ensures schema changes (descriptions, parameters) are communicated to Cursor
            if (IsServerRunning)
            {
                SendCommandNotificationAfterCompilation();
            }
            else
            {
                McpLogger.LogDebug("Server not running, skipping post-compilation command notification");
            }
        }

        /// <summary>
        /// Restores the server state if necessary.
        /// </summary>
        private static void RestoreServerStateIfNeeded()
        {
            bool wasRunning = SessionState.GetBool(McpConstants.SESSION_KEY_SERVER_RUNNING, false);
            int savedPort = SessionState.GetInt(McpConstants.SESSION_KEY_SERVER_PORT, McpServerConfig.DEFAULT_PORT);
            bool isAfterCompile = SessionState.GetBool(McpConstants.SESSION_KEY_AFTER_COMPILE, false);
            
            // If the server is already running (e.g., started from McpEditorWindow).
            if (mcpServer?.IsRunning == true)
            {
                // Just clear the post-compilation flag and exit.
                if (isAfterCompile)
                {
                    SessionState.EraseBool(McpConstants.SESSION_KEY_AFTER_COMPILE);
                    McpLogger.LogInfo("Server already running. Clearing post-compile flag.");
                    
                    // Send notification for post-compilation changes
                    SendCommandNotificationForPostCompile();
                }
                return;
            }
            
            // Clear the post-compilation flag.
            if (isAfterCompile)
            {
                SessionState.EraseBool(McpConstants.SESSION_KEY_AFTER_COMPILE);
            }
            
            if (wasRunning && (mcpServer == null || !mcpServer.IsRunning))
            {
                // If it's after a compilation, restart immediately (regardless of the Auto Start Server setting).
                if (isAfterCompile)
                {
                    McpLogger.LogInfo("Detected post-compile state. Restoring server immediately...");
                    
                    // Wait a short while before restarting immediately (to release TCP port).
                    RestoreServerAfterCompile(savedPort);
                }
                else
                {
                    // For non-compilation scenarios, such as Unity startup.
                    // Check the Auto Start Server setting.
                    bool autoStartEnabled = McpEditorSettings.GetAutoStartServer();
                    
                    if (autoStartEnabled)
                    {
                        McpLogger.LogInfo("Auto Start Server is enabled. Restoring server with delay...");
                        
                        // Wait for Unity Editor to be ready before auto-starting
                        RestoreServerOnStartup(savedPort);
                    }
                    else
                    {
                        // If Auto Start Server is off, do not start the server.
                        McpLogger.LogInfo("Auto Start Server is disabled. Server will not be restored automatically.");
                        
                        // Clear SessionState (wait for the server to be started manually).
                        SessionState.EraseBool(McpConstants.SESSION_KEY_SERVER_RUNNING);
                        SessionState.EraseInt(McpConstants.SESSION_KEY_SERVER_PORT);
                    }
                }
            }
        }

        /// <summary>
        /// Executes server recovery with retries.
        /// </summary>
        private static void TryRestoreServerWithRetry(int port, int retryCount)
        {
            const int maxRetries = 3;
            
            try
            {
                // If there is an existing server instance, ensure it is stopped.
                if (mcpServer != null)
                {
                    mcpServer.Dispose();
                    mcpServer = null;
                    System.Threading.Thread.Sleep(200);
                }
                
                mcpServer = new McpBridgeServer();
                mcpServer.StartServer(port);
                
                McpLogger.LogInfo($"Unity MCP Server restored on port {port}");
                
                // Send commands changed notification after server restoration
                // This ensures TypeScript clients can receive the notification
                SendNotificationAfterRestore();
            }
            catch (System.Exception ex)
            {
                McpLogger.LogError($"Failed to restore MCP Server on port {port} (attempt {retryCount + 1}): {ex.Message}");
                
                // If the maximum number of retries has not been reached, try again.
                if (retryCount < maxRetries)
                {
                    // Wait for port release before retry
                    RetryServerRestore(port, retryCount);
                }
                else
                {
                    // If it ultimately fails, clear the SessionState.
                    McpLogger.LogError($"Failed to restore MCP Server on port {port} after {maxRetries + 1} attempts. Clearing session state.");
                    SessionState.EraseBool(McpConstants.SESSION_KEY_SERVER_RUNNING);
                    SessionState.EraseInt(McpConstants.SESSION_KEY_SERVER_PORT);
                }
            }
        }
        
        /// <summary>
        /// Cleanup on Unity exit.
        /// </summary>
        private static void OnEditorQuitting()
        {
            StopServer();
        }

        /// <summary>
        /// Processes pending compile requests.
        /// </summary>
        private static void ProcessPendingCompileRequests()
        {
            // Temporarily disabled to avoid main thread errors due to SessionState operations.
            // TODO: Re-enable after resolving the main thread issue.
            // CompileSessionState.StartForcedRecompile();
        }

        /// <summary>
        /// Gets server status information.
        /// </summary>
        public static (bool isRunning, int port, bool wasRestoredFromSession) GetServerStatus()
        {
            bool wasRestored = SessionState.GetBool(McpConstants.SESSION_KEY_SERVER_RUNNING, false);
            return (IsServerRunning, ServerPort, wasRestored);
        }

        /// <summary>
        /// For debugging: Gets detailed server status.
        /// </summary>
        public static string GetDetailedServerStatus()
        {
            bool sessionWasRunning = SessionState.GetBool(McpConstants.SESSION_KEY_SERVER_RUNNING, false);
            int sessionPort = SessionState.GetInt(McpConstants.SESSION_KEY_SERVER_PORT, McpServerConfig.DEFAULT_PORT);
            bool serverInstanceExists = mcpServer != null;
            bool serverInstanceRunning = mcpServer?.IsRunning ?? false;
            int serverInstancePort = mcpServer?.Port ?? -1;
            
            return $"SessionState: wasRunning={sessionWasRunning}, port={sessionPort}\n" +
                   $"ServerInstance: exists={serverInstanceExists}, running={serverInstanceRunning}, port={serverInstancePort}\n" +
                   $"IsServerRunning={IsServerRunning}, ServerPort={ServerPort}";
        }

        /// <summary>
        /// Send commands changed notification to TypeScript side
        /// </summary>
        private static void SendCommandsChangedNotification()
        {
            McpLogger.LogInfo("[DEBUG] SendCommandsChangedNotification called");
            
            try
            {
                // Send both notification formats to ensure compatibility
                var notificationParams = new
                {
                    timestamp = System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    message = "Unity commands have been updated"
                };
                
                // 1. Send MCP standard notification
                var mcpNotification = new
                {
                    jsonrpc = McpServerConfig.JSONRPC_VERSION,
                    method = "notifications/tools/list_changed",
                    @params = notificationParams
                };
                
                // 2. Send custom commandsChanged notification
                var customNotification = new
                {
                    jsonrpc = McpServerConfig.JSONRPC_VERSION,
                    method = "commandsChanged",
                    @params = notificationParams
                };
                
                string mcpNotificationJson = JsonConvert.SerializeObject(mcpNotification);
                string customNotificationJson = JsonConvert.SerializeObject(customNotification);
                
                McpLogger.LogInfo($"[DEBUG] MCP Notification JSON: {mcpNotificationJson}");
                McpLogger.LogInfo($"[DEBUG] Custom Notification JSON: {customNotificationJson}");
                
                // Send notification through the bridge server
                if (mcpServer == null)
                {
                    McpLogger.LogInfo("[DEBUG] mcpServer is null, cannot send notification");
                    return;
                }
                
                McpLogger.LogInfo("[DEBUG] Sending both notification formats");
                mcpServer?.SendNotificationToClients(mcpNotificationJson);
                mcpServer?.SendNotificationToClients(customNotificationJson);
                McpLogger.LogInfo("[DEBUG] SendCommandsChangedNotification completed successfully");
            }
            catch (System.Exception ex)
            {
                McpLogger.LogError($"Failed to send commands changed notification: {ex.Message}");
                McpLogger.LogError($"[DEBUG] Exception in SendCommandsChangedNotification: {ex}");
            }
        }

        /// <summary>
        /// Manually trigger command change notification
        /// Public method for external calls (e.g., from UnityCommandRegistry)
        /// </summary>
        public static void TriggerCommandChangeNotification()
        {
            if (IsServerRunning)
            {
                SendCommandsChangedNotification();
            }
            else
            {
                McpLogger.LogDebug("Server not running, skipping command change notification");
            }
        }
        
        /// <summary>
        /// Send command notification after compilation with frame delay
        /// </summary>
        private static async void SendCommandNotificationAfterCompilation()
        {
            // Use frame delay for timing adjustment after domain reload
            // This ensures Unity Editor is in a stable state before sending notifications
            await EditorDelay.DelayFrame(1);
            
            McpLogger.LogInfo("Sending command change notification after compilation completion");
            UnityCommandRegistry.TriggerCommandsChangedNotification();
        }
        
        /// <summary>
        /// Send command notification for post-compilation changes
        /// </summary>
        private static async void SendCommandNotificationForPostCompile()
        {
            // Frame delay for timing adjustment, ensuring stable state after compilation
            await EditorDelay.DelayFrame(1);
            
            McpLogger.LogInfo("Sending command change notification for post-compilation changes (server already running)");
            UnityCommandRegistry.TriggerCommandsChangedNotification();
        }
        
        /// <summary>
        /// Restore server after compilation with frame delay
        /// </summary>
        private static async void RestoreServerAfterCompile(int port)
        {
            // Wait a short while for timing adjustment (TCP port release)
            await EditorDelay.DelayFrame(1);
            
            TryRestoreServerWithRetry(port, 0);
        }
        
        /// <summary>
        /// Restore server on startup with frame delay
        /// </summary>
        private static async void RestoreServerOnStartup(int port)
        {
            // Wait for Unity Editor to be ready before auto-starting
            await EditorDelay.DelayFrame(1);
            
            TryRestoreServerWithRetry(port, 0);
        }
        
        /// <summary>
        /// Send notification after server restore with frame delay
        /// </summary>
        private static async void SendNotificationAfterRestore()
        {
            // Frame delay for timing adjustment, ensuring server is fully ready
            await EditorDelay.DelayFrame(1);
            
            McpLogger.LogInfo("[DEBUG] Sending commands changed notification after server restoration");
            SendCommandsChangedNotification();
        }
        
        /// <summary>
        /// Retry server restore with frame delay
        /// </summary>
        private static async void RetryServerRestore(int port, int retryCount)
        {
            // Wait longer for port release before retry
            await EditorDelay.DelayFrame(5);
            
            // Do not change the port number; retry with the same port
            TryRestoreServerWithRetry(port, retryCount + 1);
        }

        /// <summary>
        /// Validates server configuration before starting
        /// Implements fail-fast behavior for invalid configurations
        /// </summary>
        private static void ValidateServerConfiguration(int port)
        {
            // Validate port number using shared validator
            if (!McpPortValidator.ValidatePort(port, "for MCP server"))
            {
                throw new System.ArgumentOutOfRangeException(nameof(port), 
                    $"Port number must be between 1 and 65535. Received: {port}");
            }

            // Check for commonly used ports that might conflict (server-specific strict validation)
            if (System.Array.IndexOf(CommonSystemPorts, port) != -1)
            {
                throw new System.InvalidOperationException(
                    $"Port {port} is a commonly used system port and should not be used for MCP server. Please choose a different port (e.g., 7400-7500).");
            }

            // Validate Unity Editor state
            if (EditorApplication.isCompiling)
            {
                throw new System.InvalidOperationException(
                    "Cannot start MCP server while Unity is compiling. Please wait for compilation to complete.");
            }

            // Check if port is already in use
            if (McpBridgeServer.IsPortInUse(port))
            {
                throw new System.InvalidOperationException(
                    $"Port {port} is already in use. Please choose a different port or stop the service using this port.");
            }

            McpLogger.LogDebug($"Server configuration validation passed for port {port}");
        }
    }
}
