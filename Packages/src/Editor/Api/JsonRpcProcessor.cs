using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Class specialized in handling JSON-RPC 2.0 processing
    /// </summary>
    public static class JsonRpcProcessor
    {
        /// <summary>
        /// Process JSON-RPC request and generate response
        /// </summary>
        public static async Task<string> ProcessRequest(string jsonRequest)
        {
            McpLogger.LogJsonRpc("RECEIVED", jsonRequest);
            
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
                Id = request["id"]?.ToObject<object>(),
                IsNotification = request["id"] == null
            };
        }

        /// <summary>
        /// Process notification (fire-and-forget)
        /// </summary>
        private static void ProcessNotification(JsonRpcRequest request)
        {
            McpLogger.LogInfo($"Received notification: {request.Method}");
        }

        /// <summary>
        /// Process RPC request and return response JSON
        /// </summary>
        private static async Task<string> ProcessRpcRequest(JsonRpcRequest request, string originalJson)
        {
            await McpCommunicationLogger.LogRequest(originalJson);
            BaseCommandResponse result = await ExecuteMethod(request.Method, request.Params);
            string response = CreateSuccessResponse(request.Id, result);
            _ = McpCommunicationLogger.RecordLogResponse(response);
            return response;
        }

        /// <summary>
        /// Create JSON-RPC success response
        /// </summary>
        /// <param name="id">Request ID - must be same type as received (string/number/null per JSON-RPC spec)</param>
        /// <param name="result">Command execution result</param>
        private static string CreateSuccessResponse(object id, BaseCommandResponse result)
        {
            JObject response = new JObject
            {
                ["jsonrpc"] = McpServerConfig.JSONRPC_VERSION,
                ["id"] = id != null ? JToken.FromObject(id) : null,
                ["result"] = JToken.FromObject(result)
            };
            return response.ToString(Formatting.None);
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
            // Use new command-based structure
            return await UnityApiHandler.ExecuteCommandAsync(method, paramsToken);
        }
    }

    /// <summary>
    /// Represents a parsed JSON-RPC request
    /// </summary>
    internal class JsonRpcRequest
    {
        public string Method { get; set; }
        public JToken Params { get; set; }
        /// <summary>
        /// JSON-RPC 2.0 spec allows id to be string, number, or null.
        /// We use object type to preserve the original type sent by client.
        /// The response must return the same id type as received.
        /// </summary>
        public object Id { get; set; }
        public bool IsNotification { get; set; }
    }

    /// <summary>
    /// Represents a JSON-RPC response
    /// </summary>
    internal class JsonRpcResponse
    {
        public string JsonRpc { get; set; } = McpServerConfig.JSONRPC_VERSION;
        /// <summary>
        /// JSON-RPC 2.0 spec requires id type to match the request.
        /// Must be string, number, or null - same as received.
        /// </summary>
        public object Id { get; set; }
        public BaseCommandResponse Result { get; set; }
        public JsonRpcError Error { get; set; }
        
        public bool IsSuccess => Error == null;
    }

    /// <summary>
    /// Represents a JSON-RPC error
    /// </summary>
    internal class JsonRpcError
    {
        public int Code { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
    }
} 