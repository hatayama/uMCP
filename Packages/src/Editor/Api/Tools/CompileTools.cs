using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Compilation tools for MCP C# SDK format
    /// Related classes:
    /// - CompileCommand: Legacy command version (will be deprecated)
    /// - CompileSchema: Legacy schema (will be deprecated)
    /// - CompileResponse: Legacy response (will be deprecated)
    /// - CompileChecker: Core compilation execution logic
    /// - CompileResult: Result data structure from CompileChecker
    /// </summary>
    [McpServerToolType]
    public static class CompileTools
    {
        /// <summary>
        /// Execute Unity project compilation
        /// </summary>
        [McpServerTool(Name = "compile")]
        [Description("Execute Unity project compilation")]
        public static async Task<CompileToolResult> Compile(
            [Description("Whether to perform forced recompilation")] 
            bool ForceRecompile = false,
            CancellationToken cancellationToken = default)
        {
            // Execute compilation using CompileChecker
            using CompileChecker compileChecker = new CompileChecker();
            CompileResult result = await compileChecker.TryCompileAsync(ForceRecompile);
            
            // Create compile issues from result
            CompileIssue[] errors = result.error.Select(e => new CompileIssue(e.message, e.file, e.line)).ToArray();
            CompileIssue[] warnings = result.warning.Select(w => new CompileIssue(w.message, w.file, w.line)).ToArray();
            
            return new CompileToolResult(
                success: result.Success,
                errorCount: result.error.Length,
                warningCount: result.warning.Length,
                completedAt: result.CompletedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                errors: errors,
                warnings: warnings
            );
        }
        
        /// <summary>
        /// Result for compile tool
        /// Compatible with legacy CompileResponse structure
        /// </summary>
        public class CompileToolResult : BaseCommandResponse
        {
            [Description("Whether compilation was successful")]
            public bool Success { get; set; }
            
            [Description("Number of compilation errors")]
            public int ErrorCount { get; set; }
            
            [Description("Number of compilation warnings")]
            public int WarningCount { get; set; }
            
            [Description("Compilation completion timestamp")]
            public string CompletedAt { get; set; }
            
            [Description("Compilation errors")]
            public CompileIssue[] Errors { get; set; }
            
            [Description("Compilation warnings")]
            public CompileIssue[] Warnings { get; set; }
            
            [Description("Optional message for additional information")]
            public string Message { get; set; }

            public CompileToolResult(bool success, int errorCount, int warningCount, string completedAt, 
                                   CompileIssue[] errors, CompileIssue[] warnings, string message = null)
            {
                Success = success;
                ErrorCount = errorCount;
                WarningCount = warningCount;
                CompletedAt = completedAt;
                Errors = errors ?? System.Array.Empty<CompileIssue>();
                Warnings = warnings ?? System.Array.Empty<CompileIssue>();
                Message = message;
            }
        }
    }
}