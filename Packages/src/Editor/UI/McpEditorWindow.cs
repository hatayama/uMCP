using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace io.github.hatayama.uMCP
{
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
        
        // Configuration services
        private readonly McpConfigRepository _repository = new(McpEditorType.Cursor);
        private McpConfigService _cursorConfigService;
        private McpConfigService _claudeCodeConfigService;
        private McpConfigService _vscodeConfigService;
        
        [MenuItem("Window/uMCP")]
        public static void ShowWindow()
        {
            McpEditorWindow window = GetWindow<McpEditorWindow>(McpConstants.PROJECT_NAME);
            window.minSize = new Vector2(McpUIConstants.MIN_WINDOW_WIDTH, McpUIConstants.MIN_WINDOW_HEIGHT);
            window.Show();
        }

        private void OnEnable()
        {
            // Initialize configuration services
            McpConfigRepository cursorRepository = new(McpEditorType.Cursor);
            McpConfigRepository claudeCodeRepository = new(McpEditorType.ClaudeCode);
            McpConfigRepository vscodeRepository = new(McpEditorType.VSCode);
            _cursorConfigService = new McpConfigService(cursorRepository, McpEditorType.Cursor);
            _claudeCodeConfigService = new McpConfigService(claudeCodeRepository, McpEditorType.ClaudeCode);
            _vscodeConfigService = new McpConfigService(vscodeRepository, McpEditorType.VSCode);
            
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
                currentServer.OnClientDisconnected -= OnClientDisconnectedHandler;
            }
        }

        /// <summary>
        /// Handle client disconnection event - force UI repaint for immediate update
        /// </summary>
        private void OnClientDisconnectedHandler(string clientEndpoint)
        {
            // Force immediate UI update even when Unity Editor is in background
            Repaint();
        }

        private void OnGUI()
        {
            // Synchronize server port and UI settings
            SyncPortSettings();
            
            // Make entire window scrollable
            mainScrollPosition = EditorGUILayout.BeginScrollView(mainScrollPosition);
            
            DrawServerStatus();
            DrawServerControls();
            DrawConnectedToolsSection();
            DrawEditorConfigSection();
#if UMCP_DEBUG
            DrawDeveloperTools();
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
        /// Draw server status section
        /// </summary>
        private void DrawServerStatus()
        {
            EditorGUILayout.BeginVertical("box");
            
            // Status display
            (bool isRunning, int port, bool wasRestored) = McpServerController.GetServerStatus();
            string status = isRunning ? "Running" : "Stopped";
            Color statusColor = isRunning ? Color.green : Color.red;
            
            GUIStyle statusStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = statusColor },
                fontStyle = FontStyle.Bold
            };
            
            // Display Server Status, Status, and Port all horizontally
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Server Status:", EditorStyles.boldLabel, GUILayout.Width(McpUIConstants.LABEL_WIDTH_SERVER_STATUS));
            EditorGUILayout.LabelField($"{status}", statusStyle, GUILayout.Width(McpUIConstants.LABEL_WIDTH_STATUS));
            EditorGUILayout.LabelField($"Port: {port}");
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Draw server control section
        /// </summary>
        private void DrawServerControls()
        {
            EditorGUILayout.BeginVertical("box");
            
            // Auto start checkbox
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Auto Start Server", GUILayout.Width(200));
            bool newAutoStart = EditorGUILayout.Toggle(autoStartServer, GUILayout.Width(20));
            EditorGUILayout.EndHorizontal();
            if (EditorGUI.EndChangeCheck())
            {
                autoStartServer = newAutoStart;
                McpEditorSettings.SetAutoStartServer(autoStartServer);
            }
            
            EditorGUILayout.Space();
            
            // Port settings
            bool isRunning = McpServerController.IsServerRunning;
            EditorGUI.BeginDisabledGroup(isRunning);
            
            EditorGUI.BeginChangeCheck();
            int newPort = EditorGUILayout.IntField("Port:", customPort);
            if (EditorGUI.EndChangeCheck())
            {
                customPort = newPort;
                // Save immediately when port number is changed
                McpEditorSettings.SetCustomPort(customPort);
            }
            
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.Space();
            
            // Start/Stop buttons
            EditorGUILayout.BeginHorizontal();
            
            EditorGUI.BeginDisabledGroup(!isRunning);
            if (GUILayout.Button("Stop Server", GUILayout.Height(McpUIConstants.BUTTON_HEIGHT_LARGE)))
            {
                StopServer();
            }
            EditorGUI.EndDisabledGroup();
            
            EditorGUI.BeginDisabledGroup(isRunning);
            if (GUILayout.Button("Start Server", GUILayout.Height(McpUIConstants.BUTTON_HEIGHT_LARGE)))
            {
                StartServer();
            }
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

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
        /// Draw connected LLM tools section
        /// </summary>
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
        
        private void DrawConnectedToolsSection()
        {
            EditorGUILayout.BeginVertical("box");
            
            showConnectedTools = EditorGUILayout.Foldout(showConnectedTools, McpUIConstants.CONNECTED_TOOLS_FOLDOUT_TEXT, true);
            
            if (showConnectedTools)
            {
                EditorGUILayout.Space();
                DrawConnectionStatus();
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }
        
        private void DrawConnectionStatus()
        {
            ConnectionDisplayState state = GetConnectionDisplayState();
            DrawConnectionStatusUI(state);
        }
        
        private ConnectionDisplayState GetConnectionDisplayState()
        {
            // Check if explicitly showing reconnecting UI
            bool showReconnectingUI = SessionState.GetBool(McpConstants.SESSION_KEY_SHOW_RECONNECTING_UI, false);
            if (showReconnectingUI)
            {
                return ConnectionDisplayState.Reconnecting;
            }
            
            // Check server status
            if (!McpServerController.IsServerRunning)
            {
                return ConnectionDisplayState.ServerNotRunning;
            }
            
            // Check connected clients
            var connectedClients = McpServerController.CurrentServer?.GetConnectedClients();
            if (connectedClients != null && connectedClients.Count > 0)
            {
                // Clear reconnecting flag when clients are actually connected
                McpServerController.ClearReconnectingFlag();
                return ConnectionDisplayState.HasConnectedClients;
            }
            
            return ConnectionDisplayState.NoConnectedClients;
        }
        
        private void DrawConnectionStatusUI(ConnectionDisplayState state)
        {
            switch (state)
            {
                case ConnectionDisplayState.ServerNotRunning:
                    EditorGUILayout.HelpBox("Server is not running. Start the server to see connected tools.", MessageType.Warning);
                    break;
                    
                case ConnectionDisplayState.Reconnecting:
                    EditorGUILayout.HelpBox(McpUIConstants.RECONNECTING_MESSAGE, MessageType.Info);
                    break;
                    
                case ConnectionDisplayState.HasConnectedClients:
                    DrawConnectedClientsList();
                    break;
                    
                case ConnectionDisplayState.NoConnectedClients:
                    EditorGUILayout.HelpBox("No LLM tools currently connected.", MessageType.Info);
                    break;
            }
        }
        
        private void DrawConnectedClientsList()
        {
            var connectedClients = McpServerController.CurrentServer?.GetConnectedClients();
            if (connectedClients == null) return;
            
            foreach (ConnectedClient client in connectedClients)
            {
                DrawConnectedClientItem(client);
            }
        }

        /// <summary>
        /// Draw individual connected client item with improved UI/UX
        /// </summary>
        private void DrawConnectedClientItem(ConnectedClient client)
        {
            // Box grouping for visual separation
            EditorGUILayout.BeginVertical("box");
            
            // Single line with client name and endpoint/PID information
            EditorGUILayout.BeginHorizontal();
            
            // Client icon and name
            EditorGUILayout.LabelField(McpUIConstants.CLIENT_ICON + client.ClientName, new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold });
            
            // Flexible space
            GUILayout.FlexibleSpace();
            
            // Endpoint with PID information
            GUIStyle endpointStyle = new GUIStyle(EditorStyles.miniLabel);
            endpointStyle.normal.textColor = Color.gray;
            string pidInfo = client.ProcessId > McpConstants.UNKNOWN_PROCESS_ID ? $" (PID: {client.ProcessId})" : "";
            EditorGUILayout.LabelField(McpUIConstants.ENDPOINT_ARROW + client.Endpoint + pidInfo, endpointStyle);
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
            
            // Add small space between client items
            EditorGUILayout.Space(McpUIConstants.CLIENT_ITEM_SPACING);
        }

        /// <summary>
        /// Draw editor configuration section
        /// </summary>
        private void DrawEditorConfigSection()
        {
            EditorGUILayout.BeginVertical("box");
            
            // Foldout header
            showLLMToolSettings = EditorGUILayout.Foldout(showLLMToolSettings, "LLM Tool Settings", true);
            
            // Show content only when expanded
            if (showLLMToolSettings)
            {
                EditorGUILayout.Space();
                
                // Editor selection dropdown
                EditorGUI.BeginChangeCheck();
                selectedEditorType = (McpEditorType)EditorGUILayout.EnumPopup("Target:", selectedEditorType);
                if (EditorGUI.EndChangeCheck())
                {
                    // Save to session when selection changes
                    SessionState.SetInt(McpConstants.SESSION_KEY_SELECTED_EDITOR_TYPE, (int)selectedEditorType);
                }
                
                bool isServerRunning = McpServerController.IsServerRunning;
                int currentServerPort = McpServerController.ServerPort;
                
                // Display only the selected editor's configuration
                McpConfigService configService = GetConfigService(selectedEditorType);
                string editorName = GetEditorDisplayName(selectedEditorType);
                
                DrawConfigurationSection(editorName, configService, isServerRunning, currentServerPort);
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
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

        
#if UMCP_DEBUG
        /// <summary>
        /// Draw developer tools section
        /// </summary>
        private void DrawDeveloperTools()
        {
            EditorGUILayout.BeginVertical("box");
            
            // Toggle header
            EditorGUI.BeginChangeCheck();
            showDeveloperTools = EditorGUILayout.Foldout(showDeveloperTools, "Developer Tools", true);
            if (EditorGUI.EndChangeCheck())
            {
                McpEditorSettings.SetShowDeveloperTools(showDeveloperTools);
            }
            
            // Developer Tools content (display only when expanded)
            if (showDeveloperTools)
            {
                EditorGUILayout.Space();
                
                // TypeScript Development Mode settings
                EditorGUILayout.LabelField("TypeScript Server Settings", EditorStyles.boldLabel);
                
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Enable Development Mode", GUILayout.Width(200));
                bool newEnableDevelopmentMode = EditorGUILayout.Toggle(enableDevelopmentMode, GUILayout.Width(20));
                EditorGUILayout.EndHorizontal();
                if (EditorGUI.EndChangeCheck())
                {
                    enableDevelopmentMode = newEnableDevelopmentMode;
                    McpEditorSettings.SetEnableDevelopmentMode(enableDevelopmentMode);
                    
                    // Update mcp.json immediately
                    UpdateMcpConfigForDevelopmentSettings();
                }
                
                EditorGUILayout.HelpBox(
                    enableDevelopmentMode 
                        ? "Development Mode: Debug tools (mcp-ping, get-unity-commands) will be available in Cursor"
                        : "Production Mode: Only essential tools will be available in Cursor",
                    enableDevelopmentMode ? MessageType.Info : MessageType.Warning
                );
                
                EditorGUILayout.Space();
                
                // Log control toggle
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Enable MCP Logs", GUILayout.Width(200));
                bool newEnableMcpLogs = EditorGUILayout.Toggle(enableMcpLogs, GUILayout.Width(20));
                EditorGUILayout.EndHorizontal();
                if (EditorGUI.EndChangeCheck())
                {
                    enableMcpLogs = newEnableMcpLogs;
                    McpEditorSettings.SetEnableMcpLogs(enableMcpLogs);
                    
                    // Update mcp.json immediately
                    UpdateMcpConfigForDevelopmentSettings();
                }
                
                // Communication logs toggle
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Enable Communication Logs", GUILayout.Width(200));
                bool newEnableCommunicationLogs = EditorGUILayout.Toggle(enableCommunicationLogs, GUILayout.Width(20));
                EditorGUILayout.EndHorizontal();
                if (EditorGUI.EndChangeCheck())
                {
                    enableCommunicationLogs = newEnableCommunicationLogs;
                    McpEditorSettings.SetEnableCommunicationLogs(enableCommunicationLogs);
                    
                    // Clear logs when communication logs are disabled
                    if (!enableCommunicationLogs)
                    {
                        McpCommunicationLogger.ClearLogs();
                    }
                }
                
                EditorGUILayout.Space();
                
                // Communication logs section (only when enabled)
                if (enableCommunicationLogs)
                {
                    DrawCommunicationLogs();
                    EditorGUILayout.Space();
                }
                
                // Debug information display button
                if (GUILayout.Button("Show Debug Info"))
                {
                    string debugInfo = McpServerController.GetDetailedServerStatus();
                    Debug.Log($"MCP Server Debug Info:\n{debugInfo}");
                }
                
                EditorGUILayout.Space();
                
                // Command notification button
                bool isServerRunning = McpServerController.IsServerRunning;
                EditorGUI.BeginDisabledGroup(!isServerRunning);
                if (GUILayout.Button("Notify Command Changes to LLM Tools", GUILayout.Height(McpUIConstants.BUTTON_HEIGHT_MEDIUM)))
                {
                    NotifyCommandChanges();
                }
                EditorGUI.EndDisabledGroup();
                
                if (!isServerRunning)
                {
                    EditorGUILayout.HelpBox("Server must be running to notify command changes", MessageType.Info);
                }
                
                EditorGUILayout.Space();
                
                // TypeScript build button
                if (GUILayout.Button("Rebuild TypeScript Server", GUILayout.Height(McpUIConstants.BUTTON_HEIGHT_LARGE)))
                {
                    RebuildTypeScriptServer();
                }
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        /// <summary>
        /// Rebuild TypeScript server
        /// </summary>
        private void RebuildTypeScriptServer()
        {
            // Create TypeScriptBuilder when needed
            TypeScriptBuilder builder = new TypeScriptBuilder();
            
            builder.BuildTypeScriptServer((success, output, error) => {
                if (success)
                {
                    // Auto-restart if server is running
                    if (McpServerController.IsServerRunning)
                    {
                        Debug.Log("Restarting server with new build...");
                        McpServerController.StopServer();
                        
                        // Wait a bit before restarting (wait for server to completely stop)
                        EditorApplication.delayCall += () => {
                            McpServerController.StartServer(customPort);
                            Debug.Log("Server restarted with updated TypeScript build!");
                            Repaint(); // Update UI
                        };
                    }
                    else
                    {
                        Debug.Log("Server is not running. Start the server manually to use the updated build.");
                    }
                }
                // Error handling is performed within TypeScriptBuilder
            });
        }

        /// <summary>
        /// Draw communication logs section
        /// </summary>
        private void DrawCommunicationLogs()
        {
            // Early return if communication logs are disabled
            if (!enableCommunicationLogs)
            {
                return;
            }
            
            EditorGUILayout.BeginVertical("box");
            
            // Header and clear button
            EditorGUILayout.BeginHorizontal();
            showCommunicationLogs = EditorGUILayout.Foldout(showCommunicationLogs, "Communication Logs", true);
            
            if (showCommunicationLogs)
            {
                if (GUILayout.Button("Clear", GUILayout.Width(McpUIConstants.BUTTON_WIDTH_CLEAR)))
                {
                    McpCommunicationLogger.ClearLogs();
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            if (showCommunicationLogs)
            {
                EditorGUILayout.Space();
                
                IReadOnlyList<McpCommunicationLogEntry> logs = McpCommunicationLogger.GetAllLogs();
                if (logs.Count == 0)
                {
                    EditorGUILayout.HelpBox("No communication logs yet", MessageType.Info);
                }
                else
                {
                    // Scroll view (resizable)
                    communicationLogScrollPosition = EditorGUILayout.BeginScrollView(
                        communicationLogScrollPosition, 
                        GUILayout.Height(communicationLogHeight)
                    );
                    
                    // Display up to maximum logs (from latest)
                    int displayCount = Mathf.Min(logs.Count, McpUIConstants.MAX_COMMUNICATION_LOG_ENTRIES);
                    int startIndex = logs.Count - 1;
                    int endIndex = logs.Count - displayCount;
                    
                    for (int i = startIndex; i >= endIndex; i--)
                    {
                        McpCommunicationLogEntry log = logs[i];
                        DrawLogEntry(log);
                        EditorGUILayout.Space(5);
                    }
                    
                    EditorGUILayout.EndScrollView();
                    
                    // Resize handle
                    DrawCommunicationLogResizeHandle();
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// Draw individual log entry
        /// </summary>
        private void DrawLogEntry(McpCommunicationLogEntry log)
        {
            EditorGUILayout.BeginVertical("box");
            
            // Header (clickable toggle)
            string toggleSymbol = log.IsExpanded ? "▼" : "▶";
            string headerText = $"{toggleSymbol} {log.HeaderText}";
            
            GUIStyle headerStyle = new GUIStyle(EditorStyles.label);
            headerStyle.fontStyle = FontStyle.Bold;
            if (log.IsError)
            {
                headerStyle.normal.textColor = Color.red;
            }
            
            Rect headerRect = GUILayoutUtility.GetRect(new GUIContent(headerText), headerStyle);
            if (GUI.Button(headerRect, headerText, headerStyle))
            {
                log.IsExpanded = !log.IsExpanded;
            }
            
            // Content (only when expanded)
            if (log.IsExpanded)
            {
                EditorGUILayout.Space();
                
                // Request
                EditorGUILayout.LabelField("Request:", EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical("box");
                string formattedRequest = FormatJson(log.Request);
                string requestKey = $"{log.CommandName}_{log.Timestamp:HHmmss}_request";
                DrawJsonContentWithScroll(formattedRequest, requestKey, requestScrollPositions);
                EditorGUILayout.EndVertical();
                
                EditorGUILayout.Space();
                
                // Response
                EditorGUILayout.LabelField("Response:", EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical("box");
                string formattedResponse = FormatJson(log.Response);
                string responseKey = $"{log.CommandName}_{log.Timestamp:HHmmss}_response";
                DrawJsonContentWithScroll(formattedResponse, responseKey, responseScrollPositions);
                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// Format JSON
        /// </summary>
        private string FormatJson(string json)
        {
            try
            {
                JObject parsed = JObject.Parse(json);
                return parsed.ToString(Formatting.Indented);
            }
            catch
            {
                return json; // Return as-is if cannot parse
            }
        }

        /// <summary>
        /// Draw JSON content with appropriate height
        /// </summary>
        private void DrawJsonContent(string jsonContent)
        {
            GUIStyle textAreaStyle = new GUIStyle(EditorStyles.textArea);
            textAreaStyle.wordWrap = true;
            
            // Calculate number of lines in content and set appropriate height
            string[] lines = jsonContent.Split('\n');
            int lineCount = lines.Length;
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float contentHeight = Mathf.Max(McpUIConstants.JSON_CONTENT_MIN_HEIGHT, Mathf.Min(lineCount * lineHeight + 20f, McpUIConstants.JSON_CONTENT_MAX_HEIGHT)); // Minimum 80px, maximum 200px
            
            // Display with regular SelectableLabel (scrolling handled by outer communication log)
            EditorGUILayout.SelectableLabel(jsonContent, textAreaStyle, GUILayout.Height(contentHeight));
        }

        /// <summary>
        /// Draw JSON content in scrollable area
        /// </summary>
        private void DrawJsonContentWithScroll(string jsonContent, string scrollKey, Dictionary<string, Vector2> scrollPositions)
        {
            GUIStyle textAreaStyle = new GUIStyle(EditorStyles.textArea);
            textAreaStyle.wordWrap = true;
            
            // Calculate number of lines in content
            string[] lines = jsonContent.Split('\n');
            int lineCount = lines.Length;
            float lineHeight = EditorGUIUtility.singleLineHeight;
            
            // Create scrollable area with fixed height
            float fixedHeight = McpUIConstants.JSON_SCROLL_FIXED_HEIGHT;
            
            // Calculate actual content height (ensure sufficient margin)
            float contentHeight = lineCount * lineHeight + McpUIConstants.JSON_CONTENT_PADDING; // Top/bottom padding + generous margin
            
            // Get scroll position (initialize if not exists)
            if (!scrollPositions.ContainsKey(scrollKey))
            {
                scrollPositions[scrollKey] = Vector2.zero;
            }
            
            // Create scroll view
            scrollPositions[scrollKey] = EditorGUILayout.BeginScrollView(
                scrollPositions[scrollKey], 
                GUILayout.Height(fixedHeight)
            );
            
            // Display JSON content (ensure sufficient height)
            EditorGUILayout.SelectableLabel(jsonContent, textAreaStyle, GUILayout.Height(contentHeight));
            
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// Draw resize handle for communication log area
        /// </summary>
        private void DrawCommunicationLogResizeHandle()
        {
            // Get resize handle area (increase height for better visibility)
            Rect handleRect = GUILayoutUtility.GetRect(0, McpUIConstants.RESIZE_HANDLE_HEIGHT, GUILayout.ExpandWidth(true));
            
            // Change mouse cursor to resize cursor
            EditorGUIUtility.AddCursorRect(handleRect, MouseCursor.ResizeVertical);
            
            // Draw resize handle appearance (more visible)
            if (Event.current.type == EventType.Repaint)
            {
                Color originalColor = GUI.color;
                
                // Make background slightly darker
                GUI.color = new Color(McpUIConstants.RESIZE_HANDLE_BACKGROUND_ALPHA, McpUIConstants.RESIZE_HANDLE_BACKGROUND_ALPHA, McpUIConstants.RESIZE_HANDLE_BACKGROUND_ALPHA, McpUIConstants.RESIZE_HANDLE_BACKGROUND_ALPHA);
                GUI.DrawTexture(handleRect, EditorGUIUtility.whiteTexture);
                
                // Draw three dots in center to look like a handle
                GUI.color = new Color(McpUIConstants.RESIZE_HANDLE_DOT_BRIGHTNESS, McpUIConstants.RESIZE_HANDLE_DOT_BRIGHTNESS, McpUIConstants.RESIZE_HANDLE_DOT_BRIGHTNESS, McpUIConstants.RESIZE_HANDLE_DOT_ALPHA);
                float centerX = handleRect.x + handleRect.width * 0.5f;
                float centerY = handleRect.y + handleRect.height * 0.5f;
                
                // Draw three dots
                for (int i = -1; i <= 1; i++)
                {
                    Rect dotRect = new Rect(centerX + i * McpUIConstants.DOT_SPACING - 1, centerY - 1, McpUIConstants.DOT_SIZE, McpUIConstants.DOT_SIZE);
                    GUI.DrawTexture(dotRect, EditorGUIUtility.whiteTexture);
                }
                
                GUI.color = originalColor;
            }
            
            // Handle mouse events
            if (Event.current.type == EventType.MouseDown && handleRect.Contains(Event.current.mousePosition))
            {
                isResizingCommunicationLog = true;
                Event.current.Use();
            }
            
            if (isResizingCommunicationLog)
            {
                if (Event.current.type == EventType.MouseDrag)
                {
                    // Adjust height according to drag amount
                    communicationLogHeight += Event.current.delta.y;
                    
                    // Limit minimum and maximum height
                    communicationLogHeight = Mathf.Clamp(communicationLogHeight, McpUIConstants.MIN_COMMUNICATION_LOG_HEIGHT, McpUIConstants.MAX_COMMUNICATION_LOG_HEIGHT);
                    
                    // Save to SessionState
                    SessionState.SetFloat(McpConstants.SESSION_KEY_COMMUNICATION_LOG_HEIGHT, communicationLogHeight);
                    
                    Event.current.Use();
                    Repaint();
                }
                else if (Event.current.type == EventType.MouseUp)
                {
                    isResizingCommunicationLog = false;
                    Event.current.Use();
                }
            }
        }

        /// <summary>
        /// Update MCP configuration for development mode and MCP logs
        /// </summary>
        private void UpdateMcpConfigForDevelopmentSettings()
        {
            try
            {
                // Get configuration service for selected editor type
                McpConfigService configService = GetConfigService(selectedEditorType);
                string editorDisplayName = GetEditorDisplayName(selectedEditorType);
                
                // Get current port
                int portToUse = McpServerController.IsServerRunning ? McpServerController.ServerPort : customPort;
                
                // Update development mode and MCP logs environment variables (preserve other settings)
                configService.UpdateDevelopmentSettings(portToUse, enableDevelopmentMode, enableMcpLogs);
                
                // Updated editor settings
                
                // Log configuration file path for debugging
                string configPath = UnityMcpPathResolver.GetConfigPath(selectedEditorType);
                // Configuration file partially updated
                
                // Log update confirmation instead of showing dialog
                string modeText = enableDevelopmentMode ? "Development Mode (debug tools enabled)" : "Production Mode (debug tools disabled)";
                string logsText = enableMcpLogs ? "MCP logs enabled" : "MCP logs disabled";
                // Configuration updated successfully
            }
            catch (System.Exception ex)
            {
                string editorDisplayName = GetEditorDisplayName(selectedEditorType);
                EditorUtility.DisplayDialog("Configuration Error", 
                    $"Failed to update {editorDisplayName} development settings: {ex.Message}", 
                    "OK");
                McpLogger.LogError($"Failed to update {editorDisplayName} development settings: {ex.Message}");
            }
        }
#endif
    }
} 