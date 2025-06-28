namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Response schema for GetCommandDetails command
    /// Provides type-safe response structure for command information
    /// </summary>
    public class GetCommandDetailsResponse : BaseCommandResponse
    {
        /// <summary>
        /// Array of detailed command information
        /// </summary>
        public CommandInfo[] Commands { get; set; }
    }
} 