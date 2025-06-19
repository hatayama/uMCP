using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// JSON-RPC 2.0処理を専門に扱うクラス
    /// </summary>
    public static class JsonRpcProcessor
    {
        /// <summary>
        /// JSON-RPCリクエストを処理してレスポンスを生成する
        /// </summary>
        public static async Task<string> ProcessRequest(string jsonRequest, CancellationToken cancellationToken = default)
        {
            // リクエストIDを生成
            string requestId = Guid.NewGuid().ToString();
            try
            {
                McpLogger.LogJsonRpc("RECEIVED", jsonRequest);
                
                JObject request = JObject.Parse(jsonRequest);
                string method = request["method"]?.ToString() ?? "unknown";
                
                // 安全にログ記録
                try
                {
                    await McpCommunicationLogger.LogRequestAsync(requestId, method, jsonRequest, cancellationToken);
                }
                catch (System.Exception logEx)
                {
                    McpLogger.LogError($"Failed to log request: {logEx.Message}");
                }
                JToken paramsToken = request["params"];
                object id = request["id"]?.ToObject<object>();

                object result = await ExecuteMethod(method, paramsToken, cancellationToken);

                // 成功レスポンス
                JObject response = new JObject
                {
                    ["jsonrpc"] = McpServerConfig.JSONRPC_VERSION,
                    ["id"] = JToken.FromObject(id),
                    ["result"] = JToken.FromObject(result)
                };

                string responseJson = response.ToString(Formatting.None);
                McpLogger.LogJsonRpc("SENT", responseJson);
                
                // 安全にログ記録
                try
                {
                    await McpCommunicationLogger.LogResponseAsync(requestId, responseJson, false, cancellationToken);
                }
                catch (System.Exception logEx)
                {
                    McpLogger.LogError($"Failed to log response: {logEx.Message}");
                }
                
                return responseJson;
            }
            catch (Exception ex)
            {
                McpLogger.LogError($"JSON-RPC processing error: {ex.Message}");
                
                // エラーレスポンス
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
                
                // 安全にログ記録
                try
                {
                    await McpCommunicationLogger.LogResponseAsync(requestId, errorJson, true, cancellationToken);
                }
                catch (System.Exception logEx)
                {
                    McpLogger.LogError($"Failed to log error response: {logEx.Message}");
                }
                
                return errorJson;
            }
        }

        /// <summary>
        /// メソッド名に応じて適切なハンドラーを実行する
        /// 新しいコマンドベースの構造を使用する
        /// </summary>
        private static async Task<object> ExecuteMethod(string method, JToken paramsToken, CancellationToken cancellationToken)
        {
            // 新しいコマンドベースの構造を使用
            return await UnityApiHandler.ExecuteCommandAsync(method, paramsToken, cancellationToken);
        }
    }
} 