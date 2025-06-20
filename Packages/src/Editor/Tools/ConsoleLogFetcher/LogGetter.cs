using UnityEditor;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Consoleログ取得のための汎用的な静的APIを提供するクラス
    /// [InitializeOnLoad]でCustomLogManagerを適切に初期化・保持
    /// </summary>
    [InitializeOnLoad]
    public static class LogGetter
    {
        private static readonly CustomLogManager LogManager;
        
        static LogGetter()
        {
            LogManager = new CustomLogManager();
        }

        /// <summary>
        /// Consoleログを取得して LogDisplayDto として返す
        /// </summary>
        /// <returns>取得したログデータ</returns>
        public static LogDisplayDto GetConsoleLog()
        {
            LogEntryDto[] logEntries = LogManager.GetAllLogEntries();
            return new LogDisplayDto(logEntries, logEntries.Length);
        }

        /// <summary>
        /// Consoleログエントリの配列を直接取得する
        /// </summary>
        /// <returns>ログエントリの配列</returns>
        public static LogEntryDto[] GetConsoleLogEntries()
        {
            return LogManager.GetAllLogEntries();
        }

        /// <summary>
        /// 指定した条件でConsoleログをフィルタリングして取得する
        /// </summary>
        /// <param name="logType">フィルタするログタイプ（null の場合は全て取得）</param>
        /// <returns>フィルタされたログデータ</returns>
        public static LogDisplayDto GetConsoleLog(string logType)
        {
            LogEntryDto[] filteredEntries;
            
            if (string.IsNullOrEmpty(logType) || logType == "All")
            {
                filteredEntries = LogManager.GetAllLogEntries();
            }
            else
            {
                filteredEntries = LogManager.GetLogEntriesByType(logType);
            }
            
            return new LogDisplayDto(filteredEntries, filteredEntries.Length);
        }

        /// <summary>
        /// ログタイプとメッセージ内容でConsoleログをフィルタリングして取得する
        /// </summary>
        /// <param name="logType">フィルタするログタイプ（null または "All" の場合は全てのタイプ）</param>
        /// <param name="searchText">メッセージ内で検索するテキスト（null または空の場合は検索しない）</param>
        /// <returns>フィルタされたログデータ</returns>
        public static LogDisplayDto GetConsoleLog(string logType, string searchText)
        {
            LogEntryDto[] filteredEntries = LogManager.GetLogEntriesByTypeAndMessage(logType, searchText);
            return new LogDisplayDto(filteredEntries, filteredEntries.Length);
        }

        /// <summary>
        /// Consoleログの総数を取得する
        /// </summary>
        /// <returns>ログの総数</returns>
        public static int GetConsoleLogCount()
        {
            return LogManager.GetLogCount();
        }

        /// <summary>
        /// 独自ログマネージャーのログをクリアする
        /// </summary>
        public static void ClearCustomLogs()
        {
            LogManager.ClearLogs();
        }
    }
}