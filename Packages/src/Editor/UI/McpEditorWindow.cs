using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Unity MCP Server制御用のEditor Window
    /// サーバーの状態表示・起動・停止を行う
    /// </summary>
    public class McpEditorWindow : EditorWindow
    {
        // SessionState用の定数
        private const string SESSION_KEY_SELECTED_EDITOR_TYPE = "uMCP.SelectedEditorType";
        private const string SESSION_KEY_COMMUNICATION_LOG_HEIGHT = "uMCP.CommunicationLogHeight";
        
        // UI状態
        private int customPort = 7400;
        private bool autoStartServer = false;
        private bool showDeveloperTools = false;
        private bool showCommunicationLogs = false;
        private bool enableMcpLogs = false;
        private Vector2 communicationLogScrollPosition;
        private float communicationLogHeight = 300f; // リサイズ可能な通信ログエリアの高さ
        private bool isResizingCommunicationLog = false; // リサイズ中かどうか
        
        // エディタ選択用のUI状態
        private McpEditorType selectedEditorType = McpEditorType.Cursor;
        
        // ログエントリごとのスクロール位置を管理
        private Dictionary<string, Vector2> requestScrollPositions = new Dictionary<string, Vector2>();
        private Dictionary<string, Vector2> responseScrollPositions = new Dictionary<string, Vector2>();
        
        // ウィンドウ全体のスクロール位置
        private Vector2 mainScrollPosition;
        
        // 設定サービス
        private readonly McpConfigRepository _repository = new(McpEditorType.Cursor);
        private McpConfigService _cursorConfigService;
        private McpConfigService _claudeCodeConfigService;
        
        [MenuItem("Window/uMCP")]
        public static void ShowWindow()
        {
            McpEditorWindow window = GetWindow<McpEditorWindow>("uMCP");
            window.minSize = new Vector2(400, 200);
            window.Show();
        }

        private void OnEnable()
        {
            // 設定サービスを初期化
            McpConfigRepository cursorRepository = new(McpEditorType.Cursor);
            McpConfigRepository claudeCodeRepository = new(McpEditorType.ClaudeCode);
            _cursorConfigService = new McpConfigService(cursorRepository, McpEditorType.Cursor);
            _claudeCodeConfigService = new McpConfigService(claudeCodeRepository, McpEditorType.ClaudeCode);
            
            // 保存された設定を読み込み
            McpEditorSettingsData settings = McpEditorSettings.GetSettings();
            customPort = settings.customPort;
            autoStartServer = settings.autoStartServer;
            showDeveloperTools = settings.showDeveloperTools;
            enableMcpLogs = settings.enableMcpLogs;
            
            // エディタ選択状態を復元
            selectedEditorType = (McpEditorType)SessionState.GetInt(SESSION_KEY_SELECTED_EDITOR_TYPE, (int)McpEditorType.Cursor);
            
            // McpLoggerの設定を同期
            McpLogger.EnableDebugLog = enableMcpLogs;
            
            // 通信ログエリアの高さを復元（SessionStateから）
            communicationLogHeight = SessionState.GetFloat(SESSION_KEY_COMMUNICATION_LOG_HEIGHT, 300f);
            
            // ログ更新イベントを購読
            McpCommunicationLogger.OnLogUpdated += Repaint;
            
            // コンパイル後かどうかチェック
            bool isAfterCompile = SessionState.GetBool("uMCP.AfterCompile", false);
            
            // コンパイル後またはAuto Start Serverが有効な場合、サーバーを起動
            if ((isAfterCompile || autoStartServer) && !McpServerController.IsServerRunning)
            {
                // コンパイル後の場合、フラグをクリア
                if (isAfterCompile)
                {
                    SessionState.EraseBool("uMCP.AfterCompile");
                    McpLogger.LogInfo("McpEditorWindow detected post-compile state. Starting server immediately...");
                    
                    // 保存されたポート番号を使用
                    int savedPort = SessionState.GetInt("uMCP.ServerPort", customPort);
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
            // ログ更新イベントの購読を解除
            McpCommunicationLogger.OnLogUpdated -= Repaint;
            
            // エディタ選択状態を保存
            SessionState.SetInt(SESSION_KEY_SELECTED_EDITOR_TYPE, (int)selectedEditorType);
            
            // サーバーの管理はMcpServerControllerに完全に任せる
            // ウィンドウを閉じてもサーバーは停止しない（グローバルリソースとして扱う）
            McpLogger.LogInfo($"McpEditorWindow.OnDisable: Window closing, server will keep running if active");
        }

        private void OnGUI()
        {
            // サーバーポートとUI設定を同期
            SyncPortSettings();
            
            // ウィンドウ全体をスクロール可能にする
            mainScrollPosition = EditorGUILayout.BeginScrollView(mainScrollPosition);
            
            DrawServerStatus();
            DrawServerControls();
            DrawEditorConfigSection();
            DrawCommunicationLogs();
            DrawDeveloperTools();
            
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// サーバーポートとUI設定を同期する
        /// </summary>
        private void SyncPortSettings()
        {
            // サーバーが動作中で、UIのポート設定と実際のサーバーポートが異なる場合は同期
            if (McpServerController.IsServerRunning)
            {
                int actualServerPort = McpServerController.ServerPort;
                if (customPort != actualServerPort)
                {
                    customPort = actualServerPort;
                    // 設定ファイルも更新
                    McpEditorSettings.SetCustomPort(customPort);
                }
            }
        }

        /// <summary>
        /// サーバー状態セクションを描画
        /// </summary>
        private void DrawServerStatus()
        {
            EditorGUILayout.BeginVertical("box");
            
            // 状態表示
            (bool isRunning, int port, bool wasRestored) = McpServerController.GetServerStatus();
            string status = isRunning ? "Running" : "Stopped";
            Color statusColor = isRunning ? Color.green : Color.red;
            
            GUIStyle statusStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = statusColor },
                fontStyle = FontStyle.Bold
            };
            
            // Server Status、Status、Port を全て横並びで表示
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
        /// サーバー制御セクションを描画
        /// </summary>
        private void DrawServerControls()
        {
            EditorGUILayout.BeginVertical("box");
            
            // 自動起動チェックボックス
            EditorGUI.BeginChangeCheck();
            bool newAutoStart = EditorGUILayout.Toggle("Auto Start Server", autoStartServer);
            if (EditorGUI.EndChangeCheck())
            {
                autoStartServer = newAutoStart;
                McpEditorSettings.SetAutoStartServer(autoStartServer);
            }
            
            EditorGUILayout.Space();
            
            // ポート設定
            bool isRunning = McpServerController.IsServerRunning;
            EditorGUI.BeginDisabledGroup(isRunning);
            
            EditorGUI.BeginChangeCheck();
            int newPort = EditorGUILayout.IntField("Port:", customPort);
            if (EditorGUI.EndChangeCheck())
            {
                customPort = newPort;
                // ポート番号が変更されたら即座に保存
                McpEditorSettings.SetCustomPort(customPort);
            }
            
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.Space();
            
            // 起動・停止ボタン
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
        /// サーバーを開始する（ユーザー操作用）
        /// </summary>
        private void StartServer()
        {
            if (ValidatePortAndStartServer())
            {
                Repaint();
            }
        }

        /// <summary>
        /// サーバーを開始する（内部処理用）
        /// </summary>
        private void StartServerInternal()
        {
            ValidatePortAndStartServer();
        }

        /// <summary>
        /// ポートを検証してサーバーを開始する
        /// </summary>
        /// <returns>成功した場合true</returns>
        private bool ValidatePortAndStartServer()
        {
            if (customPort < 1024 || customPort > 65535)
            {
                EditorUtility.DisplayDialog("Port Error", "Port must be between 1024 and 65535", "OK");
                return false;
            }

            // 既に同じポートで自分のサーバーが動いているかチェック
            if (McpServerController.IsServerRunning && McpServerController.ServerPort == customPort)
            {
                McpLogger.LogInfo($"MCP Server is already running on port {customPort}");
                return true; // 既に動いているので成功扱い
            }

            // 他のプロセスが使用中かどうかをチェック
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
                // ポート使用中エラーの場合
                EditorUtility.DisplayDialog("Server Start Error", ex.Message, "OK");
                return false;
            }
            catch (Exception ex)
            {
                // その他のエラー
                EditorUtility.DisplayDialog("Server Start Error", 
                    $"Failed to start server: {ex.Message}", 
                    "OK");
                return false;
            }
        }

        /// <summary>
        /// サーバーを停止する
        /// </summary>
        private void StopServer()
        {
            McpServerController.StopServer();
            Repaint();
        }

        /// <summary>
        /// エディタ設定セクションを描画
        /// </summary>
        private void DrawEditorConfigSection()
        {
            EditorGUILayout.LabelField("LLM Tool Settings", EditorStyles.boldLabel);
            
            // エディタ選択プルダウン
            EditorGUI.BeginChangeCheck();
            selectedEditorType = (McpEditorType)EditorGUILayout.EnumPopup("Target:", selectedEditorType);
            if (EditorGUI.EndChangeCheck())
            {
                // 選択が変更されたらセッションに保存
                SessionState.SetInt(SESSION_KEY_SELECTED_EDITOR_TYPE, (int)selectedEditorType);
            }
            
            bool isServerRunning = McpServerController.IsServerRunning;
            int currentServerPort = McpServerController.ServerPort;
            
            // 選択されたエディタの設定のみ表示
            McpConfigService configService = GetConfigService(selectedEditorType);
            string editorName = GetEditorDisplayName(selectedEditorType);
            
            DrawConfigurationSection(editorName, configService, isServerRunning, currentServerPort);
        }

        /// <summary>
        /// エディタタイプから対応する設定サービスを取得
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
        /// エディタタイプから表示名を取得
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
        /// 個別の設定セクションを描画
        /// </summary>
        private void DrawConfigurationSection(string editorName, McpConfigService configService, bool isServerRunning, int currentServerPort)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"{editorName}設定", EditorStyles.boldLabel);
            
            bool isConfigured = configService.IsConfigured();
            
            if (isConfigured)
            {
                // ポート番号の不整合チェック
                if (isServerRunning && currentServerPort != customPort)
                {
                    EditorGUILayout.HelpBox($"注意: {editorName}設定のポート番号と現在のサーバーポート({currentServerPort})が一致していない可能性があります。", MessageType.Warning);
                    
                    if (GUILayout.Button($"{editorName}設定をポート{currentServerPort}に更新"))
                    {
                        configService.AutoConfigure(currentServerPort);
                        Repaint();
                    }
                    
                    EditorGUILayout.Space();
                }
                
                EditorGUILayout.HelpBox($"{editorName}設定は既に構成されています。", MessageType.Info);
                
                string configPath = UnityMcpPathResolver.GetConfigPath(selectedEditorType);
                if (GUILayout.Button("設定ファイルを開く"))
                {
                    EditorUtility.RevealInFinder(configPath);
                }
            }
            else
            {
                EditorGUILayout.HelpBox($"{editorName}設定が見つかりません。自動設定を実行してください。", MessageType.Warning);
            }
            
            EditorGUILayout.Space();
            
            string buttonText = isServerRunning ? $"{editorName}設定を自動構成 (ポート{currentServerPort})" : $"{editorName}設定を自動構成";
            if (GUILayout.Button(buttonText))
            {
                int portToUse = isServerRunning ? currentServerPort : customPort;
                configService.AutoConfigure(portToUse);
                Repaint();
            }
            
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 開発者ツールセクションを描画
        /// </summary>
        private void DrawDeveloperTools()
        {
            EditorGUILayout.BeginVertical("box");
            
            // トグルヘッダー
            EditorGUI.BeginChangeCheck();
            showDeveloperTools = EditorGUILayout.Foldout(showDeveloperTools, "Developer Tools", true);
            if (EditorGUI.EndChangeCheck())
            {
                McpEditorSettings.SetShowDeveloperTools(showDeveloperTools);
            }
            
            // Developer Toolsの内容（展開時のみ表示）
            if (showDeveloperTools)
            {
                EditorGUILayout.Space();
                
                // ログ制御トグル
                EditorGUI.BeginChangeCheck();
                bool newEnableMcpLogs = EditorGUILayout.Toggle("Enable MCP Logs", enableMcpLogs);
                if (EditorGUI.EndChangeCheck())
                {
                    enableMcpLogs = newEnableMcpLogs;
                    McpEditorSettings.SetEnableMcpLogs(enableMcpLogs);
                }
                
                EditorGUILayout.Space();
                
                // デバッグ情報表示ボタン
                if (GUILayout.Button("Show Debug Info"))
                {
                    string debugInfo = McpServerController.GetDetailedServerStatus();
                    Debug.Log($"MCP Server Debug Info:\n{debugInfo}");
                }
                
                EditorGUILayout.Space();
                
                // TypeScriptビルドボタン
                if (GUILayout.Button("Rebuild TypeScript Server", GUILayout.Height(30)))
                {
                    RebuildTypeScriptServer();
                }
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        /// <summary>
        /// 通信ログセクションを描画
        /// </summary>
        private void DrawCommunicationLogs()
        {
            EditorGUILayout.BeginVertical("box");
            
            // ヘッダーとクリアボタン
            EditorGUILayout.BeginHorizontal();
            showCommunicationLogs = EditorGUILayout.Foldout(showCommunicationLogs, "通信ログ", true);
            
            if (showCommunicationLogs)
            {
                if (GUILayout.Button("クリア", GUILayout.Width(60)))
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
                    EditorGUILayout.HelpBox("通信ログはまだありません", MessageType.Info);
                }
                else
                {
                    // スクロールビュー（リサイズ可能）
                    communicationLogScrollPosition = EditorGUILayout.BeginScrollView(
                        communicationLogScrollPosition, 
                        GUILayout.Height(communicationLogHeight)
                    );
                    
                    for (int i = logs.Count - 1; i >= 0; i--) // 最新から表示
                    {
                        McpCommunicationLogEntry log = logs[i];
                        DrawLogEntry(log);
                        EditorGUILayout.Space(5);
                    }
                    
                    EditorGUILayout.EndScrollView();
                    
                    // リサイズハンドル
                    DrawCommunicationLogResizeHandle();
                }
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }
        
        /// <summary>
        /// 個別のログエントリーを描画
        /// </summary>
        private void DrawLogEntry(McpCommunicationLogEntry log)
        {
            EditorGUILayout.BeginVertical("box");
            
            // ヘッダー（クリック可能なトグル）
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
            
            // 内容（展開時のみ）
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
        /// JSONを整形する
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
                return json; // パースできない場合はそのまま返す
            }
        }

        /// <summary>
        /// JSON内容を適切な高さで描画する
        /// </summary>
        private void DrawJsonContent(string jsonContent)
        {
            GUIStyle textAreaStyle = new GUIStyle(EditorStyles.textArea);
            textAreaStyle.wordWrap = true;
            
            // 内容の行数を計算して適切な高さを設定
            string[] lines = jsonContent.Split('\n');
            int lineCount = lines.Length;
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float contentHeight = Mathf.Max(80f, Mathf.Min(lineCount * lineHeight + 20f, 200f)); // 最小80px、最大200px
            
            // 通常のSelectableLabelで表示（スクロールは外側の通信ログ全体で対応）
            EditorGUILayout.SelectableLabel(jsonContent, textAreaStyle, GUILayout.Height(contentHeight));
        }

        /// <summary>
        /// JSON内容をスクロール可能な領域で描画する
        /// </summary>
        private void DrawJsonContentWithScroll(string jsonContent, string scrollKey, Dictionary<string, Vector2> scrollPositions)
        {
            GUIStyle textAreaStyle = new GUIStyle(EditorStyles.textArea);
            textAreaStyle.wordWrap = true;
            
            // 内容の行数を計算
            string[] lines = jsonContent.Split('\n');
            int lineCount = lines.Length;
            float lineHeight = EditorGUIUtility.singleLineHeight;
            
            // 固定高さ（150px）でスクロール可能な領域を作成
            float fixedHeight = 150f;
            
            // コンテンツの実際の高さを計算（十分な余白を確保）
            float contentHeight = lineCount * lineHeight + 50f; // 上下のパディング + 余白を多めに
            
            // スクロール位置を取得（存在しない場合は初期化）
            if (!scrollPositions.ContainsKey(scrollKey))
            {
                scrollPositions[scrollKey] = Vector2.zero;
            }
            
            // スクロールビューを作成
            scrollPositions[scrollKey] = EditorGUILayout.BeginScrollView(
                scrollPositions[scrollKey], 
                GUILayout.Height(fixedHeight)
            );
            
            // JSON内容を表示（十分な高さを確保）
            EditorGUILayout.SelectableLabel(jsonContent, textAreaStyle, GUILayout.Height(contentHeight));
            
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 通信ログエリアのリサイズハンドルを描画
        /// </summary>
        private void DrawCommunicationLogResizeHandle()
        {
            // リサイズハンドルの領域を取得（高さを8pxに増やして見やすく）
            Rect handleRect = GUILayoutUtility.GetRect(0, 8, GUILayout.ExpandWidth(true));
            
            // マウスカーソルをリサイズカーソルに変更
            EditorGUIUtility.AddCursorRect(handleRect, MouseCursor.ResizeVertical);
            
            // リサイズハンドルの見た目を描画（より見やすく）
            if (Event.current.type == EventType.Repaint)
            {
                Color originalColor = GUI.color;
                
                // 背景を少し暗く
                GUI.color = new Color(0.3f, 0.3f, 0.3f, 0.3f);
                GUI.DrawTexture(handleRect, EditorGUIUtility.whiteTexture);
                
                // 中央に3つの点を描画してハンドルらしく
                GUI.color = new Color(0.6f, 0.6f, 0.6f, 0.8f);
                float centerX = handleRect.x + handleRect.width * 0.5f;
                float centerY = handleRect.y + handleRect.height * 0.5f;
                
                // 3つの点を描画
                for (int i = -1; i <= 1; i++)
                {
                    Rect dotRect = new Rect(centerX + i * 4 - 1, centerY - 1, 2, 2);
                    GUI.DrawTexture(dotRect, EditorGUIUtility.whiteTexture);
                }
                
                GUI.color = originalColor;
            }
            
            // マウスイベントの処理
            if (Event.current.type == EventType.MouseDown && handleRect.Contains(Event.current.mousePosition))
            {
                isResizingCommunicationLog = true;
                Event.current.Use();
            }
            
            if (isResizingCommunicationLog)
            {
                if (Event.current.type == EventType.MouseDrag)
                {
                    // ドラッグ量に応じて高さを調整
                    communicationLogHeight += Event.current.delta.y;
                    
                    // 最小・最大高さを制限
                    communicationLogHeight = Mathf.Clamp(communicationLogHeight, 100f, 800f);
                    
                    // SessionStateに保存
                    SessionState.SetFloat(SESSION_KEY_COMMUNICATION_LOG_HEIGHT, communicationLogHeight);
                    
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
        /// TypeScriptサーバーをリビルドする
        /// </summary>
        private void RebuildTypeScriptServer()
        {
            // 必要な時にTypeScriptBuilderを作成
            TypeScriptBuilder builder = new TypeScriptBuilder();
            
            builder.BuildTypeScriptServer((success, output, error) => {
                if (success)
                {
                    // サーバーが動いている場合は自動再起動
                    if (McpServerController.IsServerRunning)
                    {
                        Debug.Log("Restarting server with new build...");
                        McpServerController.StopServer();
                        
                        // 少し待ってから再起動（サーバーの完全停止を待つ）
                        EditorApplication.delayCall += () => {
                            McpServerController.StartServer(customPort);
                            Debug.Log("Server restarted with updated TypeScript build!");
                            Repaint(); // UIを更新
                        };
                    }
                    else
                    {
                        Debug.Log("Server is not running. Start the server manually to use the updated build.");
                    }
                }
                // エラー処理はTypeScriptBuilder内で行われる
            });
        }
    }
} 