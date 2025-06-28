using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Model layer for McpEditorWindow in MVP architecture
    /// Handles state management and business logic using immutable state objects
    /// Related classes:
    /// - McpEditorWindowState: State objects managed by this model
    /// - McpEditorWindow: Presenter that uses this model
    /// - McpEditorWindowView: View layer for UI rendering
    /// - McpEditorSettings: Persistent settings storage
    /// </summary>
    public class McpEditorModel
    {
        public UIState UI { get; private set; }
        public RuntimeState Runtime { get; private set; }
#if UMCP_DEBUG
        public DebugState Debug { get; private set; }
#endif

        public McpEditorModel()
        {
            UI = new UIState();
            Runtime = new RuntimeState();
#if UMCP_DEBUG
            Debug = new DebugState();
#endif
        }

        /// <summary>
        /// Update UI state with new values
        /// </summary>
        /// <param name="updater">Function to update UI state</param>
        public void UpdateUIState(Func<UIState, UIState> updater)
        {
            UI = updater(UI);
        }

        /// <summary>
        /// Update runtime state with new values
        /// </summary>
        /// <param name="updater">Function to update runtime state</param>
        public void UpdateRuntimeState(Func<RuntimeState, RuntimeState> updater)
        {
            Runtime = updater(Runtime);
        }

#if UMCP_DEBUG
        /// <summary>
        /// Update debug state with new values
        /// </summary>
        /// <param name="updater">Function to update debug state</param>
        public void UpdateDebugState(Func<DebugState, DebugState> updater)
        {
            Debug = updater(Debug);
        }
#endif

        /// <summary>
        /// Load state from persistent settings
        /// </summary>
        public void LoadFromSettings()
        {
            McpEditorSettingsData settings = McpEditorSettings.GetSettings();
            
            UpdateUIState(ui => new UIState(
                customPort: settings.customPort,
                autoStartServer: settings.autoStartServer,
                showLLMToolSettings: ui.ShowLLMToolSettings,
                showConnectedTools: ui.ShowConnectedTools,
                selectedEditorType: ui.SelectedEditorType,
                mainScrollPosition: ui.MainScrollPosition));

#if UMCP_DEBUG
            UpdateDebugState(debug => new DebugState(
                showDeveloperTools: settings.showDeveloperTools,
                enableMcpLogs: settings.enableMcpLogs,
                enableCommunicationLogs: settings.enableCommunicationLogs,
                enableDevelopmentMode: settings.enableDevelopmentMode,
                showCommunicationLogs: debug.ShowCommunicationLogs,
                communicationLogScrollPosition: debug.CommunicationLogScrollPosition,
                communicationLogHeight: debug.CommunicationLogHeight,
                requestScrollPositions: debug.RequestScrollPositions,
                responseScrollPositions: debug.ResponseScrollPositions));

            // Synchronize McpLogger settings
            McpLogger.EnableDebugLog = Debug.EnableMcpLogs;
#endif
        }

        /// <summary>
        /// Save current UI state to persistent settings
        /// </summary>
        public void SaveToSettings()
        {
            McpEditorSettings.SetCustomPort(UI.CustomPort);
            McpEditorSettings.SetAutoStartServer(UI.AutoStartServer);

#if UMCP_DEBUG
            McpEditorSettings.SetShowDeveloperTools(Debug.ShowDeveloperTools);
            McpEditorSettings.SetEnableMcpLogs(Debug.EnableMcpLogs);
            McpEditorSettings.SetEnableCommunicationLogs(Debug.EnableCommunicationLogs);
            McpEditorSettings.SetEnableDevelopmentMode(Debug.EnableDevelopmentMode);
#endif
        }

        /// <summary>
        /// Load state from Unity SessionState
        /// </summary>
        public void LoadFromSessionState()
        {
            McpEditorType selectedEditor = (McpEditorType)SessionState.GetInt(
                McpConstants.SESSION_KEY_SELECTED_EDITOR_TYPE, 
                (int)McpEditorType.Cursor);

            UpdateUIState(ui => new UIState(
                customPort: ui.CustomPort,
                autoStartServer: ui.AutoStartServer,
                showLLMToolSettings: ui.ShowLLMToolSettings,
                showConnectedTools: ui.ShowConnectedTools,
                selectedEditorType: selectedEditor,
                mainScrollPosition: ui.MainScrollPosition));

#if UMCP_DEBUG
            float communicationLogHeight = SessionState.GetFloat(
                McpConstants.SESSION_KEY_COMMUNICATION_LOG_HEIGHT, 
                McpUIConstants.DEFAULT_COMMUNICATION_LOG_HEIGHT);

            UpdateDebugState(debug => new DebugState(
                showDeveloperTools: debug.ShowDeveloperTools,
                enableMcpLogs: debug.EnableMcpLogs,
                enableCommunicationLogs: debug.EnableCommunicationLogs,
                enableDevelopmentMode: debug.EnableDevelopmentMode,
                showCommunicationLogs: debug.ShowCommunicationLogs,
                communicationLogScrollPosition: debug.CommunicationLogScrollPosition,
                communicationLogHeight: communicationLogHeight,
                requestScrollPositions: debug.RequestScrollPositions,
                responseScrollPositions: debug.ResponseScrollPositions));
#endif
        }

        /// <summary>
        /// Save current state to Unity SessionState
        /// </summary>
        public void SaveToSessionState()
        {
            SessionState.SetInt(McpConstants.SESSION_KEY_SELECTED_EDITOR_TYPE, (int)UI.SelectedEditorType);

#if UMCP_DEBUG
            SessionState.SetFloat(McpConstants.SESSION_KEY_COMMUNICATION_LOG_HEIGHT, Debug.CommunicationLogHeight);
#endif
        }

        /// <summary>
        /// Initialize post-compile mode
        /// </summary>
        public void EnablePostCompileMode()
        {
            UpdateRuntimeState(runtime => new RuntimeState(
                isPostCompileMode: true,
                needsRepaint: true,
                lastServerRunning: runtime.LastServerRunning,
                lastServerPort: runtime.LastServerPort,
                lastConnectedClientsCount: runtime.LastConnectedClientsCount,
                lastClientsInfoHash: runtime.LastClientsInfoHash));
        }

        /// <summary>
        /// Exit post-compile mode
        /// </summary>
        public void DisablePostCompileMode()
        {
            UpdateRuntimeState(runtime => new RuntimeState(
                isPostCompileMode: false,
                needsRepaint: runtime.NeedsRepaint,
                lastServerRunning: runtime.LastServerRunning,
                lastServerPort: runtime.LastServerPort,
                lastConnectedClientsCount: runtime.LastConnectedClientsCount,
                lastClientsInfoHash: runtime.LastClientsInfoHash));
        }

        /// <summary>
        /// Mark that UI repaint is needed
        /// </summary>
        public void RequestRepaint()
        {
            UpdateRuntimeState(runtime => new RuntimeState(
                isPostCompileMode: runtime.IsPostCompileMode,
                needsRepaint: true,
                lastServerRunning: runtime.LastServerRunning,
                lastServerPort: runtime.LastServerPort,
                lastConnectedClientsCount: runtime.LastConnectedClientsCount,
                lastClientsInfoHash: runtime.LastClientsInfoHash));
        }

        /// <summary>
        /// Clear repaint request
        /// </summary>
        public void ClearRepaintRequest()
        {
            UpdateRuntimeState(runtime => new RuntimeState(
                isPostCompileMode: runtime.IsPostCompileMode,
                needsRepaint: false,
                lastServerRunning: runtime.LastServerRunning,
                lastServerPort: runtime.LastServerPort,
                lastConnectedClientsCount: runtime.LastConnectedClientsCount,
                lastClientsInfoHash: runtime.LastClientsInfoHash));
        }

        /// <summary>
        /// Update server state tracking for change detection
        /// </summary>
        public void UpdateServerStateTracking(bool isRunning, int port, int clientCount, string clientsHash)
        {
            UpdateRuntimeState(runtime => new RuntimeState(
                isPostCompileMode: runtime.IsPostCompileMode,
                needsRepaint: runtime.NeedsRepaint,
                lastServerRunning: isRunning,
                lastServerPort: port,
                lastConnectedClientsCount: clientCount,
                lastClientsInfoHash: clientsHash));
        }

#if UMCP_DEBUG
        /// <summary>
        /// Update communication log scroll position
        /// </summary>
        public void UpdateCommunicationLogScrollPosition(Vector2 scrollPosition)
        {
            UpdateDebugState(debug => new DebugState(
                showDeveloperTools: debug.ShowDeveloperTools,
                enableCommunicationLogs: debug.EnableCommunicationLogs,
                showCommunicationLogs: debug.ShowCommunicationLogs,
                enableMcpLogs: debug.EnableMcpLogs,
                enableDevelopmentMode: debug.EnableDevelopmentMode,
                communicationLogScrollPosition: scrollPosition,
                communicationLogHeight: debug.CommunicationLogHeight,
                requestScrollPositions: debug.RequestScrollPositions,
                responseScrollPositions: debug.ResponseScrollPositions));
        }

        /// <summary>
        /// Update communication log height
        /// </summary>
        public void UpdateCommunicationLogHeight(float height)
        {
            UpdateDebugState(debug => new DebugState(
                showDeveloperTools: debug.ShowDeveloperTools,
                enableCommunicationLogs: debug.EnableCommunicationLogs,
                showCommunicationLogs: debug.ShowCommunicationLogs,
                enableMcpLogs: debug.EnableMcpLogs,
                enableDevelopmentMode: debug.EnableDevelopmentMode,
                communicationLogScrollPosition: debug.CommunicationLogScrollPosition,
                communicationLogHeight: height,
                requestScrollPositions: debug.RequestScrollPositions,
                responseScrollPositions: debug.ResponseScrollPositions));
        }

        /// <summary>
        /// Update request scroll position for specific log ID
        /// </summary>
        public void UpdateRequestScrollPosition(string logId, Vector2 scrollPosition)
        {
            Dictionary<string, Vector2> newRequestScrollPositions = new(Debug.RequestScrollPositions);
            newRequestScrollPositions[logId] = scrollPosition;

            UpdateDebugState(debug => new DebugState(
                showDeveloperTools: debug.ShowDeveloperTools,
                enableCommunicationLogs: debug.EnableCommunicationLogs,
                showCommunicationLogs: debug.ShowCommunicationLogs,
                enableMcpLogs: debug.EnableMcpLogs,
                enableDevelopmentMode: debug.EnableDevelopmentMode,
                communicationLogScrollPosition: debug.CommunicationLogScrollPosition,
                communicationLogHeight: debug.CommunicationLogHeight,
                requestScrollPositions: newRequestScrollPositions,
                responseScrollPositions: debug.ResponseScrollPositions));
        }

        /// <summary>
        /// Update response scroll position for specific log ID
        /// </summary>
        public void UpdateResponseScrollPosition(string logId, Vector2 scrollPosition)
        {
            Dictionary<string, Vector2> newResponseScrollPositions = new(Debug.ResponseScrollPositions);
            newResponseScrollPositions[logId] = scrollPosition;

            UpdateDebugState(debug => new DebugState(
                showDeveloperTools: debug.ShowDeveloperTools,
                enableCommunicationLogs: debug.EnableCommunicationLogs,
                showCommunicationLogs: debug.ShowCommunicationLogs,
                enableMcpLogs: debug.EnableMcpLogs,
                enableDevelopmentMode: debug.EnableDevelopmentMode,
                communicationLogScrollPosition: debug.CommunicationLogScrollPosition,
                communicationLogHeight: debug.CommunicationLogHeight,
                requestScrollPositions: debug.RequestScrollPositions,
                responseScrollPositions: newResponseScrollPositions));
        }
#endif
    }
} 