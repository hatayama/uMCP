using System.Linq;
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

        public async Task<object> ExecuteAsync(JToken paramsToken)
        {
            bool forceRecompile = paramsToken?["forceRecompile"]?.ToObject<bool>() ?? false;

            McpLogger.LogDebug($"Compile request received: forceRecompile={forceRecompile}");

            // MCP経由のコンパイルであることを示すフラグを設定
            SessionState.SetBool("uMCP.CompileFromMCP", true);
            McpLogger.LogInfo("CompileCommand: Set uMCP.CompileFromMCP = true");
            
            // CompileCheckerを使用してコンパイル実行
            using CompileChecker compileChecker = new CompileChecker();
            CompileResult result = await compileChecker.TryCompileAsync(forceRecompile);

            // レスポンス用のオブジェクトを作成
            object response = new
            {
                success = result.Success,
                errorCount = result.ErrorCount,
                warningCount = result.WarningCount,
                completedAt = result.CompletedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                errors = result.Errors.Select(e => new
                {
                    message = e.message,
                    file = e.file,
                    line = e.line,
                    column = e.column,
                    type = e.type.ToString()
                }).ToArray(),
                warnings = result.Warnings.Select(w => new
                {
                    message = w.message,
                    file = w.file,
                    line = w.line,
                    column = w.column,
                    type = w.type.ToString()
                }).ToArray()
            };

            // MCP経由コンパイルフラグはアセンブリリロード後にクリアされるため、ここではクリアしない

            McpLogger.LogDebug($"Compile completed: Success={result.Success}, Errors={result.ErrorCount}, Warnings={result.WarningCount}");

            return response;
        }
    }
}