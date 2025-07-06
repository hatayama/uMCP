using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Unity console log retrieval tools for MCP C# SDK format
    /// Related classes:
    /// - GetLogsCommand: Legacy command version (will be deprecated)
    /// - GetLogsSchema: Legacy schema (will be deprecated)
    /// - GetLogsResponse: Legacy response (will be deprecated)
    /// - LogGetter: Core log retrieval logic
    /// - LogDisplayDto: Data structure from LogGetter
    /// - McpLogType: Log type enumeration
    /// </summary>
    [McpServerToolType]
    public static class GetLogsTools
    {
        /// <summary>
        /// Retrieve logs from Unity Console
        /// </summary>
        [McpServerTool(Name = "get-logs", TimeoutMs = 300000)] // 5 minutes timeout
        [Description("Retrieve logs from Unity Console")]
        public static Task<GetLogsToolResult> GetLogs(
            [Description("Log type to filter (Error, Warning, Log, All)")] 
            McpLogType LogType = McpLogType.All,
            [Description("Maximum number of logs to retrieve")]
            int MaxCount = 100,
            [Description("Text to search within log messages (retrieve all if empty)")]
            string SearchText = "",
            [Description("Whether to display stack trace")]
            bool IncludeStackTrace = true,
            [Description("Timeout in milliseconds (optional, default: 120000)")]
            int? timeoutMs = null,
            CancellationToken cancellationToken = default)
        {
            // Get Unity Console Log using the LogGetter class.
            LogDisplayDto logData;
            if (string.IsNullOrEmpty(SearchText))
            {
                if (LogType == McpLogType.All)
                {
                    logData = LogGetter.GetConsoleLog();
                }
                else
                {
                    logData = LogGetter.GetConsoleLog(LogType);
                }
            }
            else
            {
                logData = LogGetter.GetConsoleLog(LogType, SearchText);
            }
            
            // Limit logs according to maxCount.
            LogEntryDto[] limitedEntries = logData.LogEntries;
            if (limitedEntries.Length > MaxCount)
            {
                Array.Resize(ref limitedEntries, MaxCount);
            }
            
            // Create log entries
            LogEntry[] logs = limitedEntries.Select(entry => new LogEntry
            {
                LogType = entry.LogType,
                Message = entry.Message,
                StackTrace = IncludeStackTrace ? entry.StackTrace : null,
                InstanceId = entry.InstanceId,
                Timestamp = entry.Timestamp
            }).ToArray();
            
            GetLogsToolResult result = new GetLogsToolResult(
                totalCount: logData.TotalCount,
                displayedCount: limitedEntries.Length,
                logType: LogType.ToString(),
                maxCount: MaxCount,
                searchText: SearchText,
                includeStackTrace: IncludeStackTrace,
                logs: logs
            );
            
            return Task.FromResult(result);
        }
        
        /// <summary>
        /// Result for get-logs tool
        /// Compatible with legacy GetLogsResponse structure
        /// </summary>
        public class GetLogsToolResult : BaseCommandResponse
        {
            [Description("Total number of logs available")]
            public int TotalCount { get; set; }
            
            [Description("Number of logs displayed in this response")]
            public int DisplayedCount { get; set; }
            
            [Description("Log type filter used")]
            public string LogType { get; set; }
            
            [Description("Maximum count limit used")]
            public int MaxCount { get; set; }
            
            [Description("Search text filter used")]
            public string SearchText { get; set; }
            
            [Description("Whether stack trace was included")]
            public bool IncludeStackTrace { get; set; }
            
            [Description("Array of log entries")]
            public LogEntry[] Logs { get; set; }

            public GetLogsToolResult(int totalCount, int displayedCount, string logType, int maxCount, 
                                   string searchText, bool includeStackTrace, LogEntry[] logs)
            {
                TotalCount = totalCount;
                DisplayedCount = displayedCount;
                LogType = logType;
                MaxCount = maxCount;
                SearchText = searchText;
                IncludeStackTrace = includeStackTrace;
                Logs = logs ?? Array.Empty<LogEntry>();
            }
        }
    }
}