namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Response schema for Ping command
    /// Provides type-safe response structure with immutable properties
    /// Related classes:
    /// - BaseCommandResponse: Provides timing information
    /// - PingCommand: Creates instances of this response
    /// </summary>
    public class PingResponse : BaseCommandResponse
    {
        /// <summary>
        /// The response message from Unity
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Create a new PingResponse
        /// </summary>
        /// <param name="message">Response message</param>
        public PingResponse(string message)
        {
            Message = message;
        }
    }
} 