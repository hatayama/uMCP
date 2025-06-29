using System;
using Newtonsoft.Json;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Individual log entry information
    /// </summary>
    [Serializable]
    public class LogEntry
    {
        public McpLogType Type { get; }
        public string Message { get; }
        public string StackTrace { get; }

        [JsonConstructor]
        public LogEntry(McpLogType type, string message, string stackTrace)
        {
            Type = type;
            Message = message ?? string.Empty;
            StackTrace = stackTrace ?? string.Empty;
        }
    }

    /// <summary>
    /// Response schema for GetLogs command
    /// Provides type-safe response structure
    /// </summary>
    public class GetLogsResponse : BaseCommandResponse
    {
        /// <summary>
        /// Total number of logs available
        /// </summary>
        public int TotalCount { get; }

        /// <summary>
        /// Number of logs displayed in this response
        /// </summary>
        public int DisplayedCount { get; }

        /// <summary>
        /// Log type filter used
        /// </summary>
        public string LogType { get; }

        /// <summary>
        /// Maximum count limit used
        /// </summary>
        public int MaxCount { get; }

        /// <summary>
        /// Search text filter used
        /// </summary>
        public string SearchText { get; }

        /// <summary>
        /// Whether stack trace was included
        /// </summary>
        public bool IncludeStackTrace { get; }

        /// <summary>
        /// Array of log entries
        /// </summary>
        public LogEntry[] Logs { get; }

        /// <summary>
        /// Create a new GetLogsResponse
        /// </summary>
        [JsonConstructor]
        public GetLogsResponse(int totalCount, int displayedCount, string logType, int maxCount, 
                              string searchText, bool includeStackTrace, LogEntry[] logs)
        {
            TotalCount = totalCount;
            DisplayedCount = displayedCount;
            LogType = logType ?? string.Empty;
            MaxCount = maxCount;
            SearchText = searchText ?? string.Empty;
            IncludeStackTrace = includeStackTrace;
            Logs = logs ?? Array.Empty<LogEntry>();
        }
    }
} 