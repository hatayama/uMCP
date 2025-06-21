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
        // SessionStateキー定数
        private const string SESSION_KEY_COMPILE_FROM_MCP = "uMCP.CompileFromMCP";
        
        public string CommandName => "compile";
        public string Description => "Execute Unity project compilation";

        public async Task<object> ExecuteAsync(JToken paramsToken)
        {
            bool forceRecompile = paramsToken?["forceRecompile"]?.ToObject<bool>() ?? false;

            McpLogger.LogDebug($"Compile request received: forceRecompile={forceRecompile}");

            // MCP経由のコンパイルであることを示すフラグを設定
            SessionState.SetBool(SESSION_KEY_COMPILE_FROM_MCP, true);
            McpLogger.LogInfo($"CompileCommand: Set {SESSION_KEY_COMPILE_FROM_MCP} = true");
            
            // CompileCheckerを使用してコンパイル実行
            using CompileChecker compileChecker = new CompileChecker();
            CompileResult result = await compileChecker.TryCompileAsync(forceRecompile);

            // レスポンス用のオブジェクトを作成
            object response = new
            {
                success = result.Success,
                errorCount = result.error.Length,
                warningCount = result.warning.Length,
                completedAt = result.CompletedAt,
                errors = result.error.Select(e => new { message = e.message, file = e.file, line = e.line }).ToArray(),
                warnings = result.warning.Select(w => new { message = w.message, file = w.file, line = w.line }).ToArray()
            };

            McpLogger.LogInfo($"Compile completed: Success={result.Success}, Errors={result.error.Length}, Warnings={result.warning.Length}");
            
            return response;
        }
    }
}