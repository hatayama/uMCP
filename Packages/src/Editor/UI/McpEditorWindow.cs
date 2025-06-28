using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using System.Linq;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Editor Window for controlling Unity MCP Server - Presenter layer in MVP architecture
    /// Coordinates between Model, View, and helper classes for server management
    /// Related classes:
    /// - McpEditorModel: Model layer for state management and business logic
    /// - McpEditorWindowView: View layer for UI rendering
    /// - McpEditorWindowEventHandler: Event management helper (Unity/Server events)
    /// - McpServerOperations: Server operations helper (start/stop/validation)
    /// - McpEditorWindowState: State objects (UIState, RuntimeState, DebugState)
    /// - McpConfigServiceFactory: Configuration services factory for different IDEs
    /// - McpServerController: Core server lifecycle management
    /// - McpBridgeServer: The actual TCP server implementation
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
        
        // Event handler (MVP pattern helper)
        private McpEditorWindowEventHandler _eventHandler;
        
        // Server operations handler (MVP pattern helper)
        private McpServerOperations _serverOperations;

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
            InitializeEventHandler();
            InitializeServerOperations();
            LoadSavedSettings();
            RestoreSessionState();
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
        /// Initialize event handler
        /// </summary>
        private void InitializeEventHandler()
        {
            _eventHandler = new McpEditorWindowEventHandler(_model, this);
            _eventHandler.Initialize();
        }

        /// <summary>
        /// Initialize server operations handler
        /// </summary>
        private void InitializeServerOperations()
        {
            _serverOperations = new McpServerOperations(_model, _eventHandler);
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

            // Determine if server should be started automatically
            bool shouldStartAutomatically = isAfterCompile || _model.UI.AutoStartServer;
            bool serverNotRunning = !McpServerController.IsServerRunning;
            bool shouldStartServer = shouldStartAutomatically && serverNotRunning;

            if (shouldStartServer)
            {
                if (isAfterCompile)
                {
                    SessionState.EraseBool(McpConstants.SESSION_KEY_AFTER_COMPILE);

                    // Use saved port number
                    int savedPort = SessionState.GetInt(McpConstants.SESSION_KEY_SERVER_PORT, _model.UI.CustomPort);
                    bool portNeedsUpdate = savedPort != _model.UI.CustomPort;
                    
                    if (portNeedsUpdate)
                    {
                        _model.UpdateCustomPort(savedPort);
                    }
                }

                _serverOperations.StartServerInternal();
            }
        }

        private void OnDisable()
        {
            CleanupEventHandler();
            SaveSessionState();
        }

        /// <summary>
        /// Cleanup event handler
        /// </summary>
        private void CleanupEventHandler()
        {
            _eventHandler?.Cleanup();
        }



        /// <summary>
        /// Save current state to Unity SessionState
        /// </summary>
        private void SaveSessionState()
        {
            _model.SaveToSessionState();
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
                toggleServerCallback: ToggleServer,
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
            bool serverIsRunning = McpServerController.IsServerRunning;
            
            if (serverIsRunning)
            {
                int actualServerPort = McpServerController.ServerPort;
                bool portMismatch = _model.UI.CustomPort != actualServerPort;
                
                if (portMismatch)
                {
                    _model.UpdateCustomPort(actualServerPort);
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
            if (_serverOperations.StartServer())
            {
                Repaint();
            }
        }

        /// <summary>
        /// Stop server
        /// </summary>
        private void StopServer()
        {
            _serverOperations.StopServer();
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

        /// <summary>
        /// Toggle server state (start if stopped, stop if running)
        /// </summary>
        private void ToggleServer()
        {
            if (McpServerController.IsServerRunning)
            {
                StopServer();
            }
            else
            {
                StartServer();
            }
        }
    }
}