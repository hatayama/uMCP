using System.Linq;
using System.Threading.Tasks;
using UnityEditor;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Compile command handler - Type-safe implementation using Schema and Response
    /// Handles Unity project compilation with optional force recompile
    /// </summary>
    [McpTool]
    public class CompileCommand : AbstractUnityCommand<CompileSchema, CompileResponse>
    {
        // SessionState key constants
        private const string SESSION_KEY_COMPILE_FROM_MCP = "uMCP.CompileFromMCP";
        
        public override string CommandName => "compile";
        public override string Description => "Execute Unity project compilation";



        /// <summary>
        /// Execute compile command
        /// </summary>
        /// <param name="parameters">Type-safe parameters</param>
        /// <returns>Compile result</returns>
        protected override async Task<CompileResponse> ExecuteAsync(CompileSchema parameters)
        {
            // Type-safe parameter access - no more string parsing!
            bool forceRecompile = parameters.ForceRecompile;

            await MainThreadSwitcher.SwitchToMainThread();

            // Set compile-via-MCP flag
            SessionState.SetBool(McpConstants.SESSION_KEY_COMPILE_FROM_MCP, true);

            try
            {
                McpLogger.LogDebug($"Compile request received: forceRecompile={forceRecompile}");

                // Set flag indicating compilation via MCP
                SessionState.SetBool(SESSION_KEY_COMPILE_FROM_MCP, true);
                McpLogger.LogInfo($"CompileCommand: Set {SESSION_KEY_COMPILE_FROM_MCP} = true");
                
                // Trigger command change notification BEFORE compilation starts
                // This ensures notification is sent while client is still connected
                UnityCommandRegistry.TriggerCommandsChangedNotification();
                McpLogger.LogDebug("CompileCommand: Sent commands changed notification before compilation");
                
                // Execute compilation using CompileChecker
                using CompileChecker compileChecker = new CompileChecker();
                CompileResult result = await compileChecker.TryCompileAsync(forceRecompile);

                McpLogger.LogInfo($"Compilation completed: Success={result.Success}, Errors={result.ErrorCount}, Warnings={result.WarningCount}");
                
                // Create type-safe response
                CompileIssue[] errors = result.error.Select(e => new CompileIssue(e.message, e.file, e.line)).ToArray();
                CompileIssue[] warnings = result.warning.Select(w => new CompileIssue(w.message, w.file, w.line)).ToArray();
                
                return new CompileResponse(
                    success: result.Success,
                    errorCount: result.error.Length,
                    warningCount: result.warning.Length,
                    completedAt: result.CompletedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    errors: errors,
                    warnings: warnings
                );
            }
            catch (System.Exception ex)
            {
                McpLogger.LogError($"CompileCommand: Compilation failed: {ex.Message}");
                return new CompileResponse(
                    success: false,
                    errorCount: 0,
                    warningCount: 0,
                    completedAt: System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    errors: System.Array.Empty<CompileIssue>(),
                    warnings: System.Array.Empty<CompileIssue>(),
                    message: $"Compilation failed: {ex.Message}"
                );
            }
            finally
            {
                // Clear compile-via-MCP flag
                SessionState.EraseBool(SESSION_KEY_COMPILE_FROM_MCP);
            }
        }
    }
}