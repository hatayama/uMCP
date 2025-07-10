using System;

namespace io.github.hatayama.uMCP
{
    [Serializable]
    public record LogEntryDto
    {
        public readonly string Message;
        public readonly McpLogType LogType;
        public readonly string StackTrace;

        public LogEntryDto(McpLogType logType, string message, string stackTrace)
        {
            Message = message;
            LogType = logType;
            StackTrace = stackTrace;
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
