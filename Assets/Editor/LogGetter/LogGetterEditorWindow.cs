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
        
        // フィルター制御テスト用
        private UnityLogEntriesAccessor testAccessor;

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
            
            // フィルター制御テスト用のアクセサーを初期化
            testAccessor = new UnityLogEntriesAccessor();
        }

        private void OnDisable()
        {
            if (presenter != null)
            {
                presenter.OnLogDataUpdated -= OnLogDataUpdated;
                presenter.Dispose();
                presenter = null;
            }
            
            // フィルター制御テスト用のアクセサーを破棄
            if (testAccessor != null)
            {
                testAccessor.Dispose();
                testAccessor = null;
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

            GUILayout.Space(10);

            // フィルター制御セクション
            DrawFilterControlSection();

            GUILayout.Space(10);

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
            // 現在表示中のログの統計を使用（フィルター操作を避ける）
            // LogDisplayDto allLogs = LogGetter.GetConsoleLog(); // この行がフィルター操作を引き起こしていた
            
            int logCount = 0;
            int warningCount = 0;
            int errorCount = 0;
            int assertCount = 0;
            
            foreach (LogEntryDto entry in displayData.LogEntries)
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

        private void DrawFilterControlSection()
        {
            GUILayout.Label("Console フィルター制御（テスト用）", EditorStyles.boldLabel);
            
            if (testAccessor == null)
            {
                GUILayout.Label("UnityLogEntriesAccessor が初期化されていません", EditorStyles.helpBox);
                return;
            }

            // デバッグ情報表示
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"初期化状態: {testAccessor.IsInitialized}", EditorStyles.miniLabel, GUILayout.Width(120));
            GUILayout.Label($"フィルター制御可能: {testAccessor.IsFilterControlAvailable}", EditorStyles.miniLabel, GUILayout.Width(150));
            EditorGUILayout.EndHorizontal();

            if (!testAccessor.IsInitialized)
            {
                GUILayout.Label("UnityLogEntriesAccessor の初期化に失敗しています", EditorStyles.helpBox);
                return;
            }

            GUILayout.Space(5);

            // 現在のログ数表示
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("現在のログ数:", EditorStyles.miniLabel, GUILayout.Width(100));
            int currentLogCount = testAccessor.GetLogCount();
            GUILayout.Label($"{currentLogCount}件", EditorStyles.miniLabel, GUILayout.Width(60));
            
            if (testAccessor.IsFilterControlAvailable)
            {
                GUILayout.Label("フィルター制御:", EditorStyles.miniLabel, GUILayout.Width(100));
                GUILayout.Label("利用可能", EditorStyles.miniLabel, GUILayout.Width(60));
            }
            EditorGUILayout.EndHorizontal();

            // 現在のフィルター状態表示（フィルター制御が利用可能な場合のみ表示）
            if (testAccessor.IsFilterControlAvailable)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("フィルター制御:", EditorStyles.miniLabel, GUILayout.Width(120));
                GUILayout.Label("利用可能（状態表示は未実装）", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }

            GUILayout.Space(5);

            if (!testAccessor.IsFilterControlAvailable)
            {
                GUILayout.Label("フィルター制御機能が利用できません\nリフレクションでのAPI取得に失敗している可能性があります", EditorStyles.helpBox);
                return;
            }

            // フィルター制御ボタン
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Error ON", GUILayout.Width(80)))
            {
                ToggleConsoleFlag("Error", true);
            }
            
            if (GUILayout.Button("Error OFF", GUILayout.Width(80)))
            {
                ToggleConsoleFlag("Error", false);
            }
            
            if (GUILayout.Button("Warning ON", GUILayout.Width(90)))
            {
                ToggleConsoleFlag("Warning", true);
            }
            
            if (GUILayout.Button("Warning OFF", GUILayout.Width(90)))
            {
                ToggleConsoleFlag("Warning", false);
            }
            
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Log ON", GUILayout.Width(80)))
            {
                ToggleConsoleFlag("Log", true);
            }
            
            if (GUILayout.Button("Log OFF", GUILayout.Width(80)))
            {
                ToggleConsoleFlag("Log", false);
            }
            
            if (GUILayout.Button("全フィルター ON", GUILayout.Width(100)))
            {
                testAccessor.EnableAllConsoleFlags();
                Repaint();
            }
            
            if (GUILayout.Button("全フィルター OFF", GUILayout.Width(100)))
            {
                ToggleConsoleFlag("Error", false);
                ToggleConsoleFlag("Warning", false);
                ToggleConsoleFlag("Log", false);
                Repaint();
            }
            
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);
            
            // テスト用ボタン
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("現在の状態を保存", GUILayout.Width(120)))
            {
                testAccessor.SaveConsoleFlags();
            }
            
            if (GUILayout.Button("保存した状態を復元", GUILayout.Width(130)))
            {
                testAccessor.RestoreConsoleFlags();
                Repaint();
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void ToggleConsoleFlag(string flagType, bool enabled)
        {
            Debug.Log($"=== {flagType}フィルター {(enabled ? "ON" : "OFF")} ボタンが押されました ===");
            
            if (testAccessor == null)
            {
                Debug.LogError("testAccessor が null です");
                return;
            }
            
            if (!testAccessor.IsFilterControlAvailable)
            {
                Debug.LogWarning($"フィルター制御が利用できません: {flagType}");
                return;
            }

            try
            {
                Debug.Log($"{flagType}フィルターを{(enabled ? "ON" : "OFF")}に設定中...");
                
                // フィルター状態を設定
                testAccessor.SetConsoleFlag(flagType, enabled);
                
                Debug.Log($"{flagType}フィルターの設定完了");
                Debug.Log("Unity Consoleウィンドウで変化を確認してください");
                
                Repaint();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"{flagType}フィルターの制御中にエラー: {ex.Message}");
                Debug.LogError($"スタックトレース: {ex.StackTrace}");
            }
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