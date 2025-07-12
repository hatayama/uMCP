using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace io.github.hatayama.uLoopMCP
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
                BaseToolResponse result = await ExecuteMethod(request.Method, request.Params);
                McpLogger.LogDebug($"Execution result type: {result?.GetType().Name}, Result: {result}");
                string response = CreateSuccessResponse(request.Id, result);
                McpLogger.LogDebug($"Created JSON response length: {response?.Length ?? 0}");
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
        private static string CreateSuccessResponse(object id, BaseToolResponse result)
        {
            McpLogger.LogDebug($"CreateSuccessResponse called with id: {id}, result type: {result?.GetType().Name}");
            
            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                MaxDepth = McpServerConfig.DEFAULT_JSON_MAX_DEPTH
            };
            
            try
            {
                JsonRpcSuccessResponse response = new JsonRpcSuccessResponse(
                    McpServerConfig.JSONRPC_VERSION,
                    id,
                    result
                );
                McpLogger.LogDebug("JsonRpcSuccessResponse created, serializing...");
                string serialized = JsonConvert.SerializeObject(response, Formatting.None, settings);
                McpLogger.LogDebug($"Serialization successful, length: {serialized?.Length ?? 0}");
                return serialized;
            }
            catch (Exception)
            {
                // Return safe fallback response for any serialization errors
                object fallbackResult = new
                {
                    error = "Serialization failed - returning safe fallback",
                    commandType = result != null ? result.GetType().Name : "unknown"
                };
                
                JsonRpcSuccessResponse fallbackResponse = new JsonRpcSuccessResponse(
                    McpServerConfig.JSONRPC_VERSION,
                    id,
                    fallbackResult
                );
                return JsonConvert.SerializeObject(fallbackResponse, Formatting.None);
            }
        }

        /// <summary>
        /// Create JSON-RPC error response
        /// </summary>
        /// <param name="id">Request ID - must be same type as received (string/number/null per JSON-RPC spec)</param>
        /// <param name="ex">Exception to convert to error response</param>
        private static string CreateErrorResponse(object id, Exception ex)
        {
            JsonRpcErrorData errorData;
            string errorMessage;
            
            // Handle security exceptions with detailed information
            if (ex is McpSecurityException secEx)
            {
                errorData = new SecurityBlockedErrorData(secEx.ToolName, secEx.SecurityReason, secEx.Message);
                errorMessage = "Tool blocked by security settings";
            }
            // Handle timeout exceptions with detailed information
            else if (ex is TimeoutException timeoutEx)
            {
                errorData = new InternalErrorData(timeoutEx.Message);
                errorMessage = "Request timeout";
            }
            else
            {
                errorData = new InternalErrorData(ex.Message);
                errorMessage = "Internal error";
            }
            
            JsonRpcErrorResponse errorResponse = new JsonRpcErrorResponse(
                McpServerConfig.JSONRPC_VERSION,
                id,
                new JsonRpcError(McpServerConfig.INTERNAL_ERROR_CODE, errorMessage, errorData)
            );
            
            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                MaxDepth = McpServerConfig.DEFAULT_JSON_MAX_DEPTH
            };
            
            return JsonConvert.SerializeObject(errorResponse, Formatting.None, settings);
        }

        /// <summary>
        /// Execute appropriate handler according to method name
        /// Use new command-based structure
        /// </summary>
        private static async Task<BaseToolResponse> ExecuteMethod(string method, JToken paramsToken)
        {
            return await UnityApiHandler.ExecuteCommandAsync(method, paramsToken);
        }
    }

    /// <summary>
    /// Constants for JSON-RPC error types
    /// </summary>
    public static class JsonRpcErrorTypes
    {
        public const string SecurityBlocked = "security_blocked";
        public const string InternalError = "internal_error";
    }

    /// <summary>
    /// Base class for JSON-RPC error data
    /// </summary>
    public abstract class JsonRpcErrorData
    {
        public abstract string type { get; }
        
        public string message { get; protected set; }
        
        protected JsonRpcErrorData(string message)
        {
            this.message = message;
        }
    }

    /// <summary>
    /// Error data for security blocked commands
    /// </summary>
    public class SecurityBlockedErrorData : JsonRpcErrorData
    {
        public override string type => JsonRpcErrorTypes.SecurityBlocked;
        
        public string command { get; }
        
        public string reason { get; }
        
        public SecurityBlockedErrorData(string command, string reason, string message) : base(message)
        {
            this.command = command;
            this.reason = reason;
        }
    }

    /// <summary>
    /// Error data for internal errors
    /// </summary>
    public class InternalErrorData : JsonRpcErrorData
    {
        public override string type => JsonRpcErrorTypes.InternalError;
        
        public InternalErrorData(string message) : base(message)
        {
        }
    }

    /// <summary>
    /// JSON-RPC error object
    /// </summary>
    public class JsonRpcError
    {
        public int code { get; }
        
        public string message { get; }
        
        public JsonRpcErrorData data { get; }
        
        public JsonRpcError(int code, string message, JsonRpcErrorData data)
        {
            this.code = code;
            this.message = message;
            this.data = data;
        }
    }

    /// <summary>
    /// JSON-RPC success response
    /// </summary>
    public class JsonRpcSuccessResponse
    {
        public string jsonrpc { get; }
        
        public object id { get; }
        
        public object result { get; }
        
        public JsonRpcSuccessResponse(string jsonRpc, object id, object result)
        {
            this.jsonrpc = jsonRpc;
            this.id = id;
            this.result = result;
        }
    }

    /// <summary>
    /// JSON-RPC error response
    /// </summary>
    public class JsonRpcErrorResponse
    {
        public string jsonrpc { get; }
        
        public object id { get; }
        
        public JsonRpcError error { get; }
        
        public JsonRpcErrorResponse(string jsonRpc, object id, JsonRpcError error)
        {
            this.jsonrpc = jsonRpc;
            this.id = id;
            this.error = error;
        }
    }
} 