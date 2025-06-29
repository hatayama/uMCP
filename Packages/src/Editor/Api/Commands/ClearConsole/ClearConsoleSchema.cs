using System.ComponentModel;
using Newtonsoft.Json;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Schema for ClearConsole command parameters
    /// Provides type-safe parameter access for clearing Unity Console with immutable design
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
        public bool AddConfirmationMessage { get; }

        /// <summary>
        /// Create ClearConsoleSchema with all parameters
        /// </summary>
        [JsonConstructor]
        public ClearConsoleSchema(bool addConfirmationMessage = true, int timeoutSeconds = 10)
            : base(timeoutSeconds)
        {
            AddConfirmationMessage = addConfirmationMessage;
        }

        /// <summary>
        /// Parameterless constructor for new() constraint compatibility
        /// </summary>
        public ClearConsoleSchema() : this(true, 10)
        {
        }
    }
} 