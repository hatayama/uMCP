using System;
using Newtonsoft.Json;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Response for HelloWorld command
    /// Contains greeting message with language and timestamp information
    /// Related classes:
    /// - BaseCommandResponse: Provides timing information
    /// - HelloWorldCommand: Creates instances of this response
    /// </summary>
    public class HelloWorldResponse : BaseCommandResponse
    {
        /// <summary>
        /// The greeting message
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Timestamp when the response was created
        /// </summary>
        public string Timestamp { get; }

        /// <summary>
        /// Language of the greeting
        /// </summary>
        public string Language { get; }

        /// <summary>
        /// Create a new HelloWorldResponse
        /// </summary>
        [JsonConstructor]
        public HelloWorldResponse(string message, string language, string timestamp)
        {
            Message = message ?? string.Empty;
            Language = language ?? string.Empty;
            Timestamp = timestamp ?? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
} 