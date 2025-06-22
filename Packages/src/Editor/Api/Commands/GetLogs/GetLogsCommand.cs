using System;
using System.Linq;
using System.Threading.Tasks;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// GetLogs command handler - Type-safe implementation using Schema and Response
    /// Retrieves Unity console logs with filtering options
    /// </summary>
    [McpTool]
    public class GetLogsCommand : AbstractUnityCommand<GetLogsSchema, GetLogsResponse>
    {
        public override string CommandName => "getlogs";
        public override string Description => "Retrieve logs from Unity Console";



        protected override async Task<GetLogsResponse> ExecuteAsync(GetLogsSchema parameters)
        {
            // Type-safe parameter access - no more string parsing!
            McpLogType logType = parameters.LogType;
            int maxCount = parameters.MaxCount;
            string searchText = parameters.SearchText;
            bool includeStackTrace = parameters.IncludeStackTrace;
            
            // Switch to the main thread using MainThreadSwitcher.
            await MainThreadSwitcher.SwitchToMainThread();
            
            // Convert enum to string for LogGetter
            string logTypeString = logType.ToString();
            
            // Get Unity Console Log using the LogGetter class.
            LogDisplayDto logData;
            if (string.IsNullOrEmpty(searchText))
            {
                if (logType == McpLogType.All)
                {
                    logData = LogGetter.GetConsoleLog();
                }
                else
                {
                    logData = LogGetter.GetConsoleLog(logTypeString);
                }
            }
            else
            {
                logData = LogGetter.GetConsoleLog(logTypeString, searchText);
            }
            
            // Limit logs according to maxCount.
            LogEntryDto[] limitedEntries = logData.LogEntries;
            if (limitedEntries.Length > maxCount)
            {
                Array.Resize(ref limitedEntries, maxCount);
            }
            
            // Create type-safe response
            LogEntry[] logs = limitedEntries.Select(entry => new LogEntry(
                type: entry.LogType,
                message: entry.Message,
                stackTrace: includeStackTrace ? entry.StackTrace : null,
                file: entry.File
            )).ToArray();
            
            GetLogsResponse response = new GetLogsResponse(
                totalCount: logData.TotalCount,
                displayedCount: limitedEntries.Length,
                logType: logTypeString,
                maxCount: maxCount,
                searchText: searchText,
                includeStackTrace: includeStackTrace,
                logs: logs
            );
            
            McpLogger.LogDebug($"GetLogs completed: Retrieved {limitedEntries.Length} logs out of {logData.TotalCount} total");
            
            return response;
        }
    }
} 