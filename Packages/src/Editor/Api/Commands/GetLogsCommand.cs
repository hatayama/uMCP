using System;
using System.Collections.Generic;
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
        public CommandType CommandType => CommandType.GetLogs;

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
                LogEntryDto[] temp = new LogEntryDto[maxCount];
                Array.Copy(limitedEntries, temp, maxCount);
                limitedEntries = temp;
            }
            
            // Create a response object.
            List<object> logs = new List<object>();
            foreach (LogEntryDto entry in limitedEntries)
            {
                object logEntry;
                if (includeStackTrace)
                {
                    logEntry = new
                    {
                        type = entry.LogType,
                        message = entry.Message,
                        stackTrace = entry.StackTrace,
                        file = entry.File,
                        line = McpServerConfig.DEFAULT_LINE_NUMBER, // LogEntryDto does not have a line number, so set it to 0.
                        timestamp = System.DateTime.Now.ToString(McpServerConfig.TIMESTAMP_FORMAT)
                    };
                }
                else
                {
                    logEntry = new
                    {
                        type = entry.LogType,
                        message = entry.Message,
                        file = entry.File,
                        line = McpServerConfig.DEFAULT_LINE_NUMBER, // LogEntryDto does not have a line number, so set it to 0.
                        timestamp = System.DateTime.Now.ToString(McpServerConfig.TIMESTAMP_FORMAT)
                    };
                }
                logs.Add(logEntry);
            }
            
            object response = new
            {
                logs = logs.ToArray(),
                totalCount = logs.Count,
                requestedLogType = logType,
                requestedMaxCount = maxCount,
                requestedSearchText = searchText,
                requestedIncludeStackTrace = includeStackTrace
            };
            
            return response;
        }
    }
} 