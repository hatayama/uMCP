using UnityEngine;
using UnityEditor;

namespace io.github.hatayama.uMCP
{
    public class LogGetterEditorWindow : EditorWindow
    {
        private LogGetterPresenter presenter;
        private Vector2 scrollPosition;
        private LogDisplayDto displayData;
        
        // For log type filtering
        private string selectedLogType = "All";
        private readonly string[] logTypeOptions = { "All", "Log", "Warning", "Error", "Assert" };

        [MenuItem("uMCP/LogGetter/Window")]
        public static void ShowWindow()
        {
            LogGetterEditorWindow window = GetWindow<LogGetterEditorWindow>();
            window.titleContent = new GUIContent("Log Getter");
            window.Show();
        }

        private void OnEnable()
        {
            presenter = new LogGetterPresenter();
            presenter.OnLogDataUpdated += OnLogDataUpdated;
            displayData = new LogDisplayDto(new LogEntryDto[0], 0);
            
        }

        private void OnDisable()
        {
            if (presenter != null)
            {
                presenter.OnLogDataUpdated -= OnLogDataUpdated;
                presenter.Dispose();
                presenter = null;
            }
        }

        private void OnLogDataUpdated(LogDisplayDto data)
        {
            displayData = data;
            Repaint();
        }

        private void OnGUI()
        {
            if (presenter == null) return;

            GUILayout.Label("Unity Console Log Getter", EditorStyles.boldLabel);
            GUILayout.Space(10);

            // Log type selection UI
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Log Type:", GUILayout.Width(80));
            
            int selectedIndex = System.Array.IndexOf(logTypeOptions, selectedLogType);
            if (selectedIndex == -1) selectedIndex = 0;
            
            selectedIndex = EditorGUILayout.Popup(selectedIndex, logTypeOptions, GUILayout.Width(100));
            selectedLogType = logTypeOptions[selectedIndex];
            
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);

            // Get Logs button
            string buttonText = selectedLogType == "All" ? "Get All Logs" : $"Get {selectedLogType} Logs";
            if (GUILayout.Button(buttonText, GUILayout.Height(30)))
            {
                McpLogType mcpLogType = ConvertStringToMcpLogType(selectedLogType);
                presenter.GetLogs(mcpLogType);
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Clear Logs", GUILayout.Height(25)))
            {
                presenter.ClearLogs();
                LogGetter.ClearCustomLogs();
            }

            GUILayout.Space(5);

            // Test log generation section
            GUILayout.Label("Generate Test Logs", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Generate Debug.Log", GUILayout.Height(25)))
            {
                UnityEngine.Debug.Log($"Test Log message - {System.DateTime.Now:HH:mm:ss}");
            }
            if (GUILayout.Button("Generate Debug.LogWarning", GUILayout.Height(25)))
            {
                UnityEngine.Debug.LogWarning($"Test Warning message - {System.DateTime.Now:HH:mm:ss}");
            }
            if (GUILayout.Button("Generate Debug.LogError", GUILayout.Height(25)))
            {
                UnityEngine.Debug.LogError($"Test Error message - {System.DateTime.Now:HH:mm:ss}");
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Generate Multiple Logs at Once", GUILayout.Height(25)))
            {
                string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
                UnityEngine.Debug.Log($"Bulk generation test Log 1 - {timestamp}");
                UnityEngine.Debug.Log($"Bulk generation test Log 2 - {timestamp}");
                UnityEngine.Debug.LogWarning($"Bulk generation test Warning - {timestamp}");
                UnityEngine.Debug.LogError($"Bulk generation test Error - {timestamp}");
            }

            GUILayout.Space(10);

            // Display log statistics
            DrawLogStatistics();

            GUILayout.Label($"Displayed Logs: {displayData.TotalCount} items", EditorStyles.boldLabel);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(400));

            foreach (LogEntryDto logEntry in displayData.LogEntries)
            {
                DrawLogEntry(logEntry);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawLogStatistics()
        {
            // Display statistics of currently displayed logs (to avoid infinite loops)
            LogDisplayDto allLogs = displayData;
            
            int logCount = 0;
            int warningCount = 0;
            int errorCount = 0;
            int assertCount = 0;
            
            foreach (LogEntryDto entry in allLogs.LogEntries)
            {
                switch (entry.LogType)
                {
                    case McpLogType.Log:
                        logCount++;
                        break;
                    case McpLogType.Warning:
                        warningCount++;
                        break;
                    case McpLogType.Error:
                        errorCount++;
                        break;
                    case McpLogType.None:
                        assertCount++;
                        break;
                }
            }
            
            // 統計表示
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"Console統計:", EditorStyles.miniLabel, GUILayout.Width(80));
            GUILayout.Label($"Log: {logCount}", EditorStyles.miniLabel, GUILayout.Width(60));
            GUILayout.Label($"Warning: {warningCount}", EditorStyles.miniLabel, GUILayout.Width(80));
            GUILayout.Label($"Error: {errorCount}", EditorStyles.miniLabel, GUILayout.Width(70));
            if (assertCount > 0)
            {
                GUILayout.Label($"Assert: {assertCount}", EditorStyles.miniLabel, GUILayout.Width(70));
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);
        }

        private void DrawLogEntry(LogEntryDto logEntry)
        {
            GUIStyle style = GetLogStyle(logEntry.LogType);
            
            EditorGUILayout.BeginVertical(style);
            
            EditorGUILayout.LabelField($"[{logEntry.LogType}] {logEntry.Message}", EditorStyles.wordWrappedLabel);
            
            if (!string.IsNullOrEmpty(logEntry.StackTrace))
            {
                EditorGUILayout.LabelField("Stack Trace:", EditorStyles.miniLabel);
                EditorGUILayout.TextArea(logEntry.StackTrace, EditorStyles.textArea, GUILayout.Height(60));
            }
            
            EditorGUILayout.EndVertical();
            GUILayout.Space(2);
        }

        private McpLogType ConvertStringToMcpLogType(string logType)
        {
            return logType switch
            {
                "Error" => McpLogType.Error,
                "Warning" => McpLogType.Warning,
                "Log" => McpLogType.Log,
                "All" => McpLogType.All,
                _ => McpLogType.All
            };
        }

        private GUIStyle GetLogStyle(McpLogType logType)
        {
            switch (logType)
            {
                case McpLogType.Error:
                case McpLogType.None:
                    return EditorStyles.helpBox;
                case McpLogType.Warning:
                    return EditorStyles.helpBox;
                default:
                    return EditorStyles.helpBox;
            }
        }
    }
} 