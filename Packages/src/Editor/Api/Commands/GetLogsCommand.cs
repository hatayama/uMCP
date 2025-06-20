using System;
using System.Collections.Generic;
using System.Linq;
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
            // 検索処理中のデバッグログ干渉を避けるため、searchTextが指定されている場合はデバッグログを無効化
            string logType = paramsToken?["logType"]?.ToString() ?? "All";
            int maxCount = paramsToken?["maxCount"]?.ToObject<int>() ?? 100;
            string searchText = paramsToken?["searchText"]?.ToString() ?? "";
            
            bool enableDebugLogs = string.IsNullOrEmpty(searchText);
            
            if (enableDebugLogs)
            {
                McpLogger.LogDebug($"GetLogs ExecuteAsync called with params: {paramsToken}");
                McpLogger.LogDebug($"GetLogs request received: logType={logType}, maxCount={maxCount}, searchText='{searchText}'");
            }
            
            try
            {
            
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
            
            if (enableDebugLogs)
            {
                McpLogger.LogDebug($"LogGetter returned: TotalCount={logData.TotalCount}, LogEntries.Length={logData.LogEntries.Length}");
                McpLogger.LogDebug($"Starting filter with searchText: '{searchText}', logData.LogEntries.Length: {logData.LogEntries.Length}");
            }
            
            // searchTextによるフィルタリングを実行
            List<LogEntryDto> filteredEntries = new List<LogEntryDto>();
            int checkedCount = 0;
            
            foreach (LogEntryDto entry in logData.LogEntries)
            {
                checkedCount++;
                bool shouldInclude = false;
                
                if (string.IsNullOrEmpty(searchText))
                {
                    shouldInclude = true;
                }
                else
                {
                    // MCPデバッグログを検索結果から除外
                    bool isMcpDebugLog = entry.Message != null && 
                                        (entry.Message.Contains("[Unity MCP]") || 
                                         entry.Message.Contains("[DEBUG]") ||
                                         entry.Message.Contains("GetLogs"));
                    
                    if (!isMcpDebugLog && entry.Message != null && 
                        entry.Message.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        shouldInclude = true;
                    }
                }
                
                if (shouldInclude)
                {
                    filteredEntries.Add(entry);
                }
            }
            
            if (enableDebugLogs)
            {
                McpLogger.LogDebug($"Filter completed: {filteredEntries.Count} entries match out of {checkedCount} total");
            }
            
            // maxCountに応じてログを制限
            LogEntryDto[] limitedEntries = filteredEntries.ToArray();
            if (limitedEntries.Length > maxCount)
            {
                LogEntryDto[] temp = new LogEntryDto[maxCount];
                Array.Copy(limitedEntries, temp, maxCount);
                limitedEntries = temp;
            }
            
            if (enableDebugLogs)
            {
                McpLogger.LogDebug($"After maxCount limit: limitedEntries.Length={limitedEntries.Length}");
            }
            
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
                totalCount = logData.TotalCount, // 元の総ログ数
                logType = logType,
                maxCount = maxCount,
                searchText = searchText
            };
            
            if (enableDebugLogs)
            {
                McpLogger.LogDebug($"GetLogs completed: Found {logs.Count} logs of type {logType} with searchText '{searchText}'");
            }
            
                return response;
            }
            catch (Exception ex)
            {
                if (enableDebugLogs)
                {
                    McpLogger.LogError($"GetLogs failed with exception: {ex.Message}");
                    McpLogger.LogError($"StackTrace: {ex.StackTrace}");
                }
                throw;
            }
        }
    }
} 