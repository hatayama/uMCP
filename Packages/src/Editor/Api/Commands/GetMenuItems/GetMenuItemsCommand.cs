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
    /// GetMenuItems command handler - Discovers Unity MenuItems with filtering
    /// Retrieves MenuItem information from all loaded assemblies
    /// </summary>
    [McpTool]
    public class GetMenuItemsCommand : AbstractUnityCommand<GetMenuItemsSchema, GetMenuItemsResponse>
    {
        public override string CommandName => "getmenuitems";
        public override string Description => "Retrieve Unity MenuItems with filtering options";

        protected override async Task<GetMenuItemsResponse> ExecuteAsync(GetMenuItemsSchema parameters)
        {
            // Type-safe parameter access
            string filterText = parameters.FilterText;
            MenuItemFilterType filterType = parameters.FilterType;
            bool includeValidation = parameters.IncludeValidation;
            int maxCount = parameters.MaxCount;
            
            // Switch to the main thread for Unity API access
            await MainThreadSwitcher.SwitchToMainThread();
            
            // Discover all MenuItems using reflection
            List<MenuItemInfo> allMenuItems = DiscoverMenuItems();
            
            // Apply filtering
            List<MenuItemInfo> filteredMenuItems = ApplyFiltering(allMenuItems, filterText, filterType, includeValidation);
            
            // Apply count limit
            if (filteredMenuItems.Count > maxCount)
            {
                filteredMenuItems = filteredMenuItems.Take(maxCount).ToList();
            }
            
            // Create response
            GetMenuItemsResponse response = new GetMenuItemsResponse
            {
                MenuItems = filteredMenuItems,
                TotalCount = allMenuItems.Count,
                FilteredCount = filteredMenuItems.Count,
                AppliedFilter = filterText,
                AppliedFilterType = filterType.ToString()
            };
            
            return response;
        }

        /// <summary>
        /// Discovers all MenuItem methods in loaded assemblies using reflection
        /// </summary>
        private List<MenuItemInfo> DiscoverMenuItems()
        {
            List<MenuItemInfo> menuItems = new List<MenuItemInfo>();
            
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
                                if (menuItemAttribute != null)
                                {
                                    MenuItemInfo menuItemInfo = new MenuItemInfo(
                                        menuItemAttribute.menuItem,
                                        method,
                                        menuItemAttribute.priority,
                                        menuItemAttribute.validate
                                    );
                                    
                                    menuItems.Add(menuItemInfo);
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
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Error discovering MenuItems: {ex.Message}");
            }
            
            // Sort by path for consistent ordering
            return menuItems.OrderBy(item => item.Path).ToList();
        }

        /// <summary>
        /// Applies filtering to the MenuItem list based on the specified criteria
        /// </summary>
        private List<MenuItemInfo> ApplyFiltering(
            List<MenuItemInfo> allMenuItems, 
            string filterText, 
            MenuItemFilterType filterType,
            bool includeValidation)
        {
            List<MenuItemInfo> filtered = allMenuItems;
            
            // Filter by validation function inclusion
            if (!includeValidation)
            {
                filtered = filtered.Where(item => !item.IsValidateFunction).ToList();
            }
            
            // Apply text filtering if specified
            if (!string.IsNullOrEmpty(filterText))
            {
                filtered = ApplyTextFilter(filtered, filterText, filterType);
            }
            
            return filtered;
        }

        /// <summary>
        /// Applies text filtering based on the specified filter type
        /// </summary>
        private List<MenuItemInfo> ApplyTextFilter(
            List<MenuItemInfo> menuItems, 
            string filterText, 
            MenuItemFilterType filterType)
        {
            return filterType switch
            {
                MenuItemFilterType.exact => menuItems.Where(item => 
                    string.Equals(item.Path, filterText, StringComparison.OrdinalIgnoreCase)).ToList(),
                    
                MenuItemFilterType.startswith => menuItems.Where(item => 
                    item.Path.StartsWith(filterText, StringComparison.OrdinalIgnoreCase)).ToList(),
                    
                MenuItemFilterType.contains => menuItems.Where(item => 
                    item.Path.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0).ToList(),
                    
                _ => menuItems
            };
        }
    }
}