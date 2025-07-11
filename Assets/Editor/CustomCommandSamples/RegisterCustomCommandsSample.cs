using System.Linq;
using UnityEngine;
using UnityEditor;

namespace io.github.hatayama.uLoopMCP
{
    /// <summary>
    /// Sample implementation of custom tools
    /// Reference example for users to add their own tools
    /// </summary>\
    // [InitializeOnLoad]
    public static class RegisterCustomToolsSample
    {
        static RegisterCustomToolsSample()
        {
            RegisterSampleTools();
        }
        /// <summary>
        /// Register custom tools
        /// </summary>
        [MenuItem("uLoopMCP/Tools/Custom Tools/Register Sample Tools")]
        public static void RegisterSampleTools()
        {
            CustomToolManager.RegisterCustomTool(new HelloWorldTool());
            CustomToolManager.RegisterCustomTool(new GetProjectInfoTool());
            CustomToolManager.RegisterCustomTool(new GetVersionTool());
            
            Debug.Log("Sample custom tools registered successfully!");
            Debug.Log("Available tools: " + string.Join(", ", CustomToolManager.GetRegisteredCustomTools().Select(c => c.Name)));
            
            // Manual notification is automatically called by RegisterCustomTool,
            // but we can also call it explicitly if needed
            CustomToolManager.NotifyToolChanges();
        }

        /// <summary>
        /// Unregister custom tools
        /// </summary>
        [MenuItem("uLoopMCP/Tools/Custom Tools/Unregister Sample Tools")]
        public static void UnregisterSampleCommands()
        {
            CustomToolManager.UnregisterCustomTool("helloworld");
            CustomToolManager.UnregisterCustomTool("getprojectinfo");
            CustomToolManager.UnregisterCustomTool("advancedcustom");
            CustomToolManager.UnregisterCustomTool("getversion");
            
            Debug.Log("Sample custom tools unregistered successfully!");
            
            // Manual notification is automatically called by UnregisterCustomTool,
            // but we can also call it explicitly if needed
            CustomToolManager.NotifyToolChanges();
        }

        /// <summary>
        /// Display list of currently registered tools
        /// </summary>
        [MenuItem("uLoopMCP/Tools/Custom Tools/Show Registered Tools")]
        public static void ShowRegisteredCommands()
        {
            ToolInfo[] tools = CustomToolManager.GetRegisteredCustomTools();
            Debug.Log($"Currently registered tools ({tools.Length}):");
            
            for (int i = 0; i < tools.Length; i++)
            {
                Debug.Log($"{i + 1}. {tools[i].Name} - {tools[i].Description}");
            }
            
            // Additional detailed debug information
            Debug.Log("=== Debug Info ===");
            Debug.Log(CustomToolManager.GetDebugInfo());
        }
    }
} 