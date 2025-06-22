using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;
using System.Collections.Generic;

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

        public CommandParameterSchema ParameterSchema => new CommandParameterSchema(
            new Dictionary<string, ParameterInfo>
            {
                ["forceRecompile"] = new ParameterInfo("boolean", "Whether to perform forced recompilation", false)
            }
        );

        /// <summary>
        /// Execute compile command
        /// </summary>
        /// <param name="paramsToken">Parameters</param>
        /// <returns>Compile result</returns>
        public async Task<object> ExecuteAsync(JToken paramsToken)
        {
            // Parse parameters
            bool forceRecompile = false;
            if (paramsToken != null && paramsToken.Type == JTokenType.Object)
            {
                forceRecompile = paramsToken["forceRecompile"]?.Value<bool>() ?? false;
            }

            await MainThreadSwitcher.SwitchToMainThread();

            // Set compile-via-MCP flag
            SessionState.SetBool(McpConstants.SESSION_KEY_COMPILE_FROM_MCP, true);

            try
            {
                McpLogger.LogDebug($"Compile request received: forceRecompile={forceRecompile}");

                // MCP経由のコンパイルであることを示すフラグを設定
                SessionState.SetBool(SESSION_KEY_COMPILE_FROM_MCP, true);
                McpLogger.LogInfo($"CompileCommand: Set {SESSION_KEY_COMPILE_FROM_MCP} = true");
                
                // Trigger command change notification BEFORE compilation starts
                // This ensures notification is sent while client is still connected
                UnityCommandRegistry.TriggerCommandsChangedNotification();
                McpLogger.LogDebug("CompileCommand: Sent commands changed notification before compilation");
                
                // CompileCheckerを使用してコンパイル実行
                using CompileChecker compileChecker = new CompileChecker();
                CompileResult result = await compileChecker.TryCompileAsync(forceRecompile);

                // レスポンス用のオブジェクトを作成
                return new
                {
                    success = result.Success,
                    errorCount = result.error.Length,
                    warningCount = result.warning.Length,
                    completedAt = result.CompletedAt,
                    errors = result.error.Select(e => new { message = e.message, file = e.file, line = e.line }).ToArray(),
                    warnings = result.warning.Select(w => new { message = w.message, file = w.file, line = w.line }).ToArray()
                };
            }
            catch (System.Exception ex)
            {
                McpLogger.LogError($"CompileCommand: Compilation failed: {ex.Message}");
                return new
                {
                    success = false,
                    message = $"Compilation failed: {ex.Message}",
                    errorCount = 0,
                    warningCount = 0,
                    completedAt = System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    errors = new object[0],
                    warnings = new object[0]
                };
            }
            finally
            {
                // Clear compile-via-MCP flag
                SessionState.EraseBool(SESSION_KEY_COMPILE_FROM_MCP);
            }
        }
    }
}