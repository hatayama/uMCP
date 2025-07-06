using System;
using System.ComponentModel;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using uMCP.Editor.Api.Commands.GetMenuItems;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// ExecuteMenuItem tools for MCP C# SDK format
    /// Related classes:
    /// - ExecuteMenuItemCommand: Legacy command version (will be deprecated)
    /// - ExecuteMenuItemSchema: Legacy schema (will be deprecated)
    /// - ExecuteMenuItemResponse: Legacy response (will be deprecated)
    /// </summary>
    [McpServerToolType]
    public static class ExecuteMenuItemTools
    {
        /// <summary>
        /// Execute Unity MenuItem by path
        /// </summary>
        [McpServerTool(Name = "execute-menu-item")]
        [Description("Execute Unity MenuItem by path")]
        public static Task<ExecuteMenuItemToolResult> ExecuteMenuItem(
            [Description("The menu item path to execute (e.g., \"GameObject/Create Empty\")")] 
            string menuItemPath = "",
            [Description("Whether to use reflection as fallback if EditorApplication.ExecuteMenuItem fails")] 
            bool useReflectionFallback = true,
            CancellationToken cancellationToken = default)
        {
            ExecuteMenuItemToolResult response = new ExecuteMenuItemToolResult
            {
                MenuItemPath = menuItemPath
            };
            
            // Validate parameters
            if (string.IsNullOrEmpty(menuItemPath))
            {
                response.Success = false;
                response.ErrorMessage = "MenuItemPath cannot be empty";
                response.MenuItemFound = false;
                return Task.FromResult(response);
            }
            
            // First, try using EditorApplication.ExecuteMenuItem
            bool success = TryExecuteViaEditorApplication(menuItemPath, response);
            
            // If that fails and reflection fallback is enabled, try reflection
            if (!success && useReflectionFallback)
            {
                success = TryExecuteViaReflection(menuItemPath, response);
            }
            
            if (!success)
            {
                response.Success = false;
                if (string.IsNullOrEmpty(response.ErrorMessage))
                {
                    response.ErrorMessage = $"Failed to execute MenuItem: {menuItemPath}";
                }
            }
            
            return Task.FromResult(response);
        }

        /// <summary>
        /// Attempts to execute MenuItem using EditorApplication.ExecuteMenuItem
        /// </summary>
        private static bool TryExecuteViaEditorApplication(string menuItemPath, ExecuteMenuItemToolResult response)
        {
            bool success = EditorApplication.ExecuteMenuItem(menuItemPath);
            
            if (success)
            {
                response.Success = true;
                response.ExecutionMethod = "EditorApplication";
                response.MenuItemFound = true;
                response.Details = "MenuItem executed successfully via EditorApplication.ExecuteMenuItem";
                return true;
            }
            
            response.ExecutionMethod = "EditorApplication";
            response.ErrorMessage = "EditorApplication.ExecuteMenuItem returned false";
            response.Details = "MenuItem may not exist or may not be executable via EditorApplication";
            return false;
        }

        /// <summary>
        /// Attempts to execute MenuItem using reflection to find and invoke the method directly
        /// </summary>
        private static bool TryExecuteViaReflection(string menuItemPath, ExecuteMenuItemToolResult response)
        {
            // Find the MenuItem method using the service
            MenuItemInfo menuItemInfo = MenuItemDiscoveryService.FindMenuItemByPath(menuItemPath);
            
            if (menuItemInfo == null)
            {
                response.ExecutionMethod = "Reflection";
                response.ErrorMessage = "MenuItem not found via reflection";
                response.MenuItemFound = false;
                response.Details = $"Could not find MenuItem with path: {menuItemPath}";
                return false;
            }
            
            // Don't execute validation functions
            if (menuItemInfo.IsValidateFunction)
            {
                response.ExecutionMethod = "Reflection";
                response.ErrorMessage = "Cannot execute validation function";
                response.MenuItemFound = true;
                response.Details = "The specified path is a validation function, not an executable MenuItem";
                return false;
            }
            
            // Get the method and invoke it
            Type type = Type.GetType(menuItemInfo.TypeName);
            if (type == null)
            {
                response.ExecutionMethod = "Reflection";
                response.ErrorMessage = "Could not load type";
                response.MenuItemFound = true;
                response.Details = $"Could not load type: {menuItemInfo.TypeName}";
                return false;
            }
            
            MethodInfo method = type.GetMethod(menuItemInfo.MethodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (method == null)
            {
                response.ExecutionMethod = "Reflection";
                response.ErrorMessage = "Could not find method";
                response.MenuItemFound = true;
                response.Details = $"Could not find method: {menuItemInfo.MethodName}";
                return false;
            }
            
            // Invoke the method
            method.Invoke(null, null);
            
            response.Success = true;
            response.ExecutionMethod = "Reflection";
            response.MenuItemFound = true;
            response.Details = $"MenuItem executed successfully via reflection ({menuItemInfo.TypeName}.{menuItemInfo.MethodName})";
            return true;
        }
        
        /// <summary>
        /// Result for ExecuteMenuItem tool
        /// Compatible with legacy ExecuteMenuItemResponse structure
        /// </summary>
        public class ExecuteMenuItemToolResult : BaseCommandResponse
        {
            [Description("The menu item path that was executed")]
            public string MenuItemPath { get; set; }

            [Description("Whether the execution was successful")]
            public bool Success { get; set; }

            [Description("The execution method used (EditorApplication or Reflection)")]
            public string ExecutionMethod { get; set; }

            [Description("Error message if execution failed")]
            public string ErrorMessage { get; set; }

            [Description("Additional information about the execution")]
            public string Details { get; set; }

            [Description("Whether the menu item was found in the system")]
            public bool MenuItemFound { get; set; }

            public ExecuteMenuItemToolResult()
            {
                MenuItemPath = string.Empty;
                ExecutionMethod = string.Empty;
                ErrorMessage = string.Empty;
                Details = string.Empty;
            }
        }
    }
}