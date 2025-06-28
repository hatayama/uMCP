using UnityEngine;
using System.Collections.Generic;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// State management objects for McpEditorWindow
    /// Implements State Object pattern as Model layer in MVP architecture
    /// Uses get-only properties for with expression compatibility
    /// Related classes:
    /// - McpEditorWindow: Presenter layer that uses these state objects
    /// - McpEditorWindowView: View layer for UI rendering
    /// - McpEditorModel: Model layer service for managing state transitions
    /// </summary>

    /// <summary>
    /// UI state data for McpEditorWindow
    /// </summary>
    public record UIState
    {
        public int CustomPort { get; }
        public bool AutoStartServer { get; }
        public bool ShowLLMToolSettings { get; }
        public bool ShowConnectedTools { get; }
        public McpEditorType SelectedEditorType { get; }
        public Vector2 MainScrollPosition { get; }

        public UIState(
            int customPort = McpServerConfig.DEFAULT_PORT,
            bool autoStartServer = false,
            bool showLLMToolSettings = true,
            bool showConnectedTools = true,
            McpEditorType selectedEditorType = McpEditorType.Cursor,
            Vector2 mainScrollPosition = default)
        {
            CustomPort = customPort;
            AutoStartServer = autoStartServer;
            ShowLLMToolSettings = showLLMToolSettings;
            ShowConnectedTools = showConnectedTools;
            SelectedEditorType = selectedEditorType;
            MainScrollPosition = mainScrollPosition;
        }
    }

    /// <summary>
    /// Runtime state data for McpEditorWindow
    /// Tracks dynamic state during editor window operation
    /// </summary>
    public record RuntimeState
    {
        public bool NeedsRepaint { get; }
        public bool IsPostCompileMode { get; }
        public bool LastServerRunning { get; }
        public int LastServerPort { get; }
        public int LastConnectedClientsCount { get; }
        public string LastClientsInfoHash { get; }

        public RuntimeState(
            bool needsRepaint = false,
            bool isPostCompileMode = false,
            bool lastServerRunning = false,
            int lastServerPort = 0,
            int lastConnectedClientsCount = 0,
            string lastClientsInfoHash = "")
        {
            NeedsRepaint = needsRepaint;
            IsPostCompileMode = isPostCompileMode;
            LastServerRunning = lastServerRunning;
            LastServerPort = lastServerPort;
            LastConnectedClientsCount = lastConnectedClientsCount;
            LastClientsInfoHash = lastClientsInfoHash;
        }
    }

#if UMCP_DEBUG
    /// <summary>
    /// Debug state data for McpEditorWindow
    /// Available only in debug builds
    /// </summary>
    public record DebugState
    {
        public bool ShowDeveloperTools { get; }
        public bool EnableCommunicationLogs { get; }
        public bool ShowCommunicationLogs { get; }
        public bool EnableMcpLogs { get; }
        public bool EnableDevelopmentMode { get; }
        public Vector2 CommunicationLogScrollPosition { get; }
        public float CommunicationLogHeight { get; }
        public Dictionary<string, Vector2> RequestScrollPositions { get; }
        public Dictionary<string, Vector2> ResponseScrollPositions { get; }

        public DebugState(
            bool showDeveloperTools = false,
            bool enableCommunicationLogs = false,
            bool showCommunicationLogs = false,
            bool enableMcpLogs = false,
            bool enableDevelopmentMode = false,
            Vector2 communicationLogScrollPosition = default,
            float communicationLogHeight = McpUIConstants.DEFAULT_COMMUNICATION_LOG_HEIGHT,
            Dictionary<string, Vector2> requestScrollPositions = null,
            Dictionary<string, Vector2> responseScrollPositions = null)
        {
            ShowDeveloperTools = showDeveloperTools;
            EnableCommunicationLogs = enableCommunicationLogs;
            ShowCommunicationLogs = showCommunicationLogs;
            EnableMcpLogs = enableMcpLogs;
            EnableDevelopmentMode = enableDevelopmentMode;
            CommunicationLogScrollPosition = communicationLogScrollPosition;
            CommunicationLogHeight = communicationLogHeight;
            RequestScrollPositions = requestScrollPositions ?? new Dictionary<string, Vector2>();
            ResponseScrollPositions = responseScrollPositions ?? new Dictionary<string, Vector2>();
        }
    }
#endif
} 