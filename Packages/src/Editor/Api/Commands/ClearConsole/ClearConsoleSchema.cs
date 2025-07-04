using System.ComponentModel;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Schema for ClearConsole command parameters
    /// Provides type-safe parameter access for clearing Unity Console
    /// Related classes:
    /// - ConsoleUtility: Service layer for console operations
    /// - ClearConsoleResponse: Type-safe response structure
    /// - ClearConsoleCommand: Command implementation
    /// </summary>
    public class ClearConsoleSchema : BaseCommandSchema
    {
        /// <summary>
        /// Whether to add a confirmation log message after clearing
        /// </summary>
        [Description("Whether to add a confirmation log message after clearing")]
        public bool AddConfirmationMessage { get; set; } = true;
    }
} 