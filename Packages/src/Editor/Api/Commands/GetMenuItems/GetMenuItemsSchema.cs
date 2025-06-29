using System.ComponentModel;
using Newtonsoft.Json;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Filter types for GetMenuItems command
    /// </summary>
    public enum MenuItemFilterType
    {
        contains = 0,
        exact = 1,
        startswith = 2
    }

    /// <summary>
    /// Schema for GetMenuItems command parameters
    /// Provides type-safe parameter access with immutable design
    /// Related classes:
    /// - BaseCommandSchema: Provides base timeout functionality
    /// - GetMenuItemsCommand: Uses this schema for menu item retrieval parameters
    /// </summary>
    public class GetMenuItemsSchema : BaseCommandSchema
    {
        /// <summary>
        /// Text to filter MenuItem paths (empty for all items)
        /// </summary>
        [Description("Text to filter MenuItem paths (empty for all items)")]
        public string FilterText { get; }

        /// <summary>
        /// Type of filter to apply (contains, exact, startswith)
        /// </summary>
        [Description("Type of filter to apply (contains, exact, startswith)")]
        public MenuItemFilterType FilterType { get; }

        /// <summary>
        /// Maximum number of menu items to retrieve
        /// </summary>
        [Description("Maximum number of menu items to retrieve")]
        public int MaxCount { get; }

        /// <summary>
        /// Include validation functions in the results
        /// </summary>
        [Description("Include validation functions in the results")]
        public bool IncludeValidation { get; }

        /// <summary>
        /// Create GetMenuItemsSchema with all parameters
        /// </summary>
        [JsonConstructor]
        public GetMenuItemsSchema(string filterText = "", MenuItemFilterType filterType = MenuItemFilterType.contains, int maxCount = 200, bool includeValidation = false, int timeoutSeconds = 10)
            : base(timeoutSeconds)
        {
            FilterText = filterText ?? "";
            FilterType = filterType;
            MaxCount = maxCount;
            IncludeValidation = includeValidation;
        }

        /// <summary>
        /// Parameterless constructor for new() constraint compatibility
        /// </summary>
        public GetMenuItemsSchema() : this("", MenuItemFilterType.contains, 200, false, 10)
        {
        }
    }
}