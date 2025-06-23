using System;
using System.IO;
using UnityEngine;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Unity MCP Editor settings data.
    /// </summary>
    [Serializable]
    public record McpEditorSettingsData
    {
        public int customPort = 7400;
        public bool autoStartServer = false;
        public bool showDeveloperTools = false;
        public bool enableMcpLogs = false;
        public bool enableCommunicationLogs = false;
        public bool enableDevelopmentMode = false;
        public string lastUsedConfigPath = "";
    }

    /// <summary>
    /// Management class for Unity MCP Editor settings.
    /// Saves as a JSON file in the UserSettings folder.
    /// </summary>
    public static class McpEditorSettings
    {
        private static string SettingsFilePath => Path.Combine(McpConstants.USER_SETTINGS_FOLDER, McpConstants.SETTINGS_FILE_NAME);
        
        private static McpEditorSettingsData _cachedSettings;
        
        /// <summary>
        /// Gets the settings data.
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
        /// Saves the settings data.
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
        /// Gets the custom port number.
        /// </summary>
        public static int GetCustomPort()
        {
            return GetSettings().customPort;
        }
        
        /// <summary>
        /// Saves the custom port number.
        /// </summary>
        public static void SetCustomPort(int port)
        {
            McpEditorSettingsData settings = GetSettings();
            McpEditorSettingsData updatedSettings = settings with { customPort = port };
            SaveSettings(updatedSettings);
        }
        
        /// <summary>
        /// Gets the auto-start setting.
        /// </summary>
        public static bool GetAutoStartServer()
        {
            return GetSettings().autoStartServer;
        }
        
        /// <summary>
        /// Saves the auto-start setting.
        /// </summary>
        public static void SetAutoStartServer(bool autoStart)
        {
            McpEditorSettingsData settings = GetSettings();
            McpEditorSettingsData updatedSettings = settings with { autoStartServer = autoStart };
            SaveSettings(updatedSettings);
        }
        
        /// <summary>
        /// Gets the Developer Tools display setting.
        /// </summary>
        public static bool GetShowDeveloperTools()
        {
            return GetSettings().showDeveloperTools;
        }
        
        /// <summary>
        /// Saves the Developer Tools display setting.
        /// </summary>
        public static void SetShowDeveloperTools(bool show)
        {
            McpEditorSettingsData settings = GetSettings();
            McpEditorSettingsData updatedSettings = settings with { showDeveloperTools = show };
            SaveSettings(updatedSettings);
        }
        
        /// <summary>
        /// Gets the MCP log enabled flag.
        /// </summary>
        public static bool GetEnableMcpLogs()
        {
            return GetSettings().enableMcpLogs;
        }
        
        /// <summary>
        /// Sets the MCP log enabled flag.
        /// </summary>
        public static void SetEnableMcpLogs(bool enableMcpLogs)
        {
            McpEditorSettingsData settings = GetSettings();
            McpEditorSettingsData newSettings = settings with { enableMcpLogs = enableMcpLogs };
            SaveSettings(newSettings);
            
            // Synchronize McpLogger settings as well.
            McpLogger.EnableDebugLog = enableMcpLogs;
        }
        
        /// <summary>
        /// Gets the communication logs enabled flag.
        /// </summary>
        public static bool GetEnableCommunicationLogs()
        {
            return GetSettings().enableCommunicationLogs;
        }
        
        /// <summary>
        /// Sets the communication logs enabled flag.
        /// </summary>
        public static void SetEnableCommunicationLogs(bool enableCommunicationLogs)
        {
            McpEditorSettingsData settings = GetSettings();
            McpEditorSettingsData newSettings = settings with { enableCommunicationLogs = enableCommunicationLogs };
            SaveSettings(newSettings);
        }
        
        /// <summary>
        /// Gets the development mode enabled flag.
        /// </summary>
        public static bool GetEnableDevelopmentMode()
        {
            return GetSettings().enableDevelopmentMode;
        }
        
        /// <summary>
        /// Sets the development mode enabled flag.
        /// </summary>
        public static void SetEnableDevelopmentMode(bool enableDevelopmentMode)
        {
            McpEditorSettingsData settings = GetSettings();
            McpEditorSettingsData newSettings = settings with { enableDevelopmentMode = enableDevelopmentMode };
            SaveSettings(newSettings);
        }
        
        /// <summary>
        /// Loads the settings file.
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
                    // Create default settings.
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
        /// Gets the path to the settings file.
        /// </summary>
        public static string GetSettingsFilePath()
        {
            return Path.GetFullPath(SettingsFilePath);
        }
    }
} 