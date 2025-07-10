using System.ComponentModel;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Schema for SetClientName command parameters
    /// Allows TypeScript clients to register their name for identification
    /// </summary>
    public class SetClientNameSchema : BaseToolSchema
    {
        /// <summary>
        /// Name of the MCP client tool (e.g., "Claude Code", "Cursor")
        /// </summary>
        [Description("Name of the MCP client tool")]
        public string ClientName { get; set; } = McpConstants.UNKNOWN_CLIENT_NAME;
    }
}