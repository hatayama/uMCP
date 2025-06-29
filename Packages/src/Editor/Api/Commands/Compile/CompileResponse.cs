using System;
using Newtonsoft.Json;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Compile error or warning information
    /// </summary>
    [Serializable]
    public class CompileIssue
    {
        public string Message { get; }
        public string File { get; }
        public int Line { get; }

        [JsonConstructor]
        public CompileIssue(string message, string file, int line)
        {
            Message = message ?? string.Empty;
            File = file ?? string.Empty;
            Line = line;
        }
    }

    /// <summary>
    /// Response schema for Compile command
    /// Provides type-safe response structure
    /// </summary>
    public class CompileResponse : BaseCommandResponse
    {
        /// <summary>
        /// Whether compilation was successful
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// Number of compilation errors
        /// </summary>
        public int ErrorCount { get; }

        /// <summary>
        /// Number of compilation warnings
        /// </summary>
        public int WarningCount { get; }

        /// <summary>
        /// Compilation completion timestamp
        /// </summary>
        public string CompletedAt { get; }

        /// <summary>
        /// Compilation errors
        /// </summary>
        public CompileIssue[] Errors { get; }

        /// <summary>
        /// Compilation warnings
        /// </summary>
        public CompileIssue[] Warnings { get; }

        /// <summary>
        /// Optional message for additional information
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Create a new CompileResponse
        /// </summary>
        [JsonConstructor]
        public CompileResponse(bool success, int errorCount, int warningCount, string completedAt, 
                             CompileIssue[] errors, CompileIssue[] warnings, string message = null)
        {
            Success = success;
            ErrorCount = errorCount;
            WarningCount = warningCount;
            CompletedAt = completedAt ?? string.Empty;
            Errors = errors ?? Array.Empty<CompileIssue>();
            Warnings = warnings ?? Array.Empty<CompileIssue>();
            Message = message ?? string.Empty;
        }
    }
} 