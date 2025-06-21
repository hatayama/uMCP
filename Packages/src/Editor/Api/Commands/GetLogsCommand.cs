using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// GetLogs command handler.
    /// Retrieves logs from the Unity Console.
    /// </summary>
    public class GetLogsCommand : IUnityCommand
    {
        public string CommandName => "getlogs";
        public string Description => "Retrieve logs from Unity Console";

        public async Task<object> ExecuteAsync(JToken paramsToken)
        {
            string logType = paramsToken?["logType"]?.ToString() ?? McpServerConfig.DEFAULT_LOG_TYPE;
            int maxCount = paramsToken?["maxCount"]?.ToObject<int>() ?? McpServerConfig.DEFAULT_MAX_LOG_COUNT;
            string searchText = paramsToken?["searchText"]?.ToString() ?? McpServerConfig.DEFAULT_SEARCH_TEXT;
            bool includeStackTrace = paramsToken?["includeStackTrace"]?.ToObject<bool>() ?? McpServerConfig.DEFAULT_INCLUDE_STACK_TRACE;
            
            // Switch to the main thread using MainThreadSwitcher.
            await MainThreadSwitcher.SwitchToMainThread();
            
            // Get Unity Console Log using the LogGetter class.
            LogDisplayDto logData;
            if (string.IsNullOrEmpty(searchText))
            {
                if (logType == McpServerConfig.DEFAULT_LOG_TYPE)
                {
                    logData = LogGetter.GetConsoleLog();
                }
                else
                {
                    logData = LogGetter.GetConsoleLog(logType);
                }
            }
            else
            {
                logData = LogGetter.GetConsoleLog(logType, searchText);
            }
            
            // Limit logs according to maxCount.
            LogEntryDto[] limitedEntries = logData.LogEntries;
            if (limitedEntries.Length > maxCount)
            {
                Array.Resize(ref limitedEntries, maxCount);
            }
            
            // Create response object
            object response = new
            {
                totalCount = logData.TotalCount,
                displayedCount = limitedEntries.Length,
                logType = logType,
                maxCount = maxCount,
                searchText = searchText,
                includeStackTrace = includeStackTrace,
                                 logs = limitedEntries.Select(entry => new
                 {
                     type = entry.LogType,
                     message = entry.Message,
                     stackTrace = includeStackTrace ? entry.StackTrace : null,
                     file = entry.File
                 }).ToArray()
            };
            
            McpLogger.LogDebug($"GetLogs completed: Retrieved {limitedEntries.Length} logs out of {logData.TotalCount} total");
            
            return response;
        }
    }
} 