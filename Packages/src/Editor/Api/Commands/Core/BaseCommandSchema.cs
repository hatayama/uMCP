using System.ComponentModel;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Base schema class for all Unity MCP command parameters
    /// Provides common properties like timeout configuration
    /// </summary>
    public abstract class BaseCommandSchema
    {
        /// <summary>
        /// Timeout for command execution in seconds (default: 300 seconds = 5 minutes)
        /// </summary>
        [Description("Timeout for command execution in seconds (default: 300 seconds = 5 minutes)")]
        public int TimeoutSeconds { get; set; } = 300;
    }
} 