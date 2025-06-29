using System;
using Newtonsoft.Json;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Response schema for ClearConsole command
    /// Provides type-safe response structure for console clearing operation
    /// Related classes:
    /// - BaseCommandResponse: Provides timing information
    /// - ClearConsoleCommand: Creates instances of this response
    /// - ClearedLogCounts: Value object for log count breakdown
    /// </summary>
    public class ClearConsoleResponse : BaseCommandResponse
    {
        /// <summary>
        /// Whether the console clear operation was successful
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// Number of logs that were cleared from the console
        /// </summary>
        public int ClearedLogCount { get; }

        /// <summary>
        /// Breakdown of cleared logs by type
        /// </summary>
        public ClearedLogCounts ClearedCounts { get; }

        /// <summary>
        /// Message describing the clear operation result
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Error message if the operation failed
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// Create a new ClearConsoleResponse
        /// </summary>
        [JsonConstructor]
        public ClearConsoleResponse(bool success, int clearedLogCount, ClearedLogCounts clearedCounts, string message, string errorMessage = "")
        {
            Success = success;
            ClearedLogCount = clearedLogCount;
            ClearedCounts = clearedCounts ?? new ClearedLogCounts();
            Message = message ?? string.Empty;
            ErrorMessage = errorMessage ?? string.Empty;
        }
    }

    /// <summary>
    /// Breakdown of cleared logs by type
    /// Immutable value object for log count information
    /// </summary>
    public class ClearedLogCounts
    {
        /// <summary>
        /// Number of error logs that were cleared
        /// </summary>
        public int ErrorCount { get; }

        /// <summary>
        /// Number of warning logs that were cleared
        /// </summary>
        public int WarningCount { get; }

        /// <summary>
        /// Number of info logs that were cleared
        /// </summary>
        public int LogCount { get; }

        [JsonConstructor]
        public ClearedLogCounts(int errorCount = 0, int warningCount = 0, int logCount = 0)
        {
            ErrorCount = errorCount;
            WarningCount = warningCount;
            LogCount = logCount;
        }
    }
} 