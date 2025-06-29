using Newtonsoft.Json;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Response for ExecuteMenuItem command
    /// Contains execution result and method information
    /// Related classes:
    /// - BaseCommandResponse: Provides timing information
    /// - ExecuteMenuItemCommand: Creates instances of this response
    /// </summary>
    public class ExecuteMenuItemResponse : BaseCommandResponse
    {
        /// <summary>
        /// The menu item path that was executed
        /// </summary>
        public string MenuItemPath { get; }

        /// <summary>
        /// Whether the execution was successful
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// The execution method used (EditorApplication or Reflection)
        /// </summary>
        public string ExecutionMethod { get; }

        /// <summary>
        /// Error message if execution failed
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// Additional information about the execution
        /// </summary>
        public string Details { get; }

        /// <summary>
        /// Whether the menu item was found in the system
        /// </summary>
        public bool MenuItemFound { get; }

        /// <summary>
        /// Create a new ExecuteMenuItemResponse with all parameters
        /// </summary>
        [JsonConstructor]
        public ExecuteMenuItemResponse(string menuItemPath, bool success, string executionMethod, 
                                     string errorMessage, string details, bool menuItemFound)
        {
            MenuItemPath = menuItemPath ?? string.Empty;
            Success = success;
            ExecutionMethod = executionMethod ?? string.Empty;
            ErrorMessage = errorMessage ?? string.Empty;
            Details = details ?? string.Empty;
            MenuItemFound = menuItemFound;
        }
    }
}