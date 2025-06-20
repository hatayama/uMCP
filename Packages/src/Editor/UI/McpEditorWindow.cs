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

        
        // UI constants
        private const float MIN_WINDOW_WIDTH = 400f;
        private const float MIN_WINDOW_HEIGHT = 200f;
        private const float DEFAULT_COMMUNICATION_LOG_HEIGHT = 300f;
        private const float MIN_COMMUNICATION_LOG_HEIGHT = 100f;
        private const float MAX_COMMUNICATION_LOG_HEIGHT = 800f;
        
        // UI state
        private int customPort = McpServerConfig.DEFAULT_PORT;
        private bool autoStartServer = false;
        private bool showDeveloperTools = false;
        private bool showCommunicationLogs = false;
        private bool enableMcpLogs = false;
        private Vector2 communicationLogScrollPosition;
        private float communicationLogHeight = DEFAULT_COMMUNICATION_LOG_HEIGHT; // Height of resizable communication log area
        private bool isResizingCommunicationLog = false; // Whether currently resizing
        
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
        
        [MenuItem("Window/uMCP")]
        public static void ShowWindow()
        {
            McpEditorWindow window = GetWindow<McpEditorWindow>(McpConstants.PROJECT_NAME);
            window.minSize = new Vector2(MIN_WINDOW_WIDTH, MIN_WINDOW_HEIGHT);
            window.Show();
        }

        private void OnEnable()
        {
            // Initialize configuration services
            McpConfigRepository cursorRepository = new(McpEditorType.Cursor);
            McpConfigRepository claudeCodeRepository = new(McpEditorType.ClaudeCode);
            _cursorConfigService = new McpConfigService(cursorRepository, McpEditorType.Cursor);
            _claudeCodeConfigService = new McpConfigService(claudeCodeRepository, McpEditorType.ClaudeCode);
            
            // Load saved settings
            McpEditorSettingsData settings = McpEditorSettings.GetSettings();
            customPort = settings.customPort;
            autoStartServer = settings.autoStartServer;
            showDeveloperTools = settings.showDeveloperTools;
            enableMcpLogs = settings.enableMcpLogs;
            
            // Restore editor selection state
            selectedEditorType = (McpEditorType)SessionState.GetInt(McpConstants.SESSION_KEY_SELECTED_EDITOR_TYPE, (int)McpEditorType.Cursor);
            
            // Synchronize McpLogger settings
            McpLogger.EnableDebugLog = enableMcpLogs;
            
            // Restore communication log area height (from SessionState)
            communicationLogHeight = SessionState.GetFloat(McpConstants.SESSION_KEY_COMMUNICATION_LOG_HEIGHT, DEFAULT_COMMUNICATION_LOG_HEIGHT);
            
            // Subscribe to log update events
            McpCommunicationLogger.OnLogUpdated += Repaint;
            
            // Check if after compilation
            bool isAfterCompile = SessionState.GetBool(McpConstants.SESSION_KEY_AFTER_COMPILE, false);
            
            // Start server if after compilation or Auto Start Server is enabled
            if ((isAfterCompile || autoStartServer) && !McpServerController.IsServerRunning)
            {
                // Clear flag if after compilation
                if (isAfterCompile)
                {
                    SessionState.EraseBool(McpConstants.SESSION_KEY_AFTER_COMPILE);
                    McpLogger.LogInfo("McpEditorWindow detected post-compile state. Starting server immediately...");
                    
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
            
            // Save editor selection state
            SessionState.SetInt(McpConstants.SESSION_KEY_SELECTED_EDITOR_TYPE, (int)selectedEditorType);
            
            // Leave server management completely to McpServerController
            // Server does not stop when window is closed (treated as global resource)
            McpLogger.LogInfo($"McpEditorWindow.OnDisable: Window closing, server will keep running if active");
        }

        private void OnGUI()
        {
            // Synchronize server port and UI settings
            SyncPortSettings();
            
            // Make entire window scrollable
            mainScrollPosition = EditorGUILayout.BeginScrollView(mainScrollPosition);
            
            DrawServerStatus();
            DrawServerControls();
            DrawEditorConfigSection();
            DrawCommunicationLogs();
            DrawDeveloperTools();
            
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
            EditorGUILayout.LabelField("Server Status:", EditorStyles.boldLabel, GUILayout.Width(85));
            EditorGUILayout.LabelField($"{status}", statusStyle, GUILayout.Width(80));
            EditorGUILayout.LabelField($"Port: {port}");
            EditorGUILayout.EndHorizontal();
            
            if (isRunning)
            {
                EditorGUILayout.LabelField("Listening for TypeScript MCP Server connections...");
            }
            
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
            bool newAutoStart = EditorGUILayout.Toggle("Auto Start Server", autoStartServer);
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
            
            EditorGUI.BeginDisabledGroup(isRunning);
            if (GUILayout.Button("Start Server", GUILayout.Height(30)))
            {
                StartServer();
            }
            EditorGUI.EndDisabledGroup();
            
            EditorGUI.BeginDisabledGroup(!isRunning);
            if (GUILayout.Button("Stop Server", GUILayout.Height(30)))
            {
                StopServer();
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
                McpLogger.LogInfo($"MCP Server is already running on port {customPort}");
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
        /// Draw editor configuration section
        /// </summary>
        private void DrawEditorConfigSection()
        {
            EditorGUILayout.LabelField("LLM Tool Settings", EditorStyles.boldLabel);
            
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

        /// <summary>
        /// Get corresponding configuration service from editor type
        /// </summary>
        private McpConfigService GetConfigService(McpEditorType editorType)
        {
            return editorType switch
            {
                McpEditorType.Cursor => _cursorConfigService,
                McpEditorType.ClaudeCode => _claudeCodeConfigService,
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
                _ => editorType.ToString()
            };
        }

        /// <summary>
        /// Draw individual configuration section
        /// </summary>
        private void DrawConfigurationSection(string editorName, McpConfigService configService, bool isServerRunning, int currentServerPort)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"{editorName} Settings", EditorStyles.boldLabel);
            
            bool isConfigured = false;
            try
            {
                isConfigured = configService.IsConfigured();
            }
            catch (System.Exception ex)
            {
                EditorGUILayout.HelpBox($"Error loading {editorName} configuration: {ex.Message}", MessageType.Error);
                throw;
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
                    EditorUtility.RevealInFinder(configPath);
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
                Repaint();
            }
            
            EditorGUILayout.EndVertical();
        }

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
                
                // Log control toggle
                EditorGUI.BeginChangeCheck();
                bool newEnableMcpLogs = EditorGUILayout.Toggle("Enable MCP Logs", enableMcpLogs);
                if (EditorGUI.EndChangeCheck())
                {
                    enableMcpLogs = newEnableMcpLogs;
                    McpEditorSettings.SetEnableMcpLogs(enableMcpLogs);
                }
                
                EditorGUILayout.Space();
                
                // Debug information display button
                if (GUILayout.Button("Show Debug Info"))
                {
                    string debugInfo = McpServerController.GetDetailedServerStatus();
                    Debug.Log($"MCP Server Debug Info:\n{debugInfo}");
                }
                
                EditorGUILayout.Space();
                
                // TypeScript build button
                if (GUILayout.Button("Rebuild TypeScript Server", GUILayout.Height(30)))
                {
                    RebuildTypeScriptServer();
                }
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        /// <summary>
        /// Draw communication logs section
        /// </summary>
        private void DrawCommunicationLogs()
        {
            EditorGUILayout.BeginVertical("box");
            
            // Header and clear button
            EditorGUILayout.BeginHorizontal();
            showCommunicationLogs = EditorGUILayout.Foldout(showCommunicationLogs, "Communication Logs", true);
            
            if (showCommunicationLogs)
            {
                if (GUILayout.Button("Clear", GUILayout.Width(60)))
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
                    
                    for (int i = logs.Count - 1; i >= 0; i--) // Display from latest
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
            EditorGUILayout.Space();
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
            float contentHeight = Mathf.Max(80f, Mathf.Min(lineCount * lineHeight + 20f, 200f)); // Minimum 80px, maximum 200px
            
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
            
            // Create scrollable area with fixed height (150px)
            float fixedHeight = 150f;
            
            // Calculate actual content height (ensure sufficient margin)
            float contentHeight = lineCount * lineHeight + 50f; // Top/bottom padding + generous margin
            
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
            // Get resize handle area (increase height to 8px for better visibility)
            Rect handleRect = GUILayoutUtility.GetRect(0, 8, GUILayout.ExpandWidth(true));
            
            // Change mouse cursor to resize cursor
            EditorGUIUtility.AddCursorRect(handleRect, MouseCursor.ResizeVertical);
            
            // Draw resize handle appearance (more visible)
            if (Event.current.type == EventType.Repaint)
            {
                Color originalColor = GUI.color;
                
                // Make background slightly darker
                GUI.color = new Color(0.3f, 0.3f, 0.3f, 0.3f);
                GUI.DrawTexture(handleRect, EditorGUIUtility.whiteTexture);
                
                // Draw three dots in center to look like a handle
                GUI.color = new Color(0.6f, 0.6f, 0.6f, 0.8f);
                float centerX = handleRect.x + handleRect.width * 0.5f;
                float centerY = handleRect.y + handleRect.height * 0.5f;
                
                // Draw three dots
                for (int i = -1; i <= 1; i++)
                {
                    Rect dotRect = new Rect(centerX + i * 4 - 1, centerY - 1, 2, 2);
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
                    communicationLogHeight = Mathf.Clamp(communicationLogHeight, MIN_COMMUNICATION_LOG_HEIGHT, MAX_COMMUNICATION_LOG_HEIGHT);
                    
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
    }
} 