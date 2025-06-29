using System.ComponentModel;
using Newtonsoft.Json;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Schema for SetClientName command parameters
    /// Provides type-safe parameter access with immutable design
    /// Related classes:
    /// - BaseCommandSchema: Provides base timeout functionality
    /// - SetClientNameCommand: Uses this schema for client name parameters
    /// </summary>
    public class SetClientNameSchema : BaseCommandSchema
    {
        /// <summary>
        /// Client name to set
        /// </summary>
        [Description("Client name to set")]
        public string ClientName { get; }

        /// <summary>
        /// Create SetClientNameSchema with all parameters
        /// </summary>
        [JsonConstructor]
        public SetClientNameSchema(string clientName = "", int timeoutSeconds = 10)
            : base(timeoutSeconds)
        {
            ClientName = clientName ?? McpConstants.UNKNOWN_CLIENT_NAME;
        }

        /// <summary>
        /// Parameterless constructor for new() constraint compatibility
        /// </summary>
        public SetClientNameSchema() : this(McpConstants.UNKNOWN_CLIENT_NAME, 10)
        {
        }
    }
}