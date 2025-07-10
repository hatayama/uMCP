using System.Threading.Tasks;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Ping command handler - Type-safe implementation using Schema and Response
    /// Connection test and message echo functionality
    /// </summary>
    [McpTool(DisplayDevelopmentOnly = true)]
    public class PingCommand : AbstractUnityCommand<PingSchema, PingResponse>
    {
        public override string CommandName => "ping";
        public override string Description => "Connection test and message echo";


        protected override Task<PingResponse> ExecuteAsync(PingSchema parameters)
        {
            // Type-safe parameter access - no more string parsing!
            string message = parameters.Message;
            string response = $"Unity MCP Bridge received: {message}";

            // Create type-safe response
            PingResponse pingResponse = new PingResponse(response);
            return Task.FromResult(pingResponse);
        }
    }
}