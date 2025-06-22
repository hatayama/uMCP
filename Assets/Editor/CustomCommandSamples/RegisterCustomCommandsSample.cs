using System.Linq;
using UnityEngine;
using UnityEditor;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Sample implementation of custom commands
    /// Reference example for users to add their own commands
    /// </summary>\
    [InitializeOnLoad]
    public static class RegisterCustomCommandsSample
    {
        static RegisterCustomCommandsSample()
        {
            RegisterSampleCommands();
        }
        /// <summary>
        /// Register custom commands
        /// </summary>
        [MenuItem("uMCP/Custom Commands/Register Sample Commands")]
        public static void RegisterSampleCommands()
        {
            CustomCommandManager.RegisterCustomCommand(new HelloWorldCommand());
            CustomCommandManager.RegisterCustomCommand(new GetProjectInfoCommand());
            CustomCommandManager.RegisterCustomCommand(new AdvancedCustomCommand());
            CustomCommandManager.RegisterCustomCommand(new GetVersionCommand());
            
            Debug.Log("Sample custom commands registered successfully!");
            Debug.Log("Available commands: " + string.Join(", ", CustomCommandManager.GetRegisteredCustomCommands().Select(c => c.Name)));
        }

        /// <summary>
        /// Unregister custom commands
        /// </summary>
        [MenuItem("uMCP/Custom Commands/Unregister Sample Commands")]
        public static void UnregisterSampleCommands()
        {
            CustomCommandManager.UnregisterCustomCommand("helloworld");
            CustomCommandManager.UnregisterCustomCommand("getprojectinfo");
            CustomCommandManager.UnregisterCustomCommand("advancedcustom");
            CustomCommandManager.UnregisterCustomCommand("getversion");
            
            Debug.Log("Sample custom commands unregistered successfully!");
        }

        /// <summary>
        /// Display list of currently registered commands
        /// </summary>
        [MenuItem("uMCP/Custom Commands/Show Registered Commands")]
        public static void ShowRegisteredCommands()
        {
            CommandInfo[] commands = CustomCommandManager.GetRegisteredCustomCommands();
            Debug.Log($"Currently registered commands ({commands.Length}):");
            
            for (int i = 0; i < commands.Length; i++)
            {
                Debug.Log($"{i + 1}. {commands[i].Name} - {commands[i].Description}");
            }
            
            // Additional detailed debug information
            Debug.Log("=== Debug Info ===");
            Debug.Log(CustomCommandManager.GetDebugInfo());
        }
    }
} 