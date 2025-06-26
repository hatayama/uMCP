using System.Linq;
using System.Threading.Tasks;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// SetClientName command handler - Allows MCP clients to register their name
    /// This command is called by TypeScript clients to identify themselves
    /// </summary>
    [McpTool(DisplayDevelopmentOnly = true)]
    public class SetClientNameCommand : AbstractUnityCommand<SetClientNameSchema, SetClientNameResponse>
    {
        public override string CommandName => "setClientName";
        public override string Description => "Register client name for identification in Unity MCP server";

        protected override Task<SetClientNameResponse> ExecuteAsync(SetClientNameSchema parameters)
        {
            string clientName = parameters.ClientName;
            
            UpdateClientNameInServer(clientName);
            
            string message = string.Format(McpConstants.CLIENT_SUCCESS_MESSAGE_TEMPLATE, clientName);
            SetClientNameResponse response = new SetClientNameResponse(message, clientName);
            return Task.FromResult(response);
        }
        
        private void UpdateClientNameInServer(string clientName)
        {
            McpBridgeServer server = McpServerController.CurrentServer;
            if (server == null) return;
            
            var connectedClients = server.GetConnectedClients();
            if (connectedClients.Count == 0) return;
            
            // Try to find the most recently connected client with "Unknown Client" name
            ConnectedClient mostRecentUnknownClient = connectedClients
                .Where(c => c.ClientName == McpConstants.UNKNOWN_CLIENT_NAME)
                .OrderByDescending(c => c.ConnectedAt)
                .FirstOrDefault();
            
            if (mostRecentUnknownClient.Endpoint != null)
            {
                server.UpdateClientName(mostRecentUnknownClient.Endpoint, clientName);
                return;
            }
            
            // Fallback: update the most recent client regardless of name
            ConnectedClient mostRecentClient = connectedClients
                .OrderByDescending(c => c.ConnectedAt)
                .First();
            server.UpdateClientName(mostRecentClient.Endpoint, clientName);
        }
    }
}