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
            try
            {
                McpLogger.LogJsonRpc("RECEIVED", jsonRequest);
                McpCommunicationLogger.LogRequest(jsonRequest);
                
                JObject request = JObject.Parse(jsonRequest);
                string method = request["method"]?.ToString();
                JToken paramsToken = request["params"];
                object id = request["id"]?.ToObject<object>();

                object result = await ExecuteMethod(method, paramsToken);

                // Success response
                JObject response = new JObject
                {
                    ["jsonrpc"] = McpServerConfig.JSONRPC_VERSION,
                    ["id"] = JToken.FromObject(id),
                    ["result"] = JToken.FromObject(result)
                };

                string responseJson = response.ToString(Formatting.None);
                McpLogger.LogJsonRpc("SENT", responseJson);
                McpCommunicationLogger.LogResponse(responseJson);
                
                return responseJson;
            }
            catch (Exception ex)
            {
                McpLogger.LogError($"JSON-RPC processing error: {ex.Message}");
                
                // Error response
                JObject errorResponse = new JObject
                {
                    ["jsonrpc"] = McpServerConfig.JSONRPC_VERSION,
                    ["id"] = null,
                    ["error"] = new JObject
                    {
                        ["code"] = McpServerConfig.INTERNAL_ERROR_CODE,
                        ["message"] = "Internal error",
                        ["data"] = ex.Message
                    }
                };

                string errorJson = errorResponse.ToString(Formatting.None);
                McpLogger.LogJsonRpc("ERROR", errorJson);
                McpCommunicationLogger.LogResponse(errorJson);
                
                return errorJson;
            }
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
} 