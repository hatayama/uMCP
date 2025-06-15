using UnityEngine;
using UnityEditor;
using UnityEditor.Compilation;
using System.Threading.Tasks;

namespace io.github.hatayama.uMCP
{
    public class CompileEditorWindow : EditorWindow
    {
        private CompileChecker compileChecker;
        private CompileLogDisplay logDisplay;
        private Vector2 scrollPosition;
        private bool forceRecompile = false;

        // SessionStateのキー
        private const string LOG_TEXT_KEY = "CompileEditorWindow.LogText";
        private const string HAS_DATA_KEY = "CompileEditorWindow.HasData";

        [MenuItem("Tools/CompileWindow/Window")]
        public static void ShowWindow()
        {
            CompileEditorWindow window = GetWindow<CompileEditorWindow>();
            window.titleContent = new GUIContent("コンパイルツール");
            window.Show();
        }

        private void OnEnable()
        {
            // インスタンスがまだない場合のみ作成
            if (compileChecker == null || logDisplay == null)
            {
                compileChecker = new CompileChecker();
                logDisplay = new CompileLogDisplay();

                // SessionStateから永続化されたログを復元
                bool hasPersistedData = SessionState.GetBool(HAS_DATA_KEY, false);
                if (hasPersistedData)
                {
                    string persistentLogText = SessionState.GetString(LOG_TEXT_KEY, "");
                    logDisplay.RestoreFromText(persistentLogText);
                }

                // イベント購読
                compileChecker.OnCompileStarted += logDisplay.AppendStartMessage;
                compileChecker.OnAssemblyCompiled += logDisplay.AppendAssemblyMessage;
                compileChecker.OnCompileCompleted += OnCompileCompleted;
            }
            else
            {
                // 既存インスタンスがある場合、イベント購読が切れている可能性があるので再購読
                if (!compileChecker.IsCompiling)
                {
                    compileChecker.OnCompileStarted += logDisplay.AppendStartMessage;
                    compileChecker.OnAssemblyCompiled += logDisplay.AppendAssemblyMessage;
                    compileChecker.OnCompileCompleted += OnCompileCompleted;
                }
            }
        }

        private void OnDisable()
        {
            DisposeInstances();

            // OnDisable時のみnullにして完全にクリーンアップ
            compileChecker = null;
            logDisplay = null;
        }

        private void DisposeInstances()
        {
            if (compileChecker != null)
            {
                // イベント購読解除
                if (logDisplay != null)
                {
                    compileChecker.OnCompileStarted -= logDisplay.AppendStartMessage;
                    compileChecker.OnAssemblyCompiled -= logDisplay.AppendAssemblyMessage;
                }
                compileChecker.OnCompileCompleted -= OnCompileCompleted;

                compileChecker.Dispose();
                // OnDisable時のみnullにする
            }

            if (logDisplay != null)
            {
                logDisplay.Dispose();
                // OnDisable時のみnullにする
            }
        }

        private void OnGUI()
        {
            if (compileChecker == null || logDisplay == null) return;

            GUILayout.Label("Unity コンパイルツール", EditorStyles.boldLabel);

            // 強制再コンパイルオプション
            forceRecompile = EditorGUILayout.Toggle("強制再コンパイル", forceRecompile);
            GUILayout.Space(5);

            EditorGUI.BeginDisabledGroup(compileChecker.IsCompiling);
            string buttonText = compileChecker.IsCompiling ? "コンパイル中..." :
                               (forceRecompile ? "強制コンパイル実行" : "コンパイル実行");
            if (GUILayout.Button(buttonText, GUILayout.Height(30)))
            {
                // async/awaitを使ったコンパイル実行
                _ = ExecuteCompileAsync();
            }
            EditorGUI.EndDisabledGroup();

            GUILayout.Space(5);

            // クリアボタン
            if (GUILayout.Button("ログクリア", GUILayout.Height(25)))
            {
                ClearLog();
            }

            GUILayout.Space(10);

            GUILayout.Label("コンパイル結果:", EditorStyles.boldLabel);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));
            EditorGUILayout.TextArea(logDisplay.LogText, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            DrawMessageDetails();
        }

        private async Task ExecuteCompileAsync()
        {
            CompileResult result = await compileChecker.TryCompileAsync(forceRecompile);
            
            // 結果をログに出力（デバッグ用）
            UnityEngine.Debug.Log($"コンパイル完了: 成功={result.Success}, エラー数={result.error.Length}, 警告数={result.warning.Length}");
        }

        private void OnCompileCompleted(CompileResult result)
        {
            logDisplay.AppendCompletionMessage(result);

            // ログをSessionStateに永続化
            SessionState.SetString(LOG_TEXT_KEY, logDisplay.LogText);
            SessionState.SetBool(HAS_DATA_KEY, true);

            Repaint();
        }

        private void DrawMessageDetails()
        {
            var messages = compileChecker.CompileMessages;
            if (messages.Count > 0)
            {
                GUILayout.Space(10);
                GUILayout.Label($"エラー・警告詳細 ({messages.Count}件):", EditorStyles.boldLabel);

                foreach (CompilerMessage message in messages)
                {
                    GUIStyle style = message.type == CompilerMessageType.Error ?
                        EditorStyles.helpBox : EditorStyles.helpBox;

                    string prefix = message.type == CompilerMessageType.Error ? "[エラー]" : "[警告]";
                    EditorGUILayout.LabelField($"{prefix} {message.message}", style);
                }
            }
        }

        private void ClearLog()
        {
            logDisplay.Clear();
            compileChecker.ClearMessages();

            // SessionStateもクリア
            SessionState.EraseString(LOG_TEXT_KEY);
            SessionState.EraseBool(HAS_DATA_KEY);

            Repaint();
        }
    }
}