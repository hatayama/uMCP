using System.ComponentModel;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Base schema class for all Unity MCP command parameters
    /// Provides common properties like timeout configuration with immutable design
    /// Related classes:
    /// - AbstractUnityCommand: Uses this schema as type constraint
    /// - All concrete schema classes: Inherit from this base class
    /// </summary>
    public abstract class BaseCommandSchema
    {
        /// <summary>
        /// Timeout for command execution in seconds (default: 10 seconds)
        /// </summary>
        [Description("Timeout for command execution in seconds (default: 10 seconds)")]
        public virtual int TimeoutSeconds { get; }

        /// <summary>
        /// Base constructor with default timeout
        /// </summary>
        protected BaseCommandSchema(int timeoutSeconds = 10)
        {
            TimeoutSeconds = timeoutSeconds;
        }

        /// <summary>
        /// Parameterless constructor for JSON deserialization
        /// </summary>
        protected BaseCommandSchema() : this(10)
        {
        }
    }
} 