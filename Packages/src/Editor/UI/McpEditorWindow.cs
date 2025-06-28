using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace io.github.hatayama.uMCP
{
    // Related classes:
    // - McpServerController: Manages the server lifecycle.
    // - McpBridgeServer: The core TCP server implementation.
    // - McpConfigService: Handles configuration for different IDEs.
    /// <summary>
    /// Editor Window for controlling Unity MCP Server
    /// Displays server status and handles start/stop operations
    /// </summary>
    public class McpEditorWindow : EditorWindow
    {
        // Constants for SessionState

        
        // UI constants - moved to McpUIConstants class
        
        // UI state
        private int customPort = McpServerConfig.DEFAULT_PORT;
        private bool autoStartServer = false;
        private bool showLLMToolSettings = true;
        private bool showConnectedTools = true;
#if UMCP_DEBUG
        private bool showDeveloperTools = false;
        private bool enableCommunicationLogs = false;
        private bool showCommunicationLogs = false;
        private bool enableMcpLogs = false;
        private bool enableDevelopmentMode = false;
        private Vector2 communicationLogScrollPosition;
        private float communicationLogHeight = McpUIConstants.DEFAULT_COMMUNICATION_LOG_HEIGHT; // Height of resizable communication log area
        private bool isResizingCommunicationLog = false; // Whether currently resizing
#endif
        
        // UI state for editor selection
        private McpEditorType selectedEditorType = McpEditorType.Cursor;
        
        // Manage scroll positions for each log entry
        private Dictionary<string, Vector2> requestScrollPositions = new Dictionary<string, Vector2>();
        private Dictionary<string, Vector2> responseScrollPositions = new Dictionary<string, Vector2>();
        
        // Scroll position for entire window
        private Vector2 mainScrollPosition;
        
        // UI update management
        private bool needsRepaint = false;
        private bool isPostCompileMode = false; // True after compilation, false when clients connect
        
        // Server state tracking for change detection
        private bool lastServerRunning = false;
        private int lastServerPort = 0;
        private int lastConnectedClientsCount = 0;
        private string lastClientsInfoHash = "";
        
        // Configuration services
        private readonly McpConfigRepository _repository = new(McpEditorType.Cursor);
        private McpConfigService _cursorConfigService;
        private McpConfigService _claudeCodeConfigService;
        private McpConfigService _vscodeConfigService;
        private McpConfigService _geminiCLIConfigService;
        private McpConfigService _mcpInspectorConfigService;
        
        // View layer
        private McpEditorWindowView _view;
        
        [MenuItem("Window/uMCP")]
        public static void ShowWindow()
        {
            McpEditorWindow window = GetWindow<McpEditorWindow>(McpConstants.PROJECT_NAME);
            window.minSize = new Vector2(McpUIConstants.MIN_WINDOW_WIDTH, McpUIConstants.MIN_WINDOW_HEIGHT);
            window.Show();
        }

        private void OnEnable()
        {
            // Initialize view
            _view = new McpEditorWindowView();
            
            // Initialize configuration services
            McpConfigRepository cursorRepository = new(McpEditorType.Cursor);
            McpConfigRepository claudeCodeRepository = new(McpEditorType.ClaudeCode);
            McpConfigRepository vscodeRepository = new(McpEditorType.VSCode);
            McpConfigRepository geminiCLIRepository = new(McpEditorType.GeminiCLI);
            McpConfigRepository mcpInspectorRepository = new(McpEditorType.McpInspector);
            _cursorConfigService = new McpConfigService(cursorRepository, McpEditorType.Cursor);
            _claudeCodeConfigService = new McpConfigService(claudeCodeRepository, McpEditorType.ClaudeCode);
            _vscodeConfigService = new McpConfigService(vscodeRepository, McpEditorType.VSCode);
            _geminiCLIConfigService = new McpConfigService(geminiCLIRepository, McpEditorType.GeminiCLI);
            _mcpInspectorConfigService = new McpConfigService(mcpInspectorRepository, McpEditorType.McpInspector);
            
            // Load saved settings
            McpEditorSettingsData settings = McpEditorSettings.GetSettings();
            customPort = settings.customPort;
            autoStartServer = settings.autoStartServer;
#if UMCP_DEBUG
            showDeveloperTools = settings.showDeveloperTools;
            enableMcpLogs = settings.enableMcpLogs;
            enableCommunicationLogs = settings.enableCommunicationLogs;
            enableDevelopmentMode = settings.enableDevelopmentMode;
#endif
            
            // Restore editor selection state
            selectedEditorType = (McpEditorType)SessionState.GetInt(McpConstants.SESSION_KEY_SELECTED_EDITOR_TYPE, (int)McpEditorType.Cursor);
            
#if UMCP_DEBUG
            // Synchronize McpLogger settings
            McpLogger.EnableDebugLog = enableMcpLogs;
            
            // Restore communication log area height (from SessionState)
            communicationLogHeight = SessionState.GetFloat(McpConstants.SESSION_KEY_COMMUNICATION_LOG_HEIGHT, McpUIConstants.DEFAULT_COMMUNICATION_LOG_HEIGHT);
#endif
            
            // Subscribe to log update events
            McpCommunicationLogger.OnLogUpdated += Repaint;
            
            // Subscribe to client disconnection events for immediate UI updates
            SubscribeToServerEvents();
            
            // Subscribe to update events for UI refresh
            EditorApplication.update += OnEditorUpdate;
            
            // Enable post-compile mode after domain reload - always on after OnEnable
            isPostCompileMode = true;
            needsRepaint = true;
            
            // Clear reconnecting UI flag on domain reload to ensure proper state
            SessionState.SetBool(McpConstants.SESSION_KEY_SHOW_RECONNECTING_UI, false);
            
            UnityEngine.Debug.Log("[McpEditorWindow] Post-compile mode enabled");
            
            // Check if after compilation
            bool isAfterCompile = SessionState.GetBool(McpConstants.SESSION_KEY_AFTER_COMPILE, false);
            
            // Start server if after compilation or Auto Start Server is enabled
            if ((isAfterCompile || autoStartServer) && !McpServerController.IsServerRunning)
            {
                // Clear flag if after compilation
                if (isAfterCompile)
                {
                    SessionState.EraseBool(McpConstants.SESSION_KEY_AFTER_COMPILE);
                    // McpEditorWindow detected post-compile state. Starting server immediately
                    
                    // Use saved port number
                    int savedPort = SessionState.GetInt(McpConstants.SESSION_KEY_SERVER_PORT, customPort);
                    if (savedPort != customPort)
                    {
                        customPort = savedPort;
                        McpEditorSettings.SetCustomPort(customPort);
                    }
                }
                
                StartServerInternal();
            }
        }

        private void OnDisable()
        {
            // Unsubscribe from log update events
            McpCommunicationLogger.OnLogUpdated -= Repaint;
            
            // Unsubscribe from server events
            UnsubscribeFromServerEvents();
            
            // Unsubscribe from update events
            EditorApplication.update -= OnEditorUpdate;
            
            // Save editor selection state
            SessionState.SetInt(McpConstants.SESSION_KEY_SELECTED_EDITOR_TYPE, (int)selectedEditorType);
            
            // Leave server management completely to McpServerController
            // Server does not stop when window is closed (treated as global resource)
            // Window closing, server will keep running if active
        }

        /// <summary>
        /// Subscribe to server events for immediate UI updates
        /// </summary>
        private void SubscribeToServerEvents()
        {
            // Unsubscribe first to avoid duplicate subscriptions
            UnsubscribeFromServerEvents();
            
            McpBridgeServer currentServer = McpServerController.CurrentServer;
            if (currentServer != null)
            {
                currentServer.OnClientConnected += OnClientConnectedHandler;
                currentServer.OnClientDisconnected += OnClientDisconnectedHandler;
            }
        }

        /// <summary>
        /// Unsubscribe from server events
        /// </summary>
        private void UnsubscribeFromServerEvents()
        {
            McpBridgeServer currentServer = McpServerController.CurrentServer;
            if (currentServer != null)
            {
                currentServer.OnClientConnected -= OnClientConnectedHandler;
                currentServer.OnClientDisconnected -= OnClientDisconnectedHandler;
            }
        }

        /// <summary>
        /// Handle client connection event - force UI repaint for immediate update
        /// </summary>
        private void OnClientConnectedHandler(string clientEndpoint)
        {
            // Mark that repaint is needed since events are called from background thread
            needsRepaint = true;
            
            // Exit post-compile mode when client connects
            if (isPostCompileMode)
            {
                isPostCompileMode = false;
            }
        }

        /// <summary>
        /// Handle client disconnection event - force UI repaint for immediate update
        /// </summary>
        private void OnClientDisconnectedHandler(string clientEndpoint)
        {
            // Mark that repaint is needed since events are called from background thread
            needsRepaint = true;
        }

        /// <summary>
        /// Called from EditorApplication.update - handles UI refresh even when Unity is not focused
        /// </summary>
        private void OnEditorUpdate()
        {
            // Always check for server state changes
            CheckServerStateChanges();
            
            // In post-compile mode, always repaint for immediate updates
            if (isPostCompileMode)
            {
                Repaint();
                return;
            }
            
            // Normal mode: repaint only when needed
            if (needsRepaint)
            {
                needsRepaint = false;
                Repaint();
            }
        }
        
        /// <summary>
        /// Check if server state has changed and mark repaint if needed
        /// </summary>
        private void CheckServerStateChanges()
        {
            (bool isRunning, int port, bool wasRestored) = McpServerController.GetServerStatus();
            var connectedClients = McpServerController.CurrentServer?.GetConnectedClients();
            int connectedCount = connectedClients?.Count ?? 0;
            
            // Generate hash of client information to detect changes in client names
            string clientsInfoHash = GenerateClientsInfoHash(connectedClients);
            
            // Check if any server state has changed
            if (isRunning != lastServerRunning || 
                port != lastServerPort || 
                connectedCount != lastConnectedClientsCount ||
                clientsInfoHash != lastClientsInfoHash)
            {
                lastServerRunning = isRunning;
                lastServerPort = port;
                lastConnectedClientsCount = connectedCount;
                lastClientsInfoHash = clientsInfoHash;
                needsRepaint = true;
            }
        }
        
        /// <summary>
        /// Generate hash string from client information to detect changes
        /// </summary>
        private string GenerateClientsInfoHash(System.Collections.Generic.IReadOnlyCollection<ConnectedClient> clients)
        {
            if (clients == null || clients.Count == 0)
            {
                return "empty";
            }
            
            // Create a hash based on endpoint, client name, and process ID for unique identification
            var info = clients.Select(c => $"{c.Endpoint}:{c.ClientName}:{c.ProcessId}").OrderBy(s => s);
            return string.Join("|", info);
        }

        /// <summary>
        /// Called when the window gets focus - update UI to reflect current state
        /// </summary>
        private void OnFocus()
        {
            // Refresh UI when window gains focus to reflect any state changes
            Repaint();
        }

        private void OnGUI()
        {
            // Synchronize server port and UI settings
            SyncPortSettings();
            
            // Make entire window scrollable
            mainScrollPosition = EditorGUILayout.BeginScrollView(mainScrollPosition);
            
            // Use view layer for rendering
            ServerStatusData statusData = CreateServerStatusData();
            _view.DrawServerStatus(statusData);
            
            ServerControlsData controlsData = CreateServerControlsData();
            _view.DrawServerControls(
                data: controlsData, 
                startCallback: StartServer, 
                stopCallback: StopServer, 
                autoStartCallback: (autoStart) => {
                    autoStartServer = autoStart;
                    McpEditorSettings.SetAutoStartServer(autoStartServer);
                },
                portChangeCallback: (port) => {
                    customPort = port;
                    McpEditorSettings.SetCustomPort(customPort);
                });
            
            ConnectedToolsData toolsData = CreateConnectedToolsData();
            _view.DrawConnectedToolsSection(
                data: toolsData, 
                toggleFoldoutCallback: (show) => showConnectedTools = show);
            
            EditorConfigData configData = CreateEditorConfigData();
            _view.DrawEditorConfigSection(
                data: configData, 
                editorChangeCallback: (type) => {
                    selectedEditorType = type;
                    SessionState.SetInt(McpConstants.SESSION_KEY_SELECTED_EDITOR_TYPE, (int)selectedEditorType);
                },
                configureCallback: (editor) => ConfigureEditor(editor),
                foldoutCallback: (show) => showLLMToolSettings = show);
            
#if UMCP_DEBUG
            DeveloperToolsData devToolsData = CreateDeveloperToolsData();
            _view.DrawDeveloperTools(
                data: devToolsData,
                foldoutCallback: (show) => {
                    showDeveloperTools = show;
                    McpEditorSettings.SetShowDeveloperTools(showDeveloperTools);
                },
                devModeCallback: (enable) => {
                    enableDevelopmentMode = enable;
                    McpEditorSettings.SetEnableDevelopmentMode(enableDevelopmentMode);
                },
                mcpLogsCallback: (enable) => {
                    enableMcpLogs = enable;
                    McpEditorSettings.SetEnableMcpLogs(enableMcpLogs);
                },
                commLogsCallback: (enable) => {
                    enableCommunicationLogs = enable;
                    McpEditorSettings.SetEnableCommunicationLogs(enableCommunicationLogs);
                    if (!enableCommunicationLogs)
                    {
                        McpCommunicationLogger.ClearLogs();
                    }
                },
                showDebugCallback: () => {
                    string debugInfo = McpServerController.GetDetailedServerStatus();
                    Debug.Log($"MCP Server Debug Info:\n{debugInfo}");
                },
                notifyChangesCallback: () => NotifyCommandChanges(),
                rebuildCallback: () => {
                    // TypeScript rebuild functionality moved to View layer
                    Debug.Log("TypeScript rebuild not implemented in this version");
                });
#endif
            
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// Synchronize server port and UI settings
        /// </summary>
        private void SyncPortSettings()
        {
            // Synchronize if server is running and UI port setting differs from actual server port
            if (McpServerController.IsServerRunning)
            {
                int actualServerPort = McpServerController.ServerPort;
                if (customPort != actualServerPort)
                {
                    customPort = actualServerPort;
                    // Update configuration file as well
                    McpEditorSettings.SetCustomPort(customPort);
                }
            }
        }

        /// <summary>
        /// Create server status data for view rendering
        /// </summary>
        private ServerStatusData CreateServerStatusData()
        {
            (bool isRunning, int port, bool wasRestored) = McpServerController.GetServerStatus();
            string status = isRunning ? "Running" : "Stopped";
            Color statusColor = isRunning ? Color.green : Color.red;
            
            return new ServerStatusData(isRunning, port, status, statusColor);
        }

        /// <summary>
        /// Create server controls data for view rendering
        /// </summary>
        private ServerControlsData CreateServerControlsData()
        {
            bool isRunning = McpServerController.IsServerRunning;
            return new ServerControlsData(customPort, autoStartServer, isRunning, !isRunning);
        }

        /// <summary>
        /// Create connected tools data for view rendering
        /// </summary>
        private ConnectedToolsData CreateConnectedToolsData()
        {
            bool isServerRunning = McpServerController.IsServerRunning;
            bool showReconnectingUI = SessionState.GetBool(McpConstants.SESSION_KEY_SHOW_RECONNECTING_UI, false);
            var connectedClients = McpServerController.CurrentServer?.GetConnectedClients();
            
            return new ConnectedToolsData(connectedClients, showConnectedTools, isServerRunning, showReconnectingUI);
        }

        /// <summary>
        /// Create editor config data for view rendering
        /// </summary>
        private EditorConfigData CreateEditorConfigData()
        {
            bool isServerRunning = McpServerController.IsServerRunning;
            int currentPort = McpServerController.ServerPort;
            
            return new EditorConfigData(selectedEditorType, showLLMToolSettings, isServerRunning, currentPort);
        }

        /// <summary>
        /// Configure editor settings
        /// </summary>
        private void ConfigureEditor(string editorName)
        {
            McpConfigService configService = GetConfigService(selectedEditorType);
            bool isServerRunning = McpServerController.IsServerRunning;
            int portToUse = isServerRunning ? McpServerController.ServerPort : customPort;
            
            configService.AutoConfigure(portToUse);
#if UMCP_DEBUG
            configService.UpdateDevelopmentSettings(portToUse, enableDevelopmentMode, enableMcpLogs);
#endif
            Repaint();
        }

#if UMCP_DEBUG
        /// <summary>
        /// Create developer tools data for view rendering
        /// </summary>
        private DeveloperToolsData CreateDeveloperToolsData()
        {
            IReadOnlyList<McpCommunicationLogEntry> logs = McpCommunicationLogger.GetAllLogs();
            
            return new DeveloperToolsData(
                showDeveloperTools, 
                enableMcpLogs, 
                enableCommunicationLogs, 
                enableDevelopmentMode, 
                showCommunicationLogs, 
                logs, 
                communicationLogScrollPosition, 
                communicationLogHeight, 
                requestScrollPositions, 
                responseScrollPositions
            );
        }
#endif



        /// <summary>
        /// Start server (for user operations)
        /// </summary>
        private void StartServer()
        {
            if (ValidatePortAndStartServer())
            {
                Repaint();
            }
        }

        /// <summary>
        /// Start server (for internal processing)
        /// </summary>
        private void StartServerInternal()
        {
            ValidatePortAndStartServer();
        }

        /// <summary>
        /// Validate port and start server
        /// </summary>
        /// <returns>True if successful</returns>
        private bool ValidatePortAndStartServer()
        {
            if (customPort < McpServerConfig.MIN_PORT_NUMBER || customPort > McpServerConfig.MAX_PORT_NUMBER)
            {
                EditorUtility.DisplayDialog("Port Error", $"Port must be between {McpServerConfig.MIN_PORT_NUMBER} and {McpServerConfig.MAX_PORT_NUMBER}", "OK");
                return false;
            }

            // Check if our own server is already running on the same port
            if (McpServerController.IsServerRunning && McpServerController.ServerPort == customPort)
            {
                // MCP Server is already running
                return true; // Already running, treat as success
            }

            // Check if another process is using the port
            if (McpBridgeServer.IsPortInUse(customPort))
            {
                EditorUtility.DisplayDialog("Port Error", 
                    $"Port {customPort} is already in use by another process.\nPlease choose a different port number.", 
                    "OK");
                return false;
            }

            try
            {
                McpServerController.StartServer(customPort);
                
                // Subscribe to new server events after successful start
                SubscribeToServerEvents();
                
                // Force UI update after server start
                Repaint();
                
                return true;
            }
            catch (InvalidOperationException ex)
            {
                // In case of port in use error
                EditorUtility.DisplayDialog("Server Start Error", ex.Message, "OK");
                return false;
            }
            catch (Exception ex)
            {
                // Other errors
                EditorUtility.DisplayDialog("Server Start Error", 
                    $"Failed to start server: {ex.Message}", 
                    "OK");
                return false;
            }
        }

        /// <summary>
        /// Stop server
        /// </summary>
        private void StopServer()
        {
            McpServerController.StopServer();
            Repaint();
        }


        
        /// <summary>
        /// Notify command changes to TypeScript side
        /// </summary>
        private void NotifyCommandChanges()
        {
            try
            {
                McpLogger.LogDebug("[TRACE] McpEditorWindow.NotifyCommandChanges: About to call TriggerCommandsChangedNotification (MANUAL_BUTTON)");
                UnityCommandRegistry.TriggerCommandsChangedNotification();
                EditorUtility.DisplayDialog("Command Notification", 
                    "Command changes have been notified to Cursor successfully!", 
                    "OK");
                // Command changes notification sent
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Notification Error", 
                    $"Failed to notify command changes: {ex.Message}", 
                    "OK");
                McpLogger.LogError($"Failed to notify command changes: {ex.Message}");
            }
        }

        /// <summary>
        /// Connection display states for UI logic separation
        /// </summary>
        private enum ConnectionDisplayState
        {
            ServerNotRunning,
            Reconnecting,
            HasConnectedClients,
            NoConnectedClients
        }

        /// <summary>
        /// Get corresponding configuration service from editor type
        /// </summary>
        private McpConfigService GetConfigService(McpEditorType editorType)
        {
            return editorType switch
            {
                McpEditorType.Cursor => _cursorConfigService,
                McpEditorType.ClaudeCode => _claudeCodeConfigService,
                McpEditorType.VSCode => _vscodeConfigService,
                McpEditorType.GeminiCLI => _geminiCLIConfigService,
                McpEditorType.McpInspector => _mcpInspectorConfigService,
                _ => throw new ArgumentException($"Unsupported editor type: {editorType}")
            };
        }

        /// <summary>
        /// Get display name from editor type
        /// </summary>
        private string GetEditorDisplayName(McpEditorType editorType)
        {
            return editorType switch
            {
                McpEditorType.Cursor => "Cursor",
                McpEditorType.ClaudeCode => "Claude Code",
                McpEditorType.VSCode => "VSCode",
                McpEditorType.GeminiCLI => "Gemini CLI",
                McpEditorType.McpInspector => "MCP Inspector",
                _ => editorType.ToString()
            };
        }

        /// <summary>
        /// Draw configuration section for the selected editor
        /// </summary>
        private void DrawConfigurationSection(string editorName, McpConfigService configService, bool isServerRunning, int currentServerPort)
        {
            EditorGUILayout.LabelField($"{editorName} Settings", EditorStyles.boldLabel);
            
            bool isConfigured = false;
            try
            {
                isConfigured = configService.IsConfigured();
            }
            catch (System.Exception ex)
            {
                EditorGUILayout.HelpBox($"Error loading {editorName} configuration: {ex.Message}", MessageType.Error);
                return; // Don't throw, just return to avoid GUI layout issues
            }
            
            if (isConfigured)
            {
                // Check for port number inconsistency
                if (isServerRunning && currentServerPort != customPort)
                {
                    EditorGUILayout.HelpBox($"Warning: {editorName} settings port number may not match current server port ({currentServerPort}).", MessageType.Warning);
                    
                    if (GUILayout.Button($"Update {editorName} settings to port {currentServerPort}"))
                    {
                        configService.AutoConfigure(currentServerPort);
                        Repaint();
                    }
                    
                    EditorGUILayout.Space();
                }
                
                EditorGUILayout.HelpBox($"{editorName} settings are already configured.", MessageType.Info);
                
                string configPath = UnityMcpPathResolver.GetConfigPath(selectedEditorType);
                if (GUILayout.Button("Open Configuration File"))
                {
                    Application.OpenURL("file://" + configPath);
                }
            }
            else
            {
                EditorGUILayout.HelpBox($"{editorName} settings not found. Please run auto-configuration.", MessageType.Warning);
            }
            
            EditorGUILayout.Space();
            
            string buttonText = isServerRunning ? $"Auto-configure {editorName} settings (Port {currentServerPort})" : $"Auto-configure {editorName} settings";
            if (GUILayout.Button(buttonText))
            {
                int portToUse = isServerRunning ? currentServerPort : customPort;
                configService.AutoConfigure(portToUse);
#if UMCP_DEBUG
                // Also update development settings to reflect current UI state
                configService.UpdateDevelopmentSettings(portToUse, enableDevelopmentMode, enableMcpLogs);
#endif
                Repaint();
            }
        }


    }
} 