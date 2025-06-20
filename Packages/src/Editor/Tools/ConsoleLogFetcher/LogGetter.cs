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
                LogEntryDto[] filteredEntries = model.GetConsoleLogEntries(logType);
                return new LogDisplayDto(filteredEntries, filteredEntries.Length);
            }
        }

        /// <summary>
        /// Consoleログの総数を取得する（フィルター状態を無視）
        /// </summary>
        /// <returns>ログの総数</returns>
        public static int GetConsoleLogCount()
        {
            using (UnityLogEntriesAccessor accessor = new UnityLogEntriesAccessor())
            {
                return accessor.GetLogCountWithAllFlags();
            }
        }

    }
} 