using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// View layer for McpEditorWindow - handles only UI rendering
    /// Related classes: McpEditorWindow (Presenter+Model), McpEditorWindowViewData
    /// Design document: ARCHITECTURE.md - UI layer separation pattern
    /// </summary>
    public class McpEditorWindowView
    {
        public void DrawServerStatus(ServerStatusData data)
        {
            EditorGUILayout.BeginVertical("box");
            
            GUIStyle statusStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = data.StatusColor },
                fontStyle = FontStyle.Bold
            };
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Server Status:", EditorStyles.boldLabel, GUILayout.Width(McpUIConstants.LABEL_WIDTH_SERVER_STATUS));
            EditorGUILayout.LabelField($"{data.Status}", statusStyle, GUILayout.Width(McpUIConstants.LABEL_WIDTH_STATUS));
            EditorGUILayout.LabelField($"Port: {data.Port}");
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        public void DrawServerControls(ServerControlsData data, Action toggleServerCallback, Action<bool> autoStartCallback, Action<int> portChangeCallback)
        {
            EditorGUILayout.BeginVertical("box");
            
            // Auto start checkbox
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Auto Start Server", GUILayout.Width(200));
            bool newAutoStart = EditorGUILayout.Toggle(data.AutoStartServer, GUILayout.Width(20));
            if (newAutoStart != data.AutoStartServer)
            {
                autoStartCallback?.Invoke(newAutoStart);
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // Port settings
            EditorGUI.BeginDisabledGroup(data.IsServerRunning);
            int newPort = EditorGUILayout.IntField("Port:", data.CustomPort);
            if (newPort != data.CustomPort)
            {
                portChangeCallback?.Invoke(newPort);
            }
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.Space();
            
            // Toggle Server button
            string buttonText = data.IsServerRunning ? "Stop Server" : "Start Server";
            Color originalColor = GUI.backgroundColor;
            
            // Change button color based on server state
            if (data.IsServerRunning)
            {
                GUI.backgroundColor = new Color(1f, 0.7f, 0.7f); // Light red for stop
            }
            else
            {
                GUI.backgroundColor = new Color(0.7f, 1f, 0.7f); // Light green for start
            }
            
            if (GUILayout.Button(buttonText, GUILayout.Height(McpUIConstants.BUTTON_HEIGHT_LARGE)))
            {
                toggleServerCallback?.Invoke();
            }
            
            // Restore original color
            GUI.backgroundColor = originalColor;
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        public void DrawConnectedToolsSection(ConnectedToolsData data, Action<bool> toggleFoldoutCallback)
        {
            EditorGUILayout.BeginVertical("box");
            
            bool newShowFoldout = EditorGUILayout.Foldout(data.ShowFoldout, McpUIConstants.CONNECTED_TOOLS_FOLDOUT_TEXT, true);
            if (newShowFoldout != data.ShowFoldout)
            {
                toggleFoldoutCallback?.Invoke(newShowFoldout);
            }
            
            if (data.ShowFoldout)
            {
                EditorGUILayout.Space();
                DrawConnectionStatus(data);
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        public void DrawEditorConfigSection(EditorConfigData data, Action<McpEditorType> editorChangeCallback, Action<string> configureCallback, Action<bool> foldoutCallback)
        {
            EditorGUILayout.BeginVertical("box");
            
            bool showFoldout = EditorGUILayout.Foldout(data.ShowFoldout, "LLM Tool Settings", true);
            if (showFoldout != data.ShowFoldout)
            {
                foldoutCallback?.Invoke(showFoldout);
            }
            
            if (showFoldout)
            {
                EditorGUILayout.Space();
                
                McpEditorType newSelectedEditor = (McpEditorType)EditorGUILayout.EnumPopup("Target:", data.SelectedEditor);
                if (newSelectedEditor != data.SelectedEditor)
                {
                    editorChangeCallback?.Invoke(newSelectedEditor);
                }
                
                string editorName = GetEditorDisplayName(data.SelectedEditor);
                string buttonText = data.IsServerRunning ? $"Auto-configure {editorName} settings (Port {data.CurrentPort})" : $"Auto-configure {editorName} settings";
                if (GUILayout.Button(buttonText))
                {
                    configureCallback?.Invoke(editorName);
                }
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        private void DrawConnectionStatus(ConnectedToolsData data)
        {
            if (!data.IsServerRunning)
            {
                EditorGUILayout.HelpBox("Server is not running. Start the server to see connected tools.", MessageType.Warning);
                return;
            }
            
            if (data.ShowReconnectingUI)
            {
                EditorGUILayout.HelpBox(McpUIConstants.RECONNECTING_MESSAGE, MessageType.Info);
                return;
            }
            
            if (data.Clients != null && data.Clients.Count > 0)
            {
                foreach (ConnectedClient client in data.Clients)
                {
                    DrawConnectedClientItem(client);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No LLM tools currently connected.", MessageType.Info);
            }
        }

        private void DrawConnectedClientItem(ConnectedClient client)
        {
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.BeginHorizontal();
            
            EditorGUILayout.LabelField(McpUIConstants.CLIENT_ICON + client.ClientName, new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold });
            
            GUILayout.FlexibleSpace();
            
            GUIStyle endpointStyle = new GUIStyle(EditorStyles.miniLabel);
            endpointStyle.normal.textColor = Color.gray;
            string pidInfo = client.ProcessId > McpConstants.UNKNOWN_PROCESS_ID ? $" (PID: {client.ProcessId})" : "";
            EditorGUILayout.LabelField(McpUIConstants.ENDPOINT_ARROW + client.Endpoint + pidInfo, endpointStyle);
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(McpUIConstants.CLIENT_ITEM_SPACING);
        }

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

#if UMCP_DEBUG
        public void DrawDeveloperTools(DeveloperToolsData data, Action<bool> foldoutCallback, Action<bool> devModeCallback, Action<bool> mcpLogsCallback, Action<bool> commLogsCallback, Action showDebugCallback, Action notifyChangesCallback, Action rebuildCallback)
        {
            EditorGUILayout.BeginVertical("box");
            
            bool newShowFoldout = EditorGUILayout.Foldout(data.ShowFoldout, "Developer Tools", true);
            if (newShowFoldout != data.ShowFoldout)
            {
                foldoutCallback?.Invoke(newShowFoldout);
            }
            
            if (data.ShowFoldout)
            {
                EditorGUILayout.Space();
                
                // TypeScript Development Mode settings
                EditorGUILayout.LabelField("TypeScript Server Settings", EditorStyles.boldLabel);
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Enable Development Mode", GUILayout.Width(200));
                bool newEnableDevelopmentMode = EditorGUILayout.Toggle(data.EnableDevelopmentMode, GUILayout.Width(20));
                if (newEnableDevelopmentMode != data.EnableDevelopmentMode)
                {
                    devModeCallback?.Invoke(newEnableDevelopmentMode);
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.HelpBox(
                    data.EnableDevelopmentMode 
                        ? "Development Mode: Debug tools (mcp-ping, get-unity-commands) will be available in Cursor"
                        : "Production Mode: Only essential tools will be available in Cursor",
                    data.EnableDevelopmentMode ? MessageType.Info : MessageType.Warning
                );
                
                EditorGUILayout.Space();
                
                // Log control toggle
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Enable MCP Logs", GUILayout.Width(200));
                bool newEnableMcpLogs = EditorGUILayout.Toggle(data.EnableMcpLogs, GUILayout.Width(20));
                if (newEnableMcpLogs != data.EnableMcpLogs)
                {
                    mcpLogsCallback?.Invoke(newEnableMcpLogs);
                }
                EditorGUILayout.EndHorizontal();
                
                // Communication logs toggle
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Enable Communication Logs", GUILayout.Width(200));
                bool newEnableCommunicationLogs = EditorGUILayout.Toggle(data.EnableCommunicationLogs, GUILayout.Width(20));
                if (newEnableCommunicationLogs != data.EnableCommunicationLogs)
                {
                    commLogsCallback?.Invoke(newEnableCommunicationLogs);
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space();
                
                // Communication logs section (only when enabled)
                if (data.EnableCommunicationLogs)
                {
                    DrawCommunicationLogs(data);
                    EditorGUILayout.Space();
                }
                
                // Debug information display button
                if (GUILayout.Button("Show Debug Info"))
                {
                    showDebugCallback?.Invoke();
                }
                
                EditorGUILayout.Space();
                
                // Command notification button
                bool isServerRunning = true; // TODO: Get from data
                EditorGUI.BeginDisabledGroup(!isServerRunning);
                if (GUILayout.Button("Notify Command Changes to LLM Tools", GUILayout.Height(McpUIConstants.BUTTON_HEIGHT_MEDIUM)))
                {
                    notifyChangesCallback?.Invoke();
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
                    rebuildCallback?.Invoke();
                }
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        private void DrawCommunicationLogs(DeveloperToolsData data)
        {
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.BeginHorizontal();
            bool showCommLogs = EditorGUILayout.Foldout(data.ShowCommunicationLogs, "Communication Logs", true);
            
            if (showCommLogs)
            {
                if (GUILayout.Button("Clear", GUILayout.Width(McpUIConstants.BUTTON_WIDTH_CLEAR)))
                {
                    McpCommunicationLogger.ClearLogs();
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            if (showCommLogs)
            {
                EditorGUILayout.Space();
                
                if (data.Logs.Count == 0)
                {
                    EditorGUILayout.HelpBox("No communication logs yet", MessageType.Info);
                }
                else
                {
                    Vector2 scrollPos = EditorGUILayout.BeginScrollView(
                        data.CommunicationLogScrollPosition, 
                        GUILayout.Height(data.CommunicationLogHeight)
                    );
                    
                    int displayCount = Mathf.Min(data.Logs.Count, McpUIConstants.MAX_COMMUNICATION_LOG_ENTRIES);
                    int startIndex = data.Logs.Count - 1;
                    int endIndex = data.Logs.Count - displayCount;
                    
                    for (int i = startIndex; i >= endIndex; i--)
                    {
                        McpCommunicationLogEntry log = data.Logs[i];
                        DrawLogEntry(log, data.RequestScrollPositions, data.ResponseScrollPositions);
                        EditorGUILayout.Space(5);
                    }
                    
                    EditorGUILayout.EndScrollView();
                }
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawLogEntry(McpCommunicationLogEntry log, Dictionary<string, Vector2> requestScrollPositions, Dictionary<string, Vector2> responseScrollPositions)
        {
            EditorGUILayout.BeginVertical("box");
            
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
            
            if (log.IsExpanded)
            {
                EditorGUILayout.Space();
                
                EditorGUILayout.LabelField("Request:", EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical("box");
                string formattedRequest = FormatJson(log.Request);
                string requestKey = $"{log.CommandName}_{log.Timestamp:HHmmss}_request";
                DrawJsonContentWithScroll(formattedRequest, requestKey, requestScrollPositions);
                EditorGUILayout.EndVertical();
                
                EditorGUILayout.Space();
                
                EditorGUILayout.LabelField("Response:", EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical("box");
                string formattedResponse = FormatJson(log.Response);
                string responseKey = $"{log.CommandName}_{log.Timestamp:HHmmss}_response";
                DrawJsonContentWithScroll(formattedResponse, responseKey, responseScrollPositions);
                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.EndVertical();
        }

        private string FormatJson(string json)
        {
            try
            {
                Newtonsoft.Json.Linq.JObject parsed = Newtonsoft.Json.Linq.JObject.Parse(json);
                return parsed.ToString(Newtonsoft.Json.Formatting.Indented);
            }
            catch
            {
                return json;
            }
        }

        private void DrawJsonContentWithScroll(string jsonContent, string scrollKey, Dictionary<string, Vector2> scrollPositions)
        {
            GUIStyle textAreaStyle = new GUIStyle(EditorStyles.textArea);
            textAreaStyle.wordWrap = true;
            
            string[] lines = jsonContent.Split('\n');
            int lineCount = lines.Length;
            float lineHeight = EditorGUIUtility.singleLineHeight;
            
            float fixedHeight = McpUIConstants.JSON_SCROLL_FIXED_HEIGHT;
            float contentHeight = lineCount * lineHeight + McpUIConstants.JSON_CONTENT_PADDING;
            
            if (!scrollPositions.ContainsKey(scrollKey))
            {
                scrollPositions[scrollKey] = Vector2.zero;
            }
            
            scrollPositions[scrollKey] = EditorGUILayout.BeginScrollView(
                scrollPositions[scrollKey], 
                GUILayout.Height(fixedHeight)
            );
            
            EditorGUILayout.SelectableLabel(jsonContent, textAreaStyle, GUILayout.Height(contentHeight));
            
            EditorGUILayout.EndScrollView();
        }
#endif
    }
} 