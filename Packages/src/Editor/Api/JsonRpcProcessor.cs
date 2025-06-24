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
            object result = await ExecuteMethod(request.Method, request.Params);
            string response = CreateSuccessResponse(request.Id, result);
            await McpCommunicationLogger.LogResponse(response);
            return response;
        }

        /// <summary>
        /// Create JSON-RPC success response
        /// </summary>
        private static string CreateSuccessResponse(object id, object result)
        {
            JObject response = new JObject
            {
                ["jsonrpc"] = McpServerConfig.JSONRPC_VERSION,
                ["id"] = JToken.FromObject(id),
                ["result"] = JToken.FromObject(result)
            };
            return response.ToString(Formatting.None);
        }

        /// <summary>
        /// Create JSON-RPC error response
        /// </summary>
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
        private static async Task<object> ExecuteMethod(string method, JToken paramsToken)
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
        public object Id { get; set; }
        public bool IsNotification { get; set; }
    }
} 