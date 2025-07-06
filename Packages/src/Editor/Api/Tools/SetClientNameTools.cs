using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Client name management tools for MCP C# SDK format
    /// Related classes:
    /// - SetClientNameCommand: Legacy command version (will be deprecated)
    /// - SetClientNameSchema: Legacy schema (will be deprecated)
    /// - SetClientNameResponse: Legacy response (will be deprecated)
    /// - McpBridgeServer: Server instance for client management
    /// - JsonRpcProcessor: Client context provider
    /// </summary>
    [McpServerToolType]
    public static class SetClientNameTools
    {
        /// <summary>
        /// Register client name for identification in Unity MCP server
        /// </summary>
        [McpServerTool(Name = "set-client-name")]
        [Description("Register client name for identification in Unity MCP server")]
        public static Task<SetClientNameToolResult> SetClientName(
            [Description("Name of the MCP client tool")] 
            string clientName = McpConstants.UNKNOWN_CLIENT_NAME,
            CancellationToken cancellationToken = default)
        {
            UpdateClientNameInServer(clientName);
            
            string message = string.Format(McpConstants.CLIENT_SUCCESS_MESSAGE_TEMPLATE, clientName);
            
            return Task.FromResult(new SetClientNameToolResult(
                message: message,
                clientName: clientName,
                timestamp: System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                success: true
            ));
        }
        
        private static void UpdateClientNameInServer(string clientName)
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
        
        /// <summary>
        /// Result for set-client-name tool
        /// Compatible with legacy SetClientNameResponse structure
        /// </summary>
        public class SetClientNameToolResult : BaseCommandResponse
        {
            [Description("Success status message")]
            public string Message { get; set; }
            
            [Description("Registered client name")]
            public string ClientName { get; set; }
            
            [Description("Timestamp when the client name was set")]
            public string Timestamp { get; set; }
            
            [Description("Whether the operation was successful")]
            public bool Success { get; set; }

            public SetClientNameToolResult(string message, string clientName, string timestamp, bool success)
            {
                Message = message;
                ClientName = clientName;
                Timestamp = timestamp;
                Success = success;
            }
        }
    }
}