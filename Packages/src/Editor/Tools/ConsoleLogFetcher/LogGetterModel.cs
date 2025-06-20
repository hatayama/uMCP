using System;
using System.Collections.Generic;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Consoleログの取得を管理するModelクラス
    /// </summary>
    public class LogGetterModel : IDisposable
    {
        private readonly UnityLogEntriesAccessor logAccessor;

        public LogGetterModel()
        {
            logAccessor = new UnityLogEntriesAccessor();
        }

        public LogEntryDto[] GetConsoleLogEntries()
        {
            List<LogEntryDto> logEntries = new List<LogEntryDto>();

            if (!logAccessor.IsInitialized)
            {
                return logEntries.ToArray();
            }

            // フィルター制御が利用可能な場合のみフィルター操作を行う
            if (logAccessor.IsFilterControlAvailable)
            {
                // フィルター状態を保存し、すべてのフィルターを有効化
                logAccessor.SaveConsoleFlags();
                logAccessor.EnableAllConsoleFlags();
            }
            
            // ログ取得開始
            logAccessor.StartGettingEntries();
            
            int logCount = logAccessor.GetLogCount();

            for (int i = 0; i < logCount; i++)
            {
                object logEntry = logAccessor.GetLogEntry(i);
                LogEntryDto dto = LogEntryConverter.ConvertToDto(logEntry);
                
                if (dto != null)
                {
                    logEntries.Add(dto);
                }
            }
            
            // ログ取得終了
            logAccessor.EndGettingEntries();
            
            // フィルター制御が利用可能な場合のみフィルター状態を復元
            if (logAccessor.IsFilterControlAvailable)
            {
                logAccessor.RestoreConsoleFlags();
            }

            return logEntries.ToArray();
        }

        /// <summary>
        /// 指定されたログタイプでフィルタリングしたログエントリを取得する
        /// </summary>
        /// <param name="logType">フィルタリングするログタイプ（"Error", "Warning", "Log", "All"）</param>
        public LogEntryDto[] GetConsoleLogEntries(string logType)
        {
            LogEntryDto[] allEntries = GetConsoleLogEntries();
            
            if (string.IsNullOrEmpty(logType) || logType.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                return allEntries;
            }

            List<LogEntryDto> filteredEntries = new List<LogEntryDto>();
            foreach (LogEntryDto entry in allEntries)
            {
                if (entry.LogType.Equals(logType, StringComparison.OrdinalIgnoreCase))
                {
                    filteredEntries.Add(entry);
                }
            }

            return filteredEntries.ToArray();
        }

        public void Dispose()
        {
            logAccessor?.Dispose();
        }
    }
} 