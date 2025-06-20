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
                LogGetter.ClearCustomLogs();
            }

            GUILayout.Space(5);

            // テスト用ログ生成セクション
            GUILayout.Label("テスト用ログ生成", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Debug.Log生成", GUILayout.Height(25)))
            {
                UnityEngine.Debug.Log($"テスト用Logメッセージ - {System.DateTime.Now:HH:mm:ss}");
            }
            if (GUILayout.Button("Debug.LogWarning生成", GUILayout.Height(25)))
            {
                UnityEngine.Debug.LogWarning($"テスト用Warningメッセージ - {System.DateTime.Now:HH:mm:ss}");
            }
            if (GUILayout.Button("Debug.LogError生成", GUILayout.Height(25)))
            {
                UnityEngine.Debug.LogError($"テスト用Errorメッセージ - {System.DateTime.Now:HH:mm:ss}");
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("複数ログ一括生成", GUILayout.Height(25)))
            {
                string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
                UnityEngine.Debug.Log($"一括生成テスト Log 1 - {timestamp}");
                UnityEngine.Debug.Log($"一括生成テスト Log 2 - {timestamp}");
                UnityEngine.Debug.LogWarning($"一括生成テスト Warning - {timestamp}");
                UnityEngine.Debug.LogError($"一括生成テスト Error - {timestamp}");
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
            // 現在表示中のログの統計を表示（無限ループ回避）
            LogDisplayDto allLogs = displayData;
            
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