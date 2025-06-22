using UnityEditor;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Manages the state of the MCP Server with SessionState and automatically restores it on assembly reload.
    /// </summary>
    [InitializeOnLoad]
    public static class McpServerController
    {
        
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
            // Register cleanup for when Unity exits.
            EditorApplication.quitting += OnEditorQuitting;
            
            // Processing before assembly reload.
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            
            // Processing after assembly reload.
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
            
            // Subscribe to command changes for event-based notifications
            UnityCommandRegistry.OnCommandsChanged += OnCommandsChanged;
            
            // Restore server state on initialization.
            RestoreServerStateIfNeeded();
        }

        /// <summary>
        /// Starts the server.
        /// </summary>
        public static void StartServer(int port = McpServerConfig.DEFAULT_PORT)
        {
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
                    McpLogger.LogError($"Error during server shutdown before assembly reload: {ex.Message}");
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
                    
                    // Wait a short while before restarting immediately (to release TCP).
                    EditorApplication.delayCall += () =>
                    {
                        TryRestoreServerWithRetry(savedPort, 0);
                    };
                }
                else
                {
                    // For non-compilation scenarios, such as Unity startup.
                    // Check the Auto Start Server setting.
                    bool autoStartEnabled = McpEditorSettings.GetAutoStartServer();
                    
                    if (autoStartEnabled)
                    {
                        McpLogger.LogInfo("Auto Start Server is enabled. Restoring server with delay...");
                        
                        // If Auto Start Server is on, use the conventional delayed processing.
                        EditorApplication.delayCall += () =>
                        {
                            // Add an additional delay (for a total wait of about 200-300ms).
                            EditorApplication.delayCall += () =>
                            {
                                TryRestoreServerWithRetry(savedPort, 0);
                            };
                        };
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
                // Add multiple delays to allow TypeScript client to reconnect
                EditorApplication.delayCall += () =>
                {
                    EditorApplication.delayCall += () =>
                    {
                        EditorApplication.delayCall += () =>
                        {
                            McpLogger.LogInfo("[DEBUG] Sending commands changed notification after server restoration (with extended delay)");
                            SendCommandsChangedNotification();
                        };
                    };
                };
            }
            catch (System.Exception ex)
            {
                McpLogger.LogError($"Failed to restore MCP Server on port {port} (attempt {retryCount + 1}): {ex.Message}");
                
                // If the maximum number of retries has not been reached, try again.
                if (retryCount < maxRetries)
                {
                    EditorApplication.delayCall += () =>
                    {
                        // Do not change the port number; retry with the same port.
                        TryRestoreServerWithRetry(port, retryCount + 1);
                    };
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
        /// Handles command changes for event-based notifications
        /// </summary>
        private static void OnCommandsChanged()
        {
            McpLogger.LogInfo("[DEBUG] McpServerController.OnCommandsChanged called");
            
            if (!IsServerRunning)
            {
                McpLogger.LogDebug("Server not running, skipping command change notification");
                McpLogger.LogInfo("[DEBUG] OnCommandsChanged skipped - server not running");
                return;
            }
            
            McpLogger.LogInfo("[DEBUG] OnCommandsChanged proceeding - server is running");
            
            // Send notification to TypeScript side
            SendCommandsChangedNotification();
            
            McpLogger.LogInfo("[DEBUG] OnCommandsChanged completed");
        }
        
        /// <summary>
        /// Send commands changed notification to TypeScript side
        /// </summary>
        private static void SendCommandsChangedNotification()
        {
            McpLogger.LogInfo("[DEBUG] SendCommandsChangedNotification called");
            
            try
            {
                // Create JSON-RPC notification (not a request - no response expected)
                var notification = new
                {
                    jsonrpc = McpServerConfig.JSONRPC_VERSION,
                    method = "notifications/tools/list_changed",
                    @params = new
                    {
                        timestamp = System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                        message = "Unity commands have been updated"
                    }
                };
                
                string notificationJson = JsonConvert.SerializeObject(notification);
                McpLogger.LogDebug($"Sending commands changed notification: {notificationJson}");
                McpLogger.LogInfo($"[DEBUG] Notification JSON created: {notificationJson}");
                
                // Send notification through the bridge server
                // Note: This is a notification, not a request, so no response is expected
                if (mcpServer == null)
                {
                    McpLogger.LogInfo("[DEBUG] mcpServer is null, cannot send notification");
                    return;
                }
                
                McpLogger.LogInfo("[DEBUG] Calling mcpServer.SendNotificationToClients");
                mcpServer?.SendNotificationToClients(notificationJson);
                McpLogger.LogInfo("[DEBUG] SendCommandsChangedNotification completed successfully");
            }
            catch (System.Exception ex)
            {
                McpLogger.LogError($"Failed to send commands changed notification: {ex.Message}");
                McpLogger.LogError($"[DEBUG] Exception in SendCommandsChangedNotification: {ex}");
            }
        }
    }
}

// Trigger compile test - timestamp: 2025-06-22 03:30:00 