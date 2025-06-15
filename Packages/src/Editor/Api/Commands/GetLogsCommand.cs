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
            
            McpLogger.LogDebug($"GetLogs request received: logType={logType}, maxCount={maxCount}");
            
            // MainThreadSwitcherを使用してメインスレッドに切り替え
            await MainThreadSwitcher.SwitchToMainThread();
            
            // LogGetterクラスを使ってUnity Console Logを取得
            LogDisplayDto logData;
            if (logType == "All")
            {
                logData = LogGetter.GetConsoleLog();
            }
            else
            {
                logData = LogGetter.GetConsoleLog(logType);
            }
            
            McpLogger.LogDebug($"LogGetter returned: TotalCount={logData.TotalCount}, LogEntries.Length={logData.LogEntries.Length}");
            
            // maxCountに応じてログを制限
            LogEntryDto[] limitedEntries = logData.LogEntries;
            if (limitedEntries.Length > maxCount)
            {
                LogEntryDto[] temp = new LogEntryDto[maxCount];
                Array.Copy(limitedEntries, temp, maxCount);
                limitedEntries = temp;
            }
            
            McpLogger.LogDebug($"After maxCount limit: limitedEntries.Length={limitedEntries.Length}");
            
            // レスポンス用のオブジェクトを作成
            List<object> logs = new List<object>();
            foreach (LogEntryDto entry in limitedEntries)
            {
                McpLogger.LogDebug($"Processing log entry: Type={entry.LogType}, Message={entry.Message}");
                logs.Add(new
                {
                    type = entry.LogType,
                    message = entry.Message,
                    file = entry.File,
                    line = 0, // LogEntryDtoには行番号がないため0を設定
                    timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                });
            }
            
            object response = new
            {
                logs = logs.ToArray(),
                totalCount = logs.Count,
                requestedLogType = logType,
                requestedMaxCount = maxCount
            };
            
            McpLogger.LogDebug($"GetLogs completed: Found {logs.Count} logs of type {logType}");
            
            return response;
        }
    }
} 