using UnityEngine;
using UnityEditor;
using UnityEditor.Compilation;
using System.Threading.Tasks;

namespace io.github.hatayama.uMCP
{
    public class CompileEditorWindow : EditorWindow
    {
        private CompileController _compileController;
        private CompileLogDisplay logDisplay;
        private Vector2 scrollPosition;
        private bool forceRecompile = false;

        // Note: Compile window data is now managed via McpSessionManager

        [MenuItem("uMCP/Windows/Compile Tool")]
        public static void ShowWindow()
        {
            CompileEditorWindow window = GetWindow<CompileEditorWindow>();
            window.titleContent = new GUIContent("Compile Tool");
            window.Show();
        }

        private void OnEnable()
        {
            // Create instances only if they don't exist yet
            if (_compileController == null || logDisplay == null)
            {
                _compileController = new CompileController();
                logDisplay = new CompileLogDisplay();

                // Restore persisted logs from McpSessionManager
                bool hasPersistedData = McpSessionManager.instance.CompileWindowHasData;
                if (hasPersistedData)
                {
                    string persistentLogText = McpSessionManager.instance.CompileWindowLogText;
                    logDisplay.RestoreFromText(persistentLogText);
                }

                // Subscribe to events
                _compileController.OnCompileStarted += logDisplay.AppendStartMessage;
                _compileController.OnAssemblyCompiled += logDisplay.AppendAssemblyMessage;
                _compileController.OnCompileCompleted += OnCompileCompleted;
            }
            else
            {
                // If an instance already exists, re-subscribe as the event subscription might have been lost
                if (!_compileController.IsCompiling)
                {
                    _compileController.OnCompileStarted += logDisplay.AppendStartMessage;
                    _compileController.OnAssemblyCompiled += logDisplay.AppendAssemblyMessage;
                    _compileController.OnCompileCompleted += OnCompileCompleted;
                }
            }
        }

        private void OnDisable()
        {
            DisposeInstances();

            // Set to null only on OnDisable for a complete cleanup
            _compileController = null;
            logDisplay = null;
        }

        private void DisposeInstances()
        {
            if (_compileController != null)
            {
                // Unsubscribe from events
                if (logDisplay != null)
                {
                    _compileController.OnCompileStarted -= logDisplay.AppendStartMessage;
                    _compileController.OnAssemblyCompiled -= logDisplay.AppendAssemblyMessage;
                }
                _compileController.OnCompileCompleted -= OnCompileCompleted;

                _compileController.Dispose();
                // Set to null only on OnDisable
            }

            if (logDisplay != null)
            {
                logDisplay.Dispose();
                // Set to null only on OnDisable
            }
        }

        private void OnGUI()
        {
            if (_compileController == null || logDisplay == null) return;

            GUILayout.Label("Unity Compile Tool", EditorStyles.boldLabel);

            // Force recompile option
            forceRecompile = EditorGUILayout.Toggle("Force Recompile", forceRecompile);
            GUILayout.Space(5);

            EditorGUI.BeginDisabledGroup(_compileController.IsCompiling);
            string buttonText = _compileController.IsCompiling ? "Compiling..." :
                               (forceRecompile ? "Run Force Compile" : "Run Compile");
            if (GUILayout.Button(buttonText, GUILayout.Height(30)))
            {
                // Execute compilation using async/await
                _ = ExecuteCompileAsync();
            }
            EditorGUI.EndDisabledGroup();

            GUILayout.Space(5);

            // Clear button
            if (GUILayout.Button("Clear Log", GUILayout.Height(25)))
            {
                ClearLog();
            }

            GUILayout.Space(10);

            GUILayout.Label("Compilation Result:", EditorStyles.boldLabel);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));
            EditorGUILayout.TextArea(logDisplay.LogText, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            DrawMessageDetails();
        }

        private async Task ExecuteCompileAsync()
        {
            CompileResult result = await _compileController.TryCompileAsync(forceRecompile);
            
            // Output result to log (for debugging)
            UnityEngine.Debug.Log($"Compilation finished: Success={result.Success}, Errors={result.error.Length}, Warnings={result.warning.Length}");
        }

        private void OnCompileCompleted(CompileResult result)
        {
            logDisplay.AppendCompletionMessage(result);

            // Persist log to McpSessionManager
            McpSessionManager.instance.CompileWindowLogText = logDisplay.LogText;
            McpSessionManager.instance.CompileWindowHasData = true;

            Repaint();
        }

        private void DrawMessageDetails()
        {
            var messages = _compileController.CompileMessages;
            if (messages.Count > 0)
            {
                GUILayout.Space(10);
                GUILayout.Label($"Error/Warning Details ({messages.Count} items):", EditorStyles.boldLabel);

                foreach (CompilerMessage message in messages)
                {
                    GUIStyle style = message.type == CompilerMessageType.Error ?
                        EditorStyles.helpBox : EditorStyles.helpBox;

                    string prefix = message.type == CompilerMessageType.Error ? "[Error]" : "[Warning]";
                    EditorGUILayout.LabelField($"{prefix} {message.message}", style);
                }
            }
        }

        private void ClearLog()
        {
            logDisplay.Clear();
            _compileController.ClearMessages();

            // Also clear McpSessionManager data
            McpSessionManager.instance.ClearCompileWindowData();

            Repaint();
        }
    }
}