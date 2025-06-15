using System;
using System.IO;
using UnityEngine;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Unity MCP Editor設定データ
    /// </summary>
    [Serializable]
    public record McpEditorSettingsData
    {
        public int customPort = 7400;
        public bool autoStartServer = false;
        public bool showDeveloperTools = false;
        public bool enableMcpLogs = false;
        public string lastUsedConfigPath = "";
    }

    /// <summary>
    /// Unity MCP Editor設定の管理クラス
    /// UserSettingsフォルダにJSONファイルとして保存する
    /// </summary>
    public static class McpEditorSettings
    {
        private const string SETTINGS_FILE_NAME = "UnityMcpSettings.json";
        private static string SettingsFilePath => Path.Combine("UserSettings", SETTINGS_FILE_NAME);
        
        private static McpEditorSettingsData _cachedSettings;
        
        /// <summary>
        /// 設定データを取得する
        /// </summary>
        public static McpEditorSettingsData GetSettings()
        {
            if (_cachedSettings == null)
            {
                LoadSettings();
            }
            return _cachedSettings;
        }
        
        /// <summary>
        /// 設定データを保存する
        /// </summary>
        public static void SaveSettings(McpEditorSettingsData settings)
        {
            try
            {
                string json = JsonUtility.ToJson(settings, true);
                File.WriteAllText(SettingsFilePath, json);
                _cachedSettings = settings;
                
                McpLogger.LogInfo($"MCP Editor settings saved to: {SettingsFilePath}");
            }
            catch (Exception ex)
            {
                McpLogger.LogError($"Failed to save MCP Editor settings: {ex.Message}");
            }
        }
        
        /// <summary>
        /// カスタムポート番号を取得する
        /// </summary>
        public static int GetCustomPort()
        {
            return GetSettings().customPort;
        }
        
        /// <summary>
        /// カスタムポート番号を保存する
        /// </summary>
        public static void SetCustomPort(int port)
        {
            McpEditorSettingsData settings = GetSettings();
            McpEditorSettingsData updatedSettings = settings with { customPort = port };
            SaveSettings(updatedSettings);
        }
        
        /// <summary>
        /// 自動起動設定を取得する
        /// </summary>
        public static bool GetAutoStartServer()
        {
            return GetSettings().autoStartServer;
        }
        
        /// <summary>
        /// 自動起動設定を保存する
        /// </summary>
        public static void SetAutoStartServer(bool autoStart)
        {
            McpEditorSettingsData settings = GetSettings();
            McpEditorSettingsData updatedSettings = settings with { autoStartServer = autoStart };
            SaveSettings(updatedSettings);
        }
        
        /// <summary>
        /// Developer Tools表示設定を取得する
        /// </summary>
        public static bool GetShowDeveloperTools()
        {
            return GetSettings().showDeveloperTools;
        }
        
        /// <summary>
        /// Developer Tools表示設定を保存する
        /// </summary>
        public static void SetShowDeveloperTools(bool show)
        {
            McpEditorSettingsData settings = GetSettings();
            McpEditorSettingsData updatedSettings = settings with { showDeveloperTools = show };
            SaveSettings(updatedSettings);
        }
        
        /// <summary>
        /// MCPログ有効フラグを取得する
        /// </summary>
        public static bool GetEnableMcpLogs()
        {
            return GetSettings().enableMcpLogs;
        }
        
        /// <summary>
        /// MCPログ有効フラグを設定する
        /// </summary>
        public static void SetEnableMcpLogs(bool enableMcpLogs)
        {
            McpEditorSettingsData settings = GetSettings();
            McpEditorSettingsData newSettings = settings with { enableMcpLogs = enableMcpLogs };
            SaveSettings(newSettings);
            
            // McpLoggerの設定も同期
            McpLogger.EnableDebugLog = enableMcpLogs;
        }
        
        /// <summary>
        /// 設定ファイルを読み込む
        /// </summary>
        private static void LoadSettings()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    _cachedSettings = JsonUtility.FromJson<McpEditorSettingsData>(json);
                    McpLogger.LogInfo($"MCP Editor settings loaded from: {SettingsFilePath}");
                }
                else
                {
                    // デフォルト設定を作成
                    _cachedSettings = new McpEditorSettingsData();
                    SaveSettings(_cachedSettings);
                    McpLogger.LogInfo("Created default MCP Editor settings");
                }
            }
            catch (Exception ex)
            {
                McpLogger.LogError($"Failed to load MCP Editor settings: {ex.Message}");
                _cachedSettings = new McpEditorSettingsData();
            }
        }
        
        /// <summary>
        /// 設定ファイルのパスを取得する
        /// </summary>
        public static string GetSettingsFilePath()
        {
            return Path.GetFullPath(SettingsFilePath);
        }
    }
} 