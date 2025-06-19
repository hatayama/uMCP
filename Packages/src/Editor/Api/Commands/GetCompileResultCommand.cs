using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// GetCompileResultコマンドハンドラー
    /// 二段階通信でコンパイル結果を取得する
    /// </summary>
    public class GetCompileResultCommand : IUnityCommand
    {
        public CommandType CommandType => CommandType.GetCompileResult;

        public async Task<object> ExecuteAsync(JToken paramsToken, CancellationToken cancellationToken = default)
        {
            string requestId = paramsToken?["requestId"]?.ToString();
            
            if (string.IsNullOrEmpty(requestId))
            {
                return new
                {
                    status = "error",
                    message = "requestId parameter is required"
                };
            }

            McpLogger.LogDebug($"GetCompileResult request received: requestId={requestId}");

            var compileData = McpCompileData.instance;
            CompileRequestInfo requestInfo = compileData.GetCompileRequest(requestId);
            
            if (requestInfo == null)
            {
                return new
                {
                    status = "not_found",
                    message = $"Request ID {requestId} not found"
                };
            }

            // リクエストの状態に応じてレスポンスを作成
            if (requestInfo.status == "pending")
            {
                return new
                {
                    status = "pending",
                    requestId = requestId,
                    message = "Compilation is still in progress"
                };
            }
            else if (requestInfo.status == "completed")
            {
                // 結果JSONをパースして返す
                if (!string.IsNullOrEmpty(requestInfo.resultJson))
                {
                    JObject resultObject = JObject.Parse(requestInfo.resultJson);
                    return new
                    {
                        status = "completed",
                        requestId = requestId,
                        result = resultObject
                    };
                }
                else
                {
                    // 旧形式の結果
                    return new
                    {
                        status = "completed",
                        requestId = requestId,
                        result = new
                        {
                            success = requestInfo.success,
                            errorCount = requestInfo.errorCount,
                            warningCount = requestInfo.warningCount,
                            completedAt = requestInfo.completedAt
                        }
                    };
                }
            }
            else if (requestInfo.status == "failed")
            {
                return new
                {
                    status = "failed",
                    requestId = requestId,
                    error = requestInfo.resultJson
                };
            }

            return new
            {
                status = "unknown",
                requestId = requestId,
                message = $"Unknown request status: {requestInfo.status}"
            };
        }
    }
}