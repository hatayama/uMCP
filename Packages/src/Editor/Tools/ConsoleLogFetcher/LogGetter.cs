using System;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Consoleログ取得のための汎用的な静的APIを提供するクラス
    /// </summary>
    public static class LogGetter
    {
        /// <summary>
        /// Consoleログを取得して LogDisplayDto として返す
        /// </summary>
        /// <returns>取得したログデータ</returns>
        public static LogDisplayDto GetConsoleLog()
        {
            using (LogGetterModel model = new LogGetterModel())
            {
                LogEntryDto[] logEntries = model.GetConsoleLogEntries();
                return new LogDisplayDto(logEntries, logEntries.Length);
            }
        }

        /// <summary>
        /// Consoleログエントリの配列を直接取得する
        /// </summary>
        /// <returns>ログエントリの配列</returns>
        public static LogEntryDto[] GetConsoleLogEntries()
        {
            using (LogGetterModel model = new LogGetterModel())
            {
                return model.GetConsoleLogEntries();
            }
        }

        /// <summary>
        /// 指定した条件でConsoleログをフィルタリングして取得する
        /// </summary>
        /// <param name="logType">フィルタするログタイプ（null の場合は全て取得）</param>
        /// <returns>フィルタされたログデータ</returns>
        public static LogDisplayDto GetConsoleLog(string logType)
        {
            using (LogGetterModel model = new LogGetterModel())
            {
                LogEntryDto[] allEntries = model.GetConsoleLogEntries();
                
                if (string.IsNullOrEmpty(logType))
                {
                    return new LogDisplayDto(allEntries, allEntries.Length);
                }

                LogEntryDto[] filteredEntries = FilterLogsByType(allEntries, logType);
                return new LogDisplayDto(filteredEntries, filteredEntries.Length);
            }
        }

        /// <summary>
        /// Consoleログの総数を取得する
        /// </summary>
        /// <returns>ログの総数</returns>
        public static int GetConsoleLogCount()
        {
            using (UnityLogEntriesAccessor accessor = new UnityLogEntriesAccessor())
            {
                return accessor.GetLogCount();
            }
        }

        private static LogEntryDto[] FilterLogsByType(LogEntryDto[] entries, string logType)
        {
            System.Collections.Generic.List<LogEntryDto> filtered = new System.Collections.Generic.List<LogEntryDto>();
            
            foreach (LogEntryDto entry in entries)
            {
                if (string.Equals(entry.LogType, logType, StringComparison.OrdinalIgnoreCase))
                {
                    filtered.Add(entry);
                }
            }
            
            return filtered.ToArray();
        }
    }
} 