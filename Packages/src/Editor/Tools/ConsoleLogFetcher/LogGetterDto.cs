namespace io.github.hatayama.uMCP
{
    public class LogEntryDto
    {
        public string Message { get; }
        public string LogType { get; }
        public string StackTrace { get; }
        public string File { get; }

        public LogEntryDto(string message, string logType, string stackTrace, string file)
        {
            Message = message;
            LogType = logType;
            StackTrace = stackTrace;
            File = file;
        }
    }

    public class LogDisplayDto
    {
        public LogEntryDto[] LogEntries { get; }
        public int TotalCount { get; }

        public LogDisplayDto(LogEntryDto[] logEntries, int totalCount)
        {
            LogEntries = logEntries;
            TotalCount = totalCount;
        }
    }
} 