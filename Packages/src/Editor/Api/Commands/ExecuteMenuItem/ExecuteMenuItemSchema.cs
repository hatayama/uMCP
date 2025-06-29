using System.ComponentModel;
using Newtonsoft.Json;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Schema for ExecuteMenuItem command parameters
    /// Provides type-safe parameter access for executing Unity MenuItems with immutable design
    /// Related classes:
    /// - BaseCommandSchema: Provides base timeout functionality
    /// - ExecuteMenuItemCommand: Uses this schema for menu item execution parameters
    /// </summary>
    public class ExecuteMenuItemSchema : BaseCommandSchema
    {
        /// <summary>
        /// The menu item path to execute (e.g., "GameObject/Create Empty")
        /// </summary>
        [Description("The menu item path to execute (e.g., \"GameObject/Create Empty\")")]
        public string MenuItemPath { get; }

        /// <summary>
        /// Whether to use reflection as fallback if EditorApplication.ExecuteMenuItem fails
        /// </summary>
        [Description("Whether to use reflection as fallback if EditorApplication.ExecuteMenuItem fails")]
        public bool UseReflectionFallback { get; }

        /// <summary>
        /// Create ExecuteMenuItemSchema with all parameters
        /// </summary>
        [JsonConstructor]
        public ExecuteMenuItemSchema(string menuItemPath = "", bool useReflectionFallback = true, int timeoutSeconds = 10)
            : base(timeoutSeconds)
        {
            MenuItemPath = menuItemPath ?? "";
            UseReflectionFallback = useReflectionFallback;
        }

        /// <summary>
        /// Parameterless constructor for new() constraint compatibility
        /// </summary>
        public ExecuteMenuItemSchema() : this("", true, 10)
        {
        }
    }
}