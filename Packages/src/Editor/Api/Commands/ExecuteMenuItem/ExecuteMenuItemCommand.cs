using System;
using System.Collections.Generic;
using System.Linq;
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

        protected override async Task<ExecuteMenuItemResponse> ExecuteAsync(ExecuteMenuItemSchema parameters)
        {
            // Type-safe parameter access
            string menuItemPath = parameters.MenuItemPath;
            bool useReflectionFallback = parameters.UseReflectionFallback;
            
            // Switch to the main thread for Unity API access
            await MainThreadSwitcher.SwitchToMainThread();
            
            ExecuteMenuItemResponse response = new ExecuteMenuItemResponse
            {
                MenuItemPath = menuItemPath
            };
            
            // Validate parameters
            if (string.IsNullOrEmpty(menuItemPath))
            {
                response.Success = false;
                response.ErrorMessage = "MenuItemPath cannot be empty";
                response.MenuItemFound = false;
                return response;
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
            
            return response;
        }

        /// <summary>
        /// Attempts to execute MenuItem using EditorApplication.ExecuteMenuItem
        /// </summary>
        private bool TryExecuteViaEditorApplication(string menuItemPath, ExecuteMenuItemResponse response)
        {
            try
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
                else
                {
                    response.ExecutionMethod = "EditorApplication";
                    response.ErrorMessage = "EditorApplication.ExecuteMenuItem returned false";
                    response.Details = "MenuItem may not exist or may not be executable via EditorApplication";
                    return false;
                }
            }
            catch (Exception ex)
            {
                response.ExecutionMethod = "EditorApplication";
                response.ErrorMessage = $"Exception in EditorApplication.ExecuteMenuItem: {ex.Message}";
                response.Details = ex.ToString();
                return false;
            }
        }

        /// <summary>
        /// Attempts to execute MenuItem using reflection to find and invoke the method directly
        /// </summary>
        private bool TryExecuteViaReflection(string menuItemPath, ExecuteMenuItemResponse response)
        {
            try
            {
                // Find the MenuItem method using reflection
                MenuItemInfo menuItemInfo = FindMenuItemByPath(menuItemPath);
                
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
            catch (Exception ex)
            {
                response.ExecutionMethod = "Reflection";
                response.ErrorMessage = $"Exception in reflection execution: {ex.Message}";
                response.Details = ex.ToString();
                return false;
            }
        }

        /// <summary>
        /// Finds a MenuItem by path using reflection
        /// </summary>
        private MenuItemInfo FindMenuItemByPath(string menuItemPath)
        {
            try
            {
                // Get all assemblies in the current domain
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                
                foreach (Assembly assembly in assemblies)
                {
                    try
                    {
                        // Get all types in the assembly
                        Type[] types = assembly.GetTypes();
                        
                        foreach (Type type in types)
                        {
                            // Get all static methods in the type
                            MethodInfo[] methods = type.GetMethods(
                                BindingFlags.Static | 
                                BindingFlags.Public | 
                                BindingFlags.NonPublic
                            );
                            
                            foreach (MethodInfo method in methods)
                            {
                                // Check if method has MenuItem attribute
                                MenuItem menuItemAttribute = method.GetCustomAttribute<MenuItem>();
                                if (menuItemAttribute != null && 
                                    string.Equals(menuItemAttribute.menuItem, menuItemPath, StringComparison.OrdinalIgnoreCase))
                                {
                                    return new MenuItemInfo(
                                        menuItemAttribute.menuItem,
                                        method,
                                        menuItemAttribute.priority,
                                        menuItemAttribute.validate
                                    );
                                }
                            }
                        }
                    }
                    catch (ReflectionTypeLoadException)
                    {
                        // Skip assemblies that can't be loaded
                        continue;
                    }
                    catch (Exception)
                    {
                        // Skip assemblies that cause other reflection issues
                        continue;
                    }
                }
            }
            catch (Exception)
            {
                // Return null if any error occurs
            }
            
            return null;
        }
    }
}