using System.Linq;
using System.Threading.Tasks;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// SetClientName command handler - Allows MCP clients to register their name
    /// This command is called by TypeScript clients to identify themselves
    /// DEPRECATED: Use SetClientNameTools static class instead
    /// </summary>
    // [McpTool(DisplayDevelopmentOnly = true)]  // Disabled to prevent registration
    public class SetClientNameCommand : AbstractUnityCommand<SetClientNameSchema, SetClientNameResponse>
    {
        public override string CommandName => "set-client-name";
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
            
            // Get current client context (ProcessID of the caller)
            var clientContext = JsonRpcProcessor.CurrentClientContext;
            if (clientContext == null)
            {
                McpLogger.LogWarning($"[SetClientName] No client context available for '{clientName}'");
                return;
            }
            
            // Debug: Log all connected clients and the caller info
            McpLogger.LogInfo($"[SetClientName] Setting name '{clientName}' for caller PID: {clientContext.ProcessId} ({clientContext.Endpoint})");
            McpLogger.LogInfo($"[SetClientName] Current clients:");
            foreach (var client in connectedClients)
            {
                McpLogger.LogInfo($"[SetClientName]   {client.Endpoint} (PID: {client.ProcessId}) - '{client.ClientName}'");
            }
            
            // Find client by ProcessID (direct match with caller)
            ConnectedClient targetClient = connectedClients
                .FirstOrDefault(c => c.ProcessId == clientContext.ProcessId);
            
            if (targetClient != null)
            {
                McpLogger.LogInfo($"[SetClientName] Found target client by ProcessID: {targetClient.Endpoint} (PID: {targetClient.ProcessId})");
                server.UpdateClientName(targetClient.Endpoint, clientName);
                return;
            }
            
            // Fallback: find by endpoint if ProcessID doesn't match (shouldn't happen)
            ConnectedClient fallbackClient = connectedClients
                .FirstOrDefault(c => c.Endpoint == clientContext.Endpoint);
            
            if (fallbackClient != null)
            {
                McpLogger.LogWarning($"[SetClientName] Fallback to endpoint match: {fallbackClient.Endpoint} (PID: {fallbackClient.ProcessId})");
                server.UpdateClientName(fallbackClient.Endpoint, clientName);
                return;
            }
            
            McpLogger.LogError($"[SetClientName] Could not find client for PID: {clientContext.ProcessId}, Endpoint: {clientContext.Endpoint}");
        }
    }
}