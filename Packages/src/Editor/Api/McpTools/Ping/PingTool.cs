using System.Threading.Tasks;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Ping tool handler - Type-safe implementation using Schema and Response
    /// Connection test and message echo functionality
    /// </summary>
    [McpTool(DisplayDevelopmentOnly = true)]
    public class PingTool : AbstractUnityTool<PingSchema, PingResponse>
    {
        public override string ToolName => "ping";
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