using UnityEngine;
using UnityEditor;

namespace io.github.hatayama.uMCP
{
    public class LogGetterEditorWindow : EditorWindow
    {
        private LogGetterPresenter presenter;
        private Vector2 scrollPosition;
        private LogDisplayDto displayData;
        
        // ログタイプフィルタ用
        private string selectedLogType = "All";
        private readonly string[] logTypeOptions = { "All", "Log", "Warning", "Error", "Assert" };

        [MenuItem("Tools/ログゲッター/Window")]
        public static void ShowWindow()
        {
            LogGetterEditorWindow window = GetWindow<LogGetterEditorWindow>();
            window.titleContent = new GUIContent("ログゲッター");
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

            GUILayout.Label("Unity Console ログゲッター", EditorStyles.boldLabel);
            GUILayout.Space(10);

            // ログタイプ選択UI
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("ログタイプ:", GUILayout.Width(80));
            
            int selectedIndex = System.Array.IndexOf(logTypeOptions, selectedLogType);
            if (selectedIndex == -1) selectedIndex = 0;
            
            selectedIndex = EditorGUILayout.Popup(selectedIndex, logTypeOptions, GUILayout.Width(100));
            selectedLogType = logTypeOptions[selectedIndex];
            
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);

            // ログ取得ボタン
            string buttonText = selectedLogType == "All" ? "全ログ取得" : $"{selectedLogType}ログ取得";
            if (GUILayout.Button(buttonText, GUILayout.Height(30)))
            {
                presenter.GetLogs(selectedLogType);
            }

            GUILayout.Space(5);

            if (GUILayout.Button("ログクリア", GUILayout.Height(25)))
            {
                presenter.ClearLogs();
            }

            GUILayout.Space(10);

            // ログ統計表示
            DrawLogStatistics();

            GUILayout.Label($"表示ログ数: {displayData.TotalCount}件", EditorStyles.boldLabel);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(400));

            foreach (LogEntryDto logEntry in displayData.LogEntries)
            {
                DrawLogEntry(logEntry);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawLogStatistics()
        {
            // 全ログの統計を取得
            LogDisplayDto allLogs = LogGetter.GetConsoleLog();
            
            int logCount = 0;
            int warningCount = 0;
            int errorCount = 0;
            int assertCount = 0;
            
            foreach (LogEntryDto entry in allLogs.LogEntries)
            {
                switch (entry.LogType)
                {
                    case "Log":
                        logCount++;
                        break;
                    case "Warning":
                        warningCount++;
                        break;
                    case "Error":
                        errorCount++;
                        break;
                    case "Assert":
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

        private GUIStyle GetLogStyle(string logType)
        {
            switch (logType)
            {
                case "Error":
                case "Exception":
                case "Assert":
                    return EditorStyles.helpBox;
                case "Warning":
                    return EditorStyles.helpBox;
                default:
                    return EditorStyles.helpBox;
            }
        }
    }
} 