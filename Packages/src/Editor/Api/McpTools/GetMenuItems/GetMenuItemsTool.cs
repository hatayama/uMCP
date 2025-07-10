using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using uMCP.Editor.Api.Commands.GetMenuItems;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// GetMenuItems tool handler - Discovers Unity MenuItems with filtering
    /// Retrieves MenuItem information from all loaded assemblies
    /// </summary>
    [McpTool(Description = "Retrieve Unity MenuItems with detailed metadata for programmatic execution. Unlike Unity Search menu provider, this provides implementation details (method names, assemblies, execution compatibility) needed for automation and debugging.")]
    public class GetMenuItemsTool : AbstractUnityTool<GetMenuItemsSchema, GetMenuItemsResponse>
    {
        public override string ToolName => "get-menu-items";

        protected override Task<GetMenuItemsResponse> ExecuteAsync(GetMenuItemsSchema parameters, CancellationToken cancellationToken)
        {
            // Check for cancellation before starting
            cancellationToken.ThrowIfCancellationRequested();
            
            // Type-safe parameter access
            string filterText = parameters.FilterText;
            MenuItemFilterType filterType = parameters.FilterType;
            bool includeValidation = parameters.IncludeValidation;
            int maxCount = parameters.MaxCount;
            
            // Discover all MenuItems using the service
            List<MenuItemInfo> allMenuItems = MenuItemDiscoveryService.DiscoverAllMenuItems();
            
            // Check for cancellation before filtering
            cancellationToken.ThrowIfCancellationRequested();
            
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
            
            return Task.FromResult(response);
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
                    string.Equals(item.Path, filterText, System.StringComparison.OrdinalIgnoreCase)).ToList(),
                    
                MenuItemFilterType.startswith => menuItems.Where(item => 
                    item.Path.StartsWith(filterText, System.StringComparison.OrdinalIgnoreCase)).ToList(),
                    
                MenuItemFilterType.contains => menuItems.Where(item => 
                    item.Path.IndexOf(filterText, System.StringComparison.OrdinalIgnoreCase) >= 0).ToList(),
                    
                _ => menuItems
            };
        }
    }
}