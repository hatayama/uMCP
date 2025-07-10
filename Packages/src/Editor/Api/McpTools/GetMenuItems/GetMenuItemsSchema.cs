using System.ComponentModel;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Filter types for MenuItem search
    /// </summary>
    public enum MenuItemFilterType
    {
        /// <summary>
        /// Contains filter - partial match (case insensitive)
        /// </summary>
        contains,
        
        /// <summary>
        /// Exact match filter (case insensitive)
        /// </summary>
        exact,
        
        /// <summary>
        /// Starts with filter (case insensitive)
        /// </summary>
        startswith
    }

    /// <summary>
    /// Schema for GetMenuItems command parameters
    /// Provides type-safe parameter access for retrieving Unity MenuItems with filtering
    /// </summary>
    public class GetMenuItemsSchema : BaseToolSchema
    {
        /// <summary>
        /// Text to filter MenuItem paths (empty for all items)
        /// </summary>
        [Description("Text to filter MenuItem paths (empty for all items)")]
        public string FilterText { get; set; } = "";

        /// <summary>
        /// Type of filter to apply (contains, exact, startswith)
        /// </summary>
        [Description("Type of filter to apply (contains(0), exact(1), startswith(2))")]
        public MenuItemFilterType FilterType { get; set; } = MenuItemFilterType.contains;

        /// <summary>
        /// Include validation functions in the results
        /// </summary>
        [Description("Include validation functions in the results")]
        public bool IncludeValidation { get; set; } = false;

        /// <summary>
        /// Maximum number of menu items to retrieve
        /// </summary>
        [Description("Maximum number of menu items to retrieve")]
        public int MaxCount { get; set; } = 200;
    }
}