namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Response schema for SetClientName command
    /// Confirms client name registration with immutable properties
    /// Related classes:
    /// - BaseCommandResponse: Provides timing information
    /// - SetClientNameCommand: Creates instances of this response
    /// </summary>
    public class SetClientNameResponse : BaseCommandResponse
    {
        /// <summary>
        /// Success status message
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Registered client name
        /// </summary>
        public string ClientName { get; }

        /// <summary>
        /// Create a new SetClientNameResponse
        /// </summary>
        /// <param name="message">Success message</param>
        /// <param name="clientName">Registered client name</param>
        public SetClientNameResponse(string message, string clientName)
        {
            Message = message;
            ClientName = clientName;
        }
    }
}