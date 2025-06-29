using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using uMCP.Editor.Api.Commands.GetMenuItems;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Service for discovering Unity MenuItems using reflection
    /// Provides common functionality for MenuItem discovery and lookup
    /// Related classes:
    /// - MenuItemInfo: Data structure for MenuItem information
    /// - GetMenuItemsCommand: Uses this service for MenuItem discovery
    /// </summary>
    public static class MenuItemDiscoveryService
    {
        // Cache for discovered menu items to improve performance
        private static List<MenuItemInfo> cachedMenuItems = null;
        private static DateTime lastCacheTime = DateTime.MinValue;
        private static readonly TimeSpan CacheValidityDuration = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Discovers all MenuItem methods in loaded assemblies with performance optimizations
        /// </summary>
        public static List<MenuItemInfo> DiscoverAllMenuItems()
        {
            // Check cache validity
            if (cachedMenuItems != null && DateTime.UtcNow - lastCacheTime < CacheValidityDuration)
            {
                return new List<MenuItemInfo>(cachedMenuItems);
            }

            List<MenuItemInfo> menuItems = new List<MenuItemInfo>();
            Assembly[] assemblies = GetRelevantAssemblies();
            
            foreach (Assembly assembly in assemblies)
            {
                AddMenuItemsFromAssembly(assembly, menuItems);
            }
            
            List<MenuItemInfo> sortedMenuItems = menuItems.OrderBy(item => item.Path).ToList();
            
            // Update cache
            cachedMenuItems = new List<MenuItemInfo>(sortedMenuItems);
            lastCacheTime = DateTime.UtcNow;
            
            return sortedMenuItems;
        }

        /// <summary>
        /// Finds a specific MenuItem by path
        /// </summary>
        public static MenuItemInfo FindMenuItemByPath(string menuItemPath)
        {
            // First try to find in cached results
            if (cachedMenuItems != null)
            {
                MenuItemInfo cached = cachedMenuItems.FirstOrDefault(item => 
                    string.Equals(item.Path, menuItemPath, StringComparison.OrdinalIgnoreCase));
                if (cached != null)
                {
                    return cached;
                }
            }

            // If not found in cache, search in relevant assemblies
            Assembly[] assemblies = GetRelevantAssemblies();
            
            foreach (Assembly assembly in assemblies)
            {
                MenuItemInfo menuItem = FindMenuItemInAssembly(assembly, menuItemPath);
                if (menuItem != null)
                {
                    return menuItem;
                }
            }
            
            return null;
        }

        /// <summary>
        /// Get assemblies that are likely to contain MenuItems (Unity Editor assemblies)
        /// </summary>
        private static Assembly[] GetRelevantAssemblies()
        {
            Assembly[] allAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            List<Assembly> relevantAssemblies = new List<Assembly>();

            foreach (Assembly assembly in allAssemblies)
            {
                if (IsRelevantAssembly(assembly))
                {
                    relevantAssemblies.Add(assembly);
                }
            }

            return relevantAssemblies.ToArray();
        }

        /// <summary>
        /// Check if assembly is likely to contain MenuItems
        /// </summary>
        private static bool IsRelevantAssembly(Assembly assembly)
        {
            if (assembly == null) return false;

            string assemblyName = assembly.GetName().Name;
            if (string.IsNullOrEmpty(assemblyName)) return false;

            // Include Unity Editor assemblies and project assemblies
            return assemblyName.StartsWith("UnityEditor") ||
                   assemblyName.StartsWith("Unity.") ||
                   assemblyName.Contains("Editor") ||
                   assemblyName.StartsWith("Assembly-CSharp-Editor") ||
                   assemblyName.StartsWith("uMCP") ||
                   (!assemblyName.StartsWith("System") && 
                    !assemblyName.StartsWith("Microsoft") && 
                    !assemblyName.StartsWith("mscorlib") &&
                    !assemblyName.StartsWith("netstandard") &&
                    !assemblyName.StartsWith("Newtonsoft"));
        }

        private static void AddMenuItemsFromAssembly(Assembly assembly, List<MenuItemInfo> menuItems)
        {
            Type[] types = GetTypesFromAssembly(assembly);
            if (types == null) return;
            
            foreach (Type type in types)
            {
                AddMenuItemsFromType(type, menuItems);
            }
        }

        private static MenuItemInfo FindMenuItemInAssembly(Assembly assembly, string menuItemPath)
        {
            Type[] types = GetTypesFromAssembly(assembly);
            if (types == null) return null;
            
            foreach (Type type in types)
            {
                MenuItemInfo menuItem = FindMenuItemInType(type, menuItemPath);
                if (menuItem != null)
                {
                    return menuItem;
                }
            }
            
            return null;
        }

        private static Type[] GetTypesFromAssembly(Assembly assembly)
        {
            if (assembly == null) return null;

            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                // Return only successfully loaded types
                return ex.Types.Where(t => t != null).ToArray();
            }
            catch (Exception)
            {
                // Skip assemblies that can't be loaded
                return null;
            }
        }

        private static void AddMenuItemsFromType(Type type, List<MenuItemInfo> menuItems)
        {
            if (type == null) return;

            try
            {
                MethodInfo[] methods = type.GetMethods(
                    BindingFlags.Static | 
                    BindingFlags.Public | 
                    BindingFlags.NonPublic
                );
                
                foreach (MethodInfo method in methods)
                {
                    MenuItemInfo menuItem = CreateMenuItemInfo(method);
                    if (menuItem != null)
                    {
                        menuItems.Add(menuItem);
                    }
                }
            }
            catch (Exception)
            {
                // Skip types that can't be processed
            }
        }

        private static MenuItemInfo FindMenuItemInType(Type type, string menuItemPath)
        {
            if (type == null) return null;

            try
            {
                MethodInfo[] methods = type.GetMethods(
                    BindingFlags.Static | 
                    BindingFlags.Public | 
                    BindingFlags.NonPublic
                );
                
                foreach (MethodInfo method in methods)
                {
                    MenuItemInfo menuItem = CreateMenuItemInfo(method);
                    if (menuItem != null && 
                        string.Equals(menuItem.Path, menuItemPath, StringComparison.OrdinalIgnoreCase))
                    {
                        return menuItem;
                    }
                }
            }
            catch (Exception)
            {
                // Skip types that can't be processed
            }

            return null;
        }

        private static MenuItemInfo CreateMenuItemInfo(MethodInfo method)
        {
            if (method == null) return null;

            try
            {
                MenuItem menuItemAttribute = method.GetCustomAttribute<MenuItem>();
                if (menuItemAttribute == null) return null;
                
                return new MenuItemInfo(
                    menuItemAttribute.menuItem,
                    method,
                    menuItemAttribute.priority,
                    menuItemAttribute.validate
                );
            }
            catch (Exception)
            {
                // Skip methods that can't be processed
                return null;
            }
        }

        /// <summary>
        /// Clear the cache to force fresh discovery on next call
        /// </summary>
        public static void ClearCache()
        {
            cachedMenuItems = null;
            lastCacheTime = DateTime.MinValue;
        }
    }
} 