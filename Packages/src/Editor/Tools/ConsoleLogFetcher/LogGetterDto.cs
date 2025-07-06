using System;

namespace io.github.hatayama.uMCP
{
    [Serializable]
    public record LogEntryDto
    {
        public readonly string Message;
        public readonly McpLogType LogType;
        public readonly string StackTrace;
        public readonly int InstanceId;
        public readonly string Timestamp;

        public LogEntryDto(McpLogType logType, string message, string stackTrace, int instanceId = 0, string timestamp = null)
        {
            Message = message;
            LogType = logType;
            StackTrace = stackTrace;
            InstanceId = instanceId;
            Timestamp = timestamp ?? DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
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
