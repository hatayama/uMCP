using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// GetLogsコマンドハンドラー
    /// Unity Consoleのログを取得する
    /// </summary>
    public class GetLogsCommand : IUnityCommand
    {
        public CommandType CommandType => CommandType.GetLogs;

        public async Task<object> ExecuteAsync(JToken paramsToken)
        {
            string logType = paramsToken?["logType"]?.ToString() ?? "All";
            int maxCount = paramsToken?["maxCount"]?.ToObject<int>() ?? 100;
            string searchText = paramsToken?["searchText"]?.ToString() ?? "";
            bool includeStackTrace = paramsToken?["includeStackTrace"]?.ToObject<bool>() ?? true;
            
            // MainThreadSwitcherを使用してメインスレッドに切り替え
            await MainThreadSwitcher.SwitchToMainThread();
            
            // LogGetterクラスを使ってUnity Console Logを取得
            LogDisplayDto logData;
            if (string.IsNullOrEmpty(searchText))
            {
                if (logType == "All")
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
            
            // maxCountに応じてログを制限
            LogEntryDto[] limitedEntries = logData.LogEntries;
            if (limitedEntries.Length > maxCount)
            {
                LogEntryDto[] temp = new LogEntryDto[maxCount];
                Array.Copy(limitedEntries, temp, maxCount);
                limitedEntries = temp;
            }
            
            // レスポンス用のオブジェクトを作成
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
                        line = 0, // LogEntryDtoには行番号がないため0を設定
                        timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    };
                }
                else
                {
                    logEntry = new
                    {
                        type = entry.LogType,
                        message = entry.Message,
                        file = entry.File,
                        line = 0, // LogEntryDtoには行番号がないため0を設定
                        timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
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