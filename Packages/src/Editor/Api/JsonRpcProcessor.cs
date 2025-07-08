using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Current client context for JSON-RPC processing
    /// </summary>
    public class ClientExecutionContext
    {
        public string Endpoint { get; }
        
        public ClientExecutionContext(string endpoint)
        {
            Endpoint = endpoint;
        }
    }

    /// <summary>
    /// Class specialized in handling JSON-RPC 2.0 processing
    /// 
    /// Design document reference: Packages/src/Editor/ARCHITECTURE.md
    /// 
    /// Related classes:
    /// - UnityApiHandler: Executes Unity commands based on JSON-RPC requests
    /// - McpBridgeServer: TCP server that receives JSON-RPC messages from TypeScript
    /// - MainThreadSwitcher: Ensures Unity API calls run on the main thread
    /// - JsonRpcRequest: Request model for JSON-RPC 2.0 protocol
    /// - McpCommunicationLog: Logs all JSON-RPC communication for debugging
    /// - ClientExecutionContext: Thread-local context for tracking current client
    /// 
    /// Processing flow:
    /// 1. Receives JSON message from McpBridgeServer
    /// 2. Parses and validates JSON-RPC 2.0 format
    /// 3. Sets client context for the current thread
    /// 4. Delegates to UnityApiHandler for command execution
    /// 5. Formats response according to JSON-RPC 2.0 specification
    /// 6. Returns JSON response to be sent back to client
    /// </summary>
    public static class JsonRpcProcessor
    {
        /// <summary>
        /// Current client context for async operations
        /// </summary>
        private static readonly AsyncLocal<ClientExecutionContext> _currentClientContext = new AsyncLocal<ClientExecutionContext>();
        
        /// <summary>
        /// Get current client context (ProcessID and Endpoint)
        /// </summary>
        public static ClientExecutionContext CurrentClientContext => _currentClientContext.Value;
        
        /// <summary>
        /// Process JSON-RPC request and generate response with client context
        /// </summary>
        public static async Task<string> ProcessRequest(string jsonRequest, string clientEndpoint)
        {
            var context = new ClientExecutionContext(clientEndpoint);
            _currentClientContext.Value = context;
            
            try
            {
                return await ProcessRequest(jsonRequest);
            }
            finally
            {
                _currentClientContext.Value = null;
            }
        }
        
        /// <summary>
        /// Process JSON-RPC request and generate response
        /// </summary>
        public static async Task<string> ProcessRequest(string jsonRequest)
        {
            try
            {
                JsonRpcRequest request = ParseRequest(jsonRequest);
                
                if (request.IsNotification)
                {
                    ProcessNotification(request);
                    return null;
                }
                
                return await ProcessRpcRequest(request, jsonRequest);
            }
            catch (JsonReaderException ex)
            {
                McpLogger.LogWarning($"JSON parse error (possibly incomplete data): {ex.Message}");
                return CreateErrorResponse(null, ex);
            }
            catch (Exception ex)
            {
                McpLogger.LogError($"JSON-RPC processing error: {ex.Message}");
                return CreateErrorResponse(null, ex);
            }
        }

        /// <summary>
        /// Parse JSON-RPC request string into structured object
        /// </summary>
        private static JsonRpcRequest ParseRequest(string jsonRequest)
        {
            JObject request = JObject.Parse(jsonRequest);
            return new JsonRpcRequest
            {
                Method = request["method"]?.ToString(),
                Params = request["params"],
                Id = request["id"]?.ToObject<object>()
            };
        }

        /// <summary>
        /// Process notification (fire-and-forget)
        /// </summary>
        private static void ProcessNotification(JsonRpcRequest request)
        {
            // Process notification silently
        }

        /// <summary>
        /// Process RPC request and return response JSON
        /// </summary>
        private static async Task<string> ProcessRpcRequest(JsonRpcRequest request, string originalJson)
        {
            try
            {
                await MainThreadSwitcher.SwitchToMainThread();
                await McpCommunicationLogger.LogRequest(originalJson);
                BaseCommandResponse result = await ExecuteMethod(request.Method, request.Params);
                string response = CreateSuccessResponse(request.Id, result);
                McpLogger.LogDebug($"Method: [{request.Method}], executed in {result.ExecutionTimeMs}ms");
                _ = McpCommunicationLogger.RecordLogResponse(response);
                return response;
            }
            catch (JsonSerializationException ex)
            {
                McpLogger.LogError($"JSON serialization error in method [{request.Method}]: {ex.Message}");
                UnityEngine.Debug.LogError($"[JsonRpcProcessor] JSON serialization error: {ex.Message}\nStack trace: {ex.StackTrace}");
                return CreateErrorResponse(request.Id, ex);
            }
            catch (Exception ex)
            {
                McpLogger.LogError($"Error processing method [{request.Method}]: {ex.Message}");
                UnityEngine.Debug.LogError($"[JsonRpcProcessor] Error: {ex.Message}\nStack trace: {ex.StackTrace}");
                return CreateErrorResponse(request.Id, ex);
            }
        }

        /// <summary>
        /// Create JSON-RPC success response
        /// </summary>
        /// <param name="id">Request ID - must be same type as received (string/number/null per JSON-RPC spec)</param>
        /// <param name="result">Command execution result</param>
        private static string CreateSuccessResponse(object id, BaseCommandResponse result)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                MaxDepth = 10
            };
            
            try
            {
                JObject response = new JObject
                {
                    ["jsonrpc"] = McpServerConfig.JSONRPC_VERSION,
                    ["id"] = id != null ? JToken.FromObject(id) : null,
                    ["result"] = JToken.FromObject(result, JsonSerializer.Create(settings))
                };
                return response.ToString(Formatting.None);
            }
            catch (Exception)
            {
                // Return safe fallback response for any serialization errors
                JObject fallbackResponse = new JObject
                {
                    ["jsonrpc"] = McpServerConfig.JSONRPC_VERSION,
                    ["id"] = id != null ? JToken.FromObject(id) : null,
                    ["result"] = new JObject
                    {
                        ["error"] = "Serialization failed - returning safe fallback",
                        ["commandType"] = result?.GetType()?.Name ?? "unknown"
                    }
                };
                return fallbackResponse.ToString(Formatting.None);
            }
        }

        /// <summary>
        /// Create JSON-RPC error response
        /// </summary>
        /// <param name="id">Request ID - must be same type as received (string/number/null per JSON-RPC spec)</param>
        /// <param name="ex">Exception to convert to error response</param>
        private static string CreateErrorResponse(object id, Exception ex)
        {
            JObject errorResponse = new JObject
            {
                ["jsonrpc"] = McpServerConfig.JSONRPC_VERSION,
                ["id"] = id != null ? JToken.FromObject(id) : null,
                ["error"] = new JObject
                {
                    ["code"] = McpServerConfig.INTERNAL_ERROR_CODE,
                    ["message"] = "Internal error",
                    ["data"] = ex.Message
                }
            };
            return errorResponse.ToString(Formatting.None);
        }

        /// <summary>
        /// Execute appropriate handler according to method name
        /// Use new command-based structure
        /// </summary>
        private static async Task<BaseCommandResponse> ExecuteMethod(string method, JToken paramsToken)
        {
            return await UnityApiHandler.ExecuteCommandAsync(method, paramsToken);
        }
    }
} 