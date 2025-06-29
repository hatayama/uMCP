using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using uMCP.Editor.Api.Commands.GetMenuItems;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// ExecuteMenuItem command handler - Executes Unity MenuItems by path
    /// Supports both EditorApplication.ExecuteMenuItem and reflection-based execution
    /// </summary>
    [McpTool]
    public class ExecuteMenuItemCommand : AbstractUnityCommand<ExecuteMenuItemSchema, ExecuteMenuItemResponse>
    {
        public override string CommandName => "executemenuitem";
        public override string Description => "Execute Unity MenuItem by path";

        protected override Task<ExecuteMenuItemResponse> ExecuteAsync(ExecuteMenuItemSchema parameters)
        {
            // Type-safe parameter access
            string menuItemPath = parameters.MenuItemPath;
            bool useReflectionFallback = parameters.UseReflectionFallback;
            
            // Validate parameters
            if (string.IsNullOrEmpty(menuItemPath))
            {
                ExecuteMenuItemResponse errorResponse = new ExecuteMenuItemResponse(
                    menuItemPath: menuItemPath ?? string.Empty,
                    success: false,
                    executionMethod: string.Empty,
                    errorMessage: "MenuItemPath cannot be empty",
                    details: string.Empty,
                    menuItemFound: false
                );
                return Task.FromResult(errorResponse);
            }
            
            // First, try using EditorApplication.ExecuteMenuItem
            ExecuteMenuItemResponse response = TryExecuteViaEditorApplication(menuItemPath);
            
            // If that fails and reflection fallback is enabled, try reflection
            if (!response.Success && useReflectionFallback)
            {
                response = TryExecuteViaReflection(menuItemPath);
            }
            
            // If still not successful, create final error response
            if (!response.Success && string.IsNullOrEmpty(response.ErrorMessage))
            {
                response = new ExecuteMenuItemResponse(
                    menuItemPath: menuItemPath,
                    success: false,
                    executionMethod: response.ExecutionMethod,
                    errorMessage: $"Failed to execute MenuItem: {menuItemPath}",
                    details: response.Details,
                    menuItemFound: response.MenuItemFound
                );
            }
            
            return Task.FromResult(response);
        }

        /// <summary>
        /// Attempts to execute MenuItem using EditorApplication.ExecuteMenuItem
        /// </summary>
        private ExecuteMenuItemResponse TryExecuteViaEditorApplication(string menuItemPath)
        {
            bool success = EditorApplication.ExecuteMenuItem(menuItemPath);
            
            if (success)
            {
                return new ExecuteMenuItemResponse(
                    menuItemPath: menuItemPath,
                    success: true,
                    executionMethod: "EditorApplication",
                    errorMessage: string.Empty,
                    details: "MenuItem executed successfully via EditorApplication.ExecuteMenuItem",
                    menuItemFound: true
                );
            }
            
            return new ExecuteMenuItemResponse(
                menuItemPath: menuItemPath,
                success: false,
                executionMethod: "EditorApplication",
                errorMessage: "EditorApplication.ExecuteMenuItem returned false",
                details: "MenuItem may not exist or may not be executable via EditorApplication",
                menuItemFound: false
            );
        }

        /// <summary>
        /// Attempts to execute MenuItem using reflection to find and invoke the method directly
        /// </summary>
        private ExecuteMenuItemResponse TryExecuteViaReflection(string menuItemPath)
        {
            // Find the MenuItem method using the service
            MenuItemInfo menuItemInfo = MenuItemDiscoveryService.FindMenuItemByPath(menuItemPath);
            
            if (menuItemInfo == null)
            {
                return new ExecuteMenuItemResponse(
                    menuItemPath: menuItemPath,
                    success: false,
                    executionMethod: "Reflection",
                    errorMessage: "MenuItem not found via reflection",
                    details: $"Could not find MenuItem with path: {menuItemPath}",
                    menuItemFound: false
                );
            }
            
            // Don't execute validation functions
            if (menuItemInfo.IsValidateFunction)
            {
                return new ExecuteMenuItemResponse(
                    menuItemPath: menuItemPath,
                    success: false,
                    executionMethod: "Reflection",
                    errorMessage: "Cannot execute validation function",
                    details: "The specified path is a validation function, not an executable MenuItem",
                    menuItemFound: true
                );
            }
            
            // Get the method and invoke it
            Type type = Type.GetType(menuItemInfo.TypeName);
            if (type == null)
            {
                return new ExecuteMenuItemResponse(
                    menuItemPath: menuItemPath,
                    success: false,
                    executionMethod: "Reflection",
                    errorMessage: "Could not load type",
                    details: $"Could not load type: {menuItemInfo.TypeName}",
                    menuItemFound: true
                );
            }
            
            MethodInfo method = type.GetMethod(menuItemInfo.MethodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (method == null)
            {
                return new ExecuteMenuItemResponse(
                    menuItemPath: menuItemPath,
                    success: false,
                    executionMethod: "Reflection",
                    errorMessage: "Could not find method",
                    details: $"Could not find method: {menuItemInfo.MethodName}",
                    menuItemFound: true
                );
            }
            
            // Invoke the method
            method.Invoke(null, null);
            
            return new ExecuteMenuItemResponse(
                menuItemPath: menuItemPath,
                success: true,
                executionMethod: "Reflection",
                errorMessage: string.Empty,
                details: $"MenuItem executed successfully via reflection ({menuItemInfo.TypeName}.{menuItemInfo.MethodName})",
                menuItemFound: true
            );
        }
    }
}