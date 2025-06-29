using Newtonsoft.Json;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Response schema for GetCommandDetails command
    /// Provides type-safe response structure for Unity command information
    /// Related classes:
    /// - BaseCommandResponse: Provides timing information
    /// - GetCommandDetailsCommand: Creates instances of this response
    /// - CommandInfo: Individual command information structure
    /// </summary>
    public class GetCommandDetailsResponse : BaseCommandResponse
    {
        /// <summary>
        /// Array of available command information
        /// </summary>
        public CommandInfo[] Commands { get; }

        /// <summary>
        /// Create a new GetCommandDetailsResponse
        /// </summary>
        /// <param name="commands">Array of command information</param>
        [JsonConstructor]
        public GetCommandDetailsResponse(CommandInfo[] commands)
        {
            Commands = commands ?? System.Array.Empty<CommandInfo>();
        }
    }
} 