using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// GetMenuItems tools for MCP C# SDK format
    /// Related classes:
    /// - GetMenuItemsCommand: Legacy command version (will be deprecated)
    /// - GetMenuItemsSchema: Legacy schema (will be deprecated)
    /// - GetMenuItemsResponse: Legacy response (will be deprecated)
    /// - MenuItemInfo: MenuItem information structure
    /// - MenuItemDiscoveryService: MenuItem discovery service
    /// </summary>
    [McpServerToolType]
    public static class GetMenuItemsTools
    {
        /// <summary>
        /// Retrieve Unity MenuItems with detailed metadata for programmatic execution
        /// </summary>
        [McpServerTool(Name = "get-menu-items")]
        [Description("Retrieve Unity MenuItems with detailed metadata for programmatic execution. Unlike Unity Search menu provider, this provides implementation details (method names, assemblies, execution compatibility) needed for automation and debugging.")]
        public static Task<GetMenuItemsToolResult> GetMenuItems(
            [Description("Text to filter MenuItem paths (empty for all items)")] 
            string filterText = "",
            [Description("Type of filter to apply (contains, exact, startswith)")] 
            MenuItemFilterType filterType = MenuItemFilterType.Contains,
            [Description("Include validation functions in the results")] 
            bool includeValidation = false,
            [Description("Maximum number of menu items to retrieve")] 
            int maxCount = 200,
            CancellationToken cancellationToken = default)
        {
            // Discover all MenuItems using the service
            List<MenuItemInfo> allMenuItems = MenuItemDiscoveryService.DiscoverAllMenuItems().ToList();
            
            // Apply filtering
            List<MenuItemInfo> filteredMenuItems = ApplyFiltering(allMenuItems, filterText, filterType, includeValidation);
            
            // Apply count limit
            if (filteredMenuItems.Count > maxCount)
            {
                filteredMenuItems = filteredMenuItems.Take(maxCount).ToList();
            }
            
            // Create response
            GetMenuItemsToolResult result = new GetMenuItemsToolResult(
                menuItems: filteredMenuItems,
                totalCount: allMenuItems.Count,
                filteredCount: filteredMenuItems.Count,
                appliedFilter: filterText,
                appliedFilterType: filterType.ToString()
            );
            
            return Task.FromResult(result);
        }

        /// <summary>
        /// Applies filtering to the MenuItem list based on the specified criteria
        /// </summary>
        private static List<MenuItemInfo> ApplyFiltering(
            List<MenuItemInfo> allMenuItems, 
            string filterText, 
            MenuItemFilterType filterType,
            bool includeValidation)
        {
            List<MenuItemInfo> filtered = allMenuItems;
            
            // Filter by validation function inclusion
            if (!includeValidation)
            {
                // Filter out validation functions - need to check if item is extended type
                filtered = filtered.Where(item => 
                {
                    MenuItemInfoExtended extended = item as MenuItemInfoExtended;
                    return extended == null || !extended.IsValidateFunction;
                }).ToList();
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
        private static List<MenuItemInfo> ApplyTextFilter(
            List<MenuItemInfo> menuItems, 
            string filterText, 
            MenuItemFilterType filterType)
        {
            return filterType switch
            {
                MenuItemFilterType.Exact => menuItems.Where(item => 
                    string.Equals(item.Path, filterText, System.StringComparison.OrdinalIgnoreCase)).ToList(),
                    
                MenuItemFilterType.StartsWith => menuItems.Where(item => 
                    item.Path.StartsWith(filterText, System.StringComparison.OrdinalIgnoreCase)).ToList(),
                    
                MenuItemFilterType.Contains => menuItems.Where(item => 
                    item.Path.IndexOf(filterText, System.StringComparison.OrdinalIgnoreCase) >= 0).ToList(),
                    
                _ => menuItems
            };
        }
        
        /// <summary>
        /// Result for get-menu-items tool
        /// Compatible with legacy GetMenuItemsResponse structure
        /// </summary>
        public class GetMenuItemsToolResult : BaseCommandResponse
        {
            [Description("List of discovered MenuItems matching the filter criteria")]
            public List<MenuItemInfo> MenuItems { get; set; }

            [Description("Total number of MenuItems discovered before filtering")]
            public int TotalCount { get; set; }

            [Description("Number of MenuItems returned after filtering")]
            public int FilteredCount { get; set; }

            [Description("The filter text that was applied")]
            public string AppliedFilter { get; set; }

            [Description("The filter type that was applied")]
            public string AppliedFilterType { get; set; }

            public GetMenuItemsToolResult(
                List<MenuItemInfo> menuItems,
                int totalCount,
                int filteredCount,
                string appliedFilter,
                string appliedFilterType)
            {
                MenuItems = menuItems ?? new List<MenuItemInfo>();
                TotalCount = totalCount;
                FilteredCount = filteredCount;
                AppliedFilter = appliedFilter ?? string.Empty;
                AppliedFilterType = appliedFilterType ?? string.Empty;
            }
        }
    }
}