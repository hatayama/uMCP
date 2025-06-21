using UnityEditor;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// A class that provides a general-purpose static API for retrieving console logs.
    /// Initializes and retains CustomLogManager appropriately with [InitializeOnLoad].
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
        /// Retrieves console logs and returns them as a LogDisplayDto.
        /// </summary>
        /// <returns>The retrieved log data.</returns>
        public static LogDisplayDto GetConsoleLog()
        {
            LogEntryDto[] logEntries = LogManager.GetAllLogEntries();
            return new LogDisplayDto(logEntries, logEntries.Length);
        }

        /// <summary>
        /// Directly retrieves an array of console log entries.
        /// </summary>
        /// <returns>An array of log entries.</returns>
        public static LogEntryDto[] GetConsoleLogEntries()
        {
            return LogManager.GetAllLogEntries();
        }

        /// <summary>
        /// Filters and retrieves console logs based on specified conditions.
        /// </summary>
        /// <param name="logType">The log type to filter by (if null, all types are retrieved).</param>
        /// <returns>The filtered log data.</returns>
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
        /// Filters and retrieves console logs by log type and message content.
        /// </summary>
        /// <param name="logType">The log type to filter by (if null or "All", all types are included).</param>
        /// <param name="searchText">The text to search for within messages (if null or empty, no search is performed).</param>
        /// <returns>The filtered log data.</returns>
        public static LogDisplayDto GetConsoleLog(string logType, string searchText)
        {
            LogEntryDto[] filteredEntries = LogManager.GetLogEntriesByTypeAndMessage(logType, searchText);
            return new LogDisplayDto(filteredEntries, filteredEntries.Length);
        }

        /// <summary>
        /// Gets the total number of console logs.
        /// </summary>
        /// <returns>The total number of logs.</returns>
        public static int GetConsoleLogCount()
        {
            return LogManager.GetLogCount();
        }

        /// <summary>
        /// Clears the logs of the custom log manager.
        /// </summary>
        public static void ClearCustomLogs()
        {
            LogManager.ClearLogs();
        }
    }
}