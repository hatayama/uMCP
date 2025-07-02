using UnityEditor;
using UnityEngine;
using System.Linq;

namespace io.github.hatayama.uMCP.Editor
{
    /// <summary>
    /// Unity Editor menu items for toggling UMCP_DEBUG scripting define symbol
    /// Provides easy access to enable/disable debug features in uMCP
    /// Related classes:
    /// - McpEditorWindow: Uses UMCP_DEBUG to show/hide developer tools
    /// - McpLogger: Debug logging behavior controlled by this symbol
    /// </summary>
    public static class UmcpDebugToggle
    {
        private const string DEBUG_SYMBOL = "UMCP_DEBUG";
        private const string MENU_PATH_ENABLE = "Window/uMCP/Enable Debug Mode";
        private const string MENU_PATH_DISABLE = "Window/uMCP/Disable Debug Mode";

        /// <summary>
        /// Check if UMCP_DEBUG symbol is currently defined
        /// </summary>
        private static bool IsDebugModeEnabled()
        {
            BuildTargetGroup targetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
            return defines.Split(';').Contains(DEBUG_SYMBOL);
        }

        /// <summary>
        /// Enable UMCP_DEBUG scripting define symbol
        /// </summary>
        [MenuItem(MENU_PATH_ENABLE)]
        public static void EnableDebugMode()
        {
            if (IsDebugModeEnabled())
            {
                Debug.Log("[uMCP] Debug mode is already enabled");
                return;
            }

            BuildTargetGroup targetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
            
            if (string.IsNullOrEmpty(defines))
            {
                defines = DEBUG_SYMBOL;
            }
            else
            {
                defines += ";" + DEBUG_SYMBOL;
            }
            
            PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, defines);
            Debug.Log("[uMCP] Debug mode enabled. Unity will recompile scripts.");
        }

        /// <summary>
        /// Disable UMCP_DEBUG scripting define symbol
        /// </summary>
        [MenuItem(MENU_PATH_DISABLE)]
        public static void DisableDebugMode()
        {
            if (!IsDebugModeEnabled())
            {
                Debug.Log("[uMCP] Debug mode is already disabled");
                return;
            }

            BuildTargetGroup targetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
            
            string[] defineArray = defines.Split(';');
            defineArray = defineArray.Where(d => d != DEBUG_SYMBOL).ToArray();
            defines = string.Join(";", defineArray);
            
            PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, defines);
            Debug.Log("[uMCP] Debug mode disabled. Unity will recompile scripts.");
        }

        /// <summary>
        /// Validate menu item - only show Enable when debug mode is disabled
        /// </summary>
        [MenuItem(MENU_PATH_ENABLE, true)]
        public static bool ValidateEnableDebugMode()
        {
            return !IsDebugModeEnabled();
        }

        /// <summary>
        /// Validate menu item - only show Disable when debug mode is enabled
        /// </summary>
        [MenuItem(MENU_PATH_DISABLE, true)]
        public static bool ValidateDisableDebugMode()
        {
            return IsDebugModeEnabled();
        }
    }
}