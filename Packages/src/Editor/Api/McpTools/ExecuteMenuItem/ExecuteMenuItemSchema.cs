using System.ComponentModel;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Schema for ExecuteMenuItem command parameters
    /// Provides type-safe parameter access for executing Unity MenuItems
    /// </summary>
    public class ExecuteMenuItemSchema : BaseCommandSchema
    {
        /// <summary>
        /// The menu item path to execute (e.g., "GameObject/Create Empty")
        /// </summary>
        [Description("The menu item path to execute (e.g., \"GameObject/Create Empty\")")]
        public string MenuItemPath { get; set; } = "";

        /// <summary>
        /// Whether to use reflection as fallback if EditorApplication.ExecuteMenuItem fails
        /// </summary>
        [Description("Whether to use reflection as fallback if EditorApplication.ExecuteMenuItem fails")]
        public bool UseReflectionFallback { get; set; } = true;
    }
}