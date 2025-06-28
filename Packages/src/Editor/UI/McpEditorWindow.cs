using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using System.Linq;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Editor Window for controlling Unity MCP Server - Presenter layer in MVP architecture
    /// Displays server status and handles start/stop operations
    /// Related classes:
    /// - McpEditorModel: Model layer for state management and business logic
    /// - McpEditorWindowView: View layer for UI rendering
    /// - McpEditorWindowState: State objects (UIState, RuntimeState, DebugState)
    /// - McpServerController: Manages the server lifecycle
    /// - McpBridgeServer: The core TCP server implementation
    /// - McpConfigService: Handles configuration for different IDEs
    /// - McpEditorSettings: Persistent settings storage
    /// </summary>
    public class McpEditorWindow : EditorWindow
    {
        // Configuration services factory
        private McpConfigServiceFactory _configServiceFactory;
        
        // View layer
        private McpEditorWindowView _view;
        
        // Model layer (MVP pattern)
        private McpEditorModel _model;

        [MenuItem("Window/uMCP")]
        public static void ShowWindow()
        {
            McpEditorWindow window = GetWindow<McpEditorWindow>(McpConstants.PROJECT_NAME);
            window.minSize = new Vector2(McpUIConstants.MIN_WINDOW_WIDTH, McpUIConstants.MIN_WINDOW_HEIGHT);
            window.Show();
        }

        private void OnEnable()
        {
            InitializeModel();
            InitializeView();
            InitializeConfigurationServices();
            LoadSavedSettings();
            RestoreSessionState();
            SubscribeToEvents();
            HandlePostCompileMode();
        }

        /// <summary>
        /// Initialize model layer
        /// </summary>
        private void InitializeModel()
        {
            _model = new McpEditorModel();
        }

        /// <summary>
        /// Initialize view layer
        /// </summary>
        private void InitializeView()
        {
            _view = new McpEditorWindowView();
        }

                /// <summary>
        /// Initialize configuration services factory
        /// </summary>
        private void InitializeConfigurationServices()
        {
            _configServiceFactory = new McpConfigServiceFactory();
        }

        /// <summary>
        /// Load saved settings from preferences
        /// </summary>
        private void LoadSavedSettings()
        {
            _model.LoadFromSettings();
        }

        /// <summary>
        /// Restore session state from Unity SessionState
        /// </summary>
        private void RestoreSessionState()
        {
            _model.LoadFromSessionState();
        }

        /// <summary>
        /// Subscribe to all necessary events
        /// </summary>
        private void SubscribeToEvents()
        {
            McpCommunicationLogger.OnLogUpdated += Repaint;
            SubscribeToServerEvents();
            EditorApplication.update += OnEditorUpdate;
        }

        /// <summary>
        /// Handle post-compile mode initialization and auto-start logic
        /// </summary>
        private void HandlePostCompileMode()
        {
            // Enable post-compile mode after domain reload
            _model.EnablePostCompileMode();

            // Clear reconnecting UI flag on domain reload to ensure proper state
            SessionState.SetBool(McpConstants.SESSION_KEY_SHOW_RECONNECTING_UI, false);

            UnityEngine.Debug.Log("[McpEditorWindow] Post-compile mode enabled");

            // Check if after compilation
            bool isAfterCompile = SessionState.GetBool(McpConstants.SESSION_KEY_AFTER_COMPILE, false);

            // Start server if after compilation or Auto Start Server is enabled
            if ((isAfterCompile || _model.UI.AutoStartServer) && !McpServerController.IsServerRunning)
            {
                if (isAfterCompile)
                {
                    SessionState.EraseBool(McpConstants.SESSION_KEY_AFTER_COMPILE);

                    // Use saved port number
                    int savedPort = SessionState.GetInt(McpConstants.SESSION_KEY_SERVER_PORT, _model.UI.CustomPort);
                    if (savedPort != _model.UI.CustomPort)
                    {
                        _model.UpdateUIState(ui => new UIState(
                            customPort: savedPort,
                            autoStartServer: ui.AutoStartServer,
                            showLLMToolSettings: ui.ShowLLMToolSettings,
                            showConnectedTools: ui.ShowConnectedTools,
                            selectedEditorType: ui.SelectedEditorType,
                            mainScrollPosition: ui.MainScrollPosition));
                        McpEditorSettings.SetCustomPort(savedPort);
                    }
                }

                StartServerInternal();
            }
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
            SaveSessionState();
        }

        /// <summary>
        /// Unsubscribe from all events
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            McpCommunicationLogger.OnLogUpdated -= Repaint;
            UnsubscribeFromServerEvents();
            EditorApplication.update -= OnEditorUpdate;
        }

        /// <summary>
        /// Save current state to Unity SessionState
        /// </summary>
        private void SaveSessionState()
        {
            _model.SaveToSessionState();
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
            _model.RequestRepaint();

            // Exit post-compile mode when client connects
            if (_model.Runtime.IsPostCompileMode)
            {
                _model.DisablePostCompileMode();
            }
        }

        /// <summary>
        /// Handle client disconnection event - force UI repaint for immediate update
        /// </summary>
        private void OnClientDisconnectedHandler(string clientEndpoint)
        {
            // Mark that repaint is needed since events are called from background thread
            _model.RequestRepaint();
        }

        /// <summary>
        /// Called from EditorApplication.update - handles UI refresh even when Unity is not focused
        /// </summary>
        private void OnEditorUpdate()
        {
            // Always check for server state changes
            CheckServerStateChanges();

            // In post-compile mode, always repaint for immediate updates
            if (_model.Runtime.IsPostCompileMode)
            {
                Repaint();
                return;
            }

            // Normal mode: repaint only when needed
            if (_model.Runtime.NeedsRepaint)
            {
                _model.ClearRepaintRequest();
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
            if (isRunning != _model.Runtime.LastServerRunning ||
                port != _model.Runtime.LastServerPort ||
                connectedCount != _model.Runtime.LastConnectedClientsCount ||
                clientsInfoHash != _model.Runtime.LastClientsInfoHash)
            {
                _model.UpdateServerStateTracking(isRunning, port, connectedCount, clientsInfoHash);
                _model.RequestRepaint();
            }
        }

        /// <summary>
        /// Generate hash string from client information to detect changes
        /// </summary>
        private string GenerateClientsInfoHash(IReadOnlyCollection<ConnectedClient> clients)
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
            Vector2 newScrollPosition = EditorGUILayout.BeginScrollView(_model.UI.MainScrollPosition);
            if (newScrollPosition != _model.UI.MainScrollPosition)
            {
                UpdateMainScrollPosition(newScrollPosition);
            }

            // Use view layer for rendering
            ServerStatusData statusData = CreateServerStatusData();
            _view.DrawServerStatus(statusData);

            ServerControlsData controlsData = CreateServerControlsData();
            _view.DrawServerControls(
                data: controlsData,
                startCallback: StartServer,
                stopCallback: StopServer,
                autoStartCallback: UpdateAutoStartServer,
                portChangeCallback: UpdateCustomPort);

            ConnectedToolsData toolsData = CreateConnectedToolsData();
            _view.DrawConnectedToolsSection(
                data: toolsData,
                toggleFoldoutCallback: UpdateShowConnectedTools);

            EditorConfigData configData = CreateEditorConfigData();
            _view.DrawEditorConfigSection(
                data: configData,
                editorChangeCallback: UpdateSelectedEditorType,
                configureCallback: (editor) => ConfigureEditor(),
                foldoutCallback: UpdateShowLLMToolSettings);

#if UMCP_DEBUG
            DeveloperToolsData devToolsData = CreateDeveloperToolsData();
            _view.DrawDeveloperTools(
                data: devToolsData,
                foldoutCallback: UpdateShowDeveloperTools,
                devModeCallback: UpdateEnableDevelopmentMode,
                mcpLogsCallback: UpdateEnableMcpLogs,
                commLogsCallback: UpdateEnableCommunicationLogs,
                showDebugCallback: () =>
                {
                    string debugInfo = McpServerController.GetDetailedServerStatus();
                    Debug.Log($"MCP Server Debug Info:\n{debugInfo}");
                },
                notifyChangesCallback: () => NotifyCommandChanges(),
                rebuildCallback: () =>
                {
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
                if (_model.UI.CustomPort != actualServerPort)
                {
                    _model.UpdateUIState(ui => new UIState(
                        customPort: actualServerPort,
                        autoStartServer: ui.AutoStartServer,
                        showLLMToolSettings: ui.ShowLLMToolSettings,
                        showConnectedTools: ui.ShowConnectedTools,
                        selectedEditorType: ui.SelectedEditorType,
                        mainScrollPosition: ui.MainScrollPosition));
                    // Update configuration file as well
                    McpEditorSettings.SetCustomPort(actualServerPort);
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
            return new ServerControlsData(_model.UI.CustomPort, _model.UI.AutoStartServer, isRunning, !isRunning);
        }

        /// <summary>
        /// Create connected tools data for view rendering
        /// </summary>
        private ConnectedToolsData CreateConnectedToolsData()
        {
            bool isServerRunning = McpServerController.IsServerRunning;
            bool showReconnectingUI = SessionState.GetBool(McpConstants.SESSION_KEY_SHOW_RECONNECTING_UI, false);
            var connectedClients = McpServerController.CurrentServer?.GetConnectedClients();

            return new ConnectedToolsData(connectedClients, _model.UI.ShowConnectedTools, isServerRunning, showReconnectingUI);
        }

        /// <summary>
        /// Create editor config data for view rendering
        /// </summary>
        private EditorConfigData CreateEditorConfigData()
        {
            bool isServerRunning = McpServerController.IsServerRunning;
            int currentPort = McpServerController.ServerPort;

            return new EditorConfigData(_model.UI.SelectedEditorType, _model.UI.ShowLLMToolSettings, isServerRunning, currentPort);
        }

        /// <summary>
        /// Configure editor settings
        /// </summary>
        private void ConfigureEditor()
        {
            McpConfigService configService = GetConfigService(_model.UI.SelectedEditorType);
            bool isServerRunning = McpServerController.IsServerRunning;
            int portToUse = isServerRunning ? McpServerController.ServerPort : _model.UI.CustomPort;

            configService.AutoConfigure(portToUse);
#if UMCP_DEBUG
            configService.UpdateDevelopmentSettings(portToUse, _model.Debug.EnableDevelopmentMode, _model.Debug.EnableMcpLogs);
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
                _model.Debug.ShowDeveloperTools,
                _model.Debug.EnableMcpLogs,
                _model.Debug.EnableCommunicationLogs,
                _model.Debug.EnableDevelopmentMode,
                _model.Debug.ShowCommunicationLogs,
                logs,
                _model.Debug.CommunicationLogScrollPosition,
                _model.Debug.CommunicationLogHeight,
                _model.Debug.RequestScrollPositions,
                _model.Debug.ResponseScrollPositions
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
            int currentPort = _model.UI.CustomPort;
            if (currentPort < McpServerConfig.MIN_PORT_NUMBER || currentPort > McpServerConfig.MAX_PORT_NUMBER)
            {
                EditorUtility.DisplayDialog("Port Error", $"Port must be between {McpServerConfig.MIN_PORT_NUMBER} and {McpServerConfig.MAX_PORT_NUMBER}", "OK");
                return false;
            }

            // Check if our own server is already running on the same port
            if (McpServerController.IsServerRunning && McpServerController.ServerPort == currentPort)
            {
                // MCP Server is already running
                return true; // Already running, treat as success
            }

            // Check if another process is using the port
            if (McpBridgeServer.IsPortInUse(currentPort))
            {
                EditorUtility.DisplayDialog("Port Error",
                    $"Port {currentPort} is already in use by another process.\nPlease choose a different port number.",
                    "OK");
                return false;
            }

            try
            {
                McpServerController.StartServer(currentPort);

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
        /// Get corresponding configuration service from editor type
        /// </summary>
        private McpConfigService GetConfigService(McpEditorType editorType)
        {
            return _configServiceFactory.GetConfigService(editorType);
        }

        // UIState update helper methods for callback unification
        
        /// <summary>
        /// Update AutoStartServer setting with persistence
        /// </summary>
        private void UpdateAutoStartServer(bool autoStart)
        {
            _model.UpdateAutoStartServer(autoStart);
        }

        /// <summary>
        /// Update CustomPort setting with persistence
        /// </summary>
        private void UpdateCustomPort(int port)
        {
            _model.UpdateCustomPort(port);
        }

        /// <summary>
        /// Update ShowConnectedTools setting
        /// </summary>
        private void UpdateShowConnectedTools(bool show)
        {
            _model.UpdateShowConnectedTools(show);
        }

        /// <summary>
        /// Update ShowLLMToolSettings setting
        /// </summary>
        private void UpdateShowLLMToolSettings(bool show)
        {
            _model.UpdateShowLLMToolSettings(show);
        }

        /// <summary>
        /// Update SelectedEditorType setting with persistence
        /// </summary>
        private void UpdateSelectedEditorType(McpEditorType type)
        {
            _model.UpdateSelectedEditorType(type);
        }

        /// <summary>
        /// Update MainScrollPosition setting
        /// </summary>
        private void UpdateMainScrollPosition(Vector2 position)
        {
            _model.UpdateMainScrollPosition(position);
        }

        // DebugState update helper methods for callback unification

        /// <summary>
        /// Update ShowDeveloperTools setting with persistence
        /// </summary>
        private void UpdateShowDeveloperTools(bool show)
        {
            _model.UpdateShowDeveloperTools(show);
        }

        /// <summary>
        /// Update EnableDevelopmentMode setting with persistence
        /// </summary>
        private void UpdateEnableDevelopmentMode(bool enable)
        {
            _model.UpdateEnableDevelopmentMode(enable);
        }

        /// <summary>
        /// Update EnableMcpLogs setting with persistence
        /// </summary>
        private void UpdateEnableMcpLogs(bool enable)
        {
            _model.UpdateEnableMcpLogs(enable);
        }

        /// <summary>
        /// Update EnableCommunicationLogs setting with persistence and log clearing
        /// </summary>
        private void UpdateEnableCommunicationLogs(bool enable)
        {
            _model.UpdateEnableCommunicationLogs(enable);
        }
    }
}