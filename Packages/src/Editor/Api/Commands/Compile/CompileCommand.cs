using System.Linq;
using System.Threading.Tasks;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Compile command handler - Type-safe implementation using Schema and Response
    /// Handles Unity project compilation with optional force recompile
    /// DEPRECATED: Use CompileTools static class instead
    /// </summary>
    // [McpTool]  // Disabled to prevent registration
    public class CompileCommand : AbstractUnityCommand<CompileSchema, CompileResponse>
    {
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
            
            // Execute compilation using CompileChecker
            using CompileChecker compileChecker = new CompileChecker();
            CompileResult result = await compileChecker.TryCompileAsync(forceRecompile);
            
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
    }
}