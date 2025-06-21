namespace io.github.hatayama.uMCP
{
    public record LogEntryDto
    {
        public readonly string Message;
        public readonly string LogType;
        public readonly string StackTrace;
        public readonly string File;

        public LogEntryDto(string message, string logType, string stackTrace, string file)
        {
            Message = message;
            LogType = logType;
            StackTrace = stackTrace;
            File = file;
        }
    }

    public record LogDisplayDto
    {
        public readonly LogEntryDto[] LogEntries;
        public readonly int TotalCount;

        public LogDisplayDto(LogEntryDto[] logEntries, int totalCount)
        {
            LogEntries = logEntries;
            TotalCount = totalCount;
        }
    }
}
