using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Compileコマンドハンドラー
    /// Unityプロジェクトのコンパイルを実行する
    /// </summary>
    public class CompileCommand : IUnityCommand
    {
        public CommandType CommandType => CommandType.Compile;

        public async Task<object> ExecuteAsync(JToken paramsToken, CancellationToken cancellationToken = default)
        {
            bool forceRecompile = paramsToken?["forceRecompile"]?.ToObject<bool>() ?? false;

            McpLogger.LogDebug($"Compile request received (Two-Phase): forceRecompile={forceRecompile}");

            // 一意のリクエストIDを生成
            string requestId = System.Guid.NewGuid().ToString();

            // MCP経由のコンパイル要求をScriptableSingletonに保存
            var compileData = McpCompileData.instance;
            compileData.CompileFromMcp = true;
            compileData.AddCompileRequest(requestId, forceRecompile, "MCP-Client");
            
            McpLogger.LogInfo($"CompileCommand: Added compile request {requestId}, triggering AssetDatabase.Refresh()");

            // AssetDatabase.Refresh()を実行（これによりDomain Reloadが発生）
            AssetDatabase.Refresh();

            // 即座に受付応答を返す（Domain Reload前に）
            object acceptedResponse = new
            {
                status = "accepted",
                requestId = requestId,
                message = "Compile request accepted. Use getCompileResult to check status."
            };

            McpLogger.LogDebug($"Returning accepted response for request: {requestId}");
            return acceptedResponse;
        }
    }
}