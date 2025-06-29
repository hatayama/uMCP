using System.Collections.Generic;
using Newtonsoft.Json;
using uMCP.Editor.Api.Commands.GetMenuItems;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Response for GetMenuItems command
    /// Contains discovered Unity MenuItems with filter information
    /// Related classes:
    /// - BaseCommandResponse: Provides timing information
    /// - GetMenuItemsCommand: Creates instances of this response
    /// - MenuItemInfo: Individual menu item data structure
    /// </summary>
    public class GetMenuItemsResponse : BaseCommandResponse
    {
        /// <summary>
        /// List of discovered MenuItems matching the filter criteria
        /// </summary>
        public List<MenuItemInfo> MenuItems { get; }

        /// <summary>
        /// Total number of MenuItems discovered before filtering
        /// </summary>
        public int TotalCount { get; }

        /// <summary>
        /// Number of MenuItems returned after filtering  
        /// </summary>
        public int FilteredCount { get; }

        /// <summary>
        /// The filter text that was applied
        /// </summary>
        public string AppliedFilter { get; }

        /// <summary>
        /// The filter type that was applied
        /// </summary>
        public string AppliedFilterType { get; }

        /// <summary>
        /// Create a new GetMenuItemsResponse with all required data
        /// </summary>
        [JsonConstructor]
        public GetMenuItemsResponse(List<MenuItemInfo> menuItems, int totalCount, int filteredCount, string appliedFilter, string appliedFilterType)
        {
            MenuItems = menuItems ?? new List<MenuItemInfo>();
            TotalCount = totalCount;
            FilteredCount = filteredCount;
            AppliedFilter = appliedFilter ?? string.Empty;
            AppliedFilterType = appliedFilterType ?? string.Empty;
        }
    }
}