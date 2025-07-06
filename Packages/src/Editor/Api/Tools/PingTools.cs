using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Ping tools for MCP C# SDK format
    /// Related classes:
    /// - PingCommand: Legacy command version (will be deprecated)
    /// - PingSchema: Legacy schema (will be deprecated)
    /// - PingResponse: Legacy response (will be deprecated)
    /// </summary>
    [McpServerToolType]
    public static class PingTools
    {
        /// <summary>
        /// Connection test and message echo
        /// </summary>
        [McpServerTool(Name = "ping")]
        [Description("Connection test and message echo")]
        public static Task<PingToolResult> Ping(
            [Description("Message to send to Unity")] 
            string message = "Hello from TypeScript MCP Server",
            CancellationToken cancellationToken = default)
        {
            // Echo the message back with Unity prefix
            string response = $"Unity MCP Bridge received: {message}";
            
            return Task.FromResult(new PingToolResult(
                message: response,
                receivedMessage: message,
                timestamp: System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            ));
        }
        
        /// <summary>
        /// Result for ping tool
        /// </summary>
        public record PingToolResult(
            [property: Description("The response message from Unity")] string message,
            [property: Description("The original message that was received")] string receivedMessage,
            [property: Description("Timestamp when the ping was processed")] string timestamp
        );
    }
}