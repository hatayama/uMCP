using System.ComponentModel;
using Newtonsoft.Json;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Schema for Ping command parameters
    /// Provides type-safe parameter access with immutable design
    /// Related classes:
    /// - BaseCommandSchema: Provides base timeout functionality
    /// - PingCommand: Uses this schema for ping message parameters
    /// </summary>
    public class PingSchema : BaseCommandSchema
    {
        /// <summary>
        /// Message to send to Unity
        /// </summary>
        [Description("Message to send to Unity")]
        public string Message { get; }

        /// <summary>
        /// Create PingSchema with all parameters
        /// </summary>
        [JsonConstructor]
        public PingSchema(string message = "Hello from TypeScript MCP Server", int timeoutSeconds = 10)
            : base(timeoutSeconds)
        {
            Message = message ?? "Hello from TypeScript MCP Server";
        }

        /// <summary>
        /// Parameterless constructor for new() constraint compatibility
        /// </summary>
        public PingSchema() : this("Hello from TypeScript MCP Server", 10)
        {
        }
    }
} 