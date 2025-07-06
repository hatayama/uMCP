namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Represents a compilation issue (error or warning)
    /// </summary>
    public class CompileIssue
    {
        public string Message { get; set; }
        public string File { get; set; }
        public int Line { get; set; }
        
        public CompileIssue(string message, string file, int line)
        {
            Message = message;
            File = file;
            Line = line;
        }
    }

    /// <summary>
    /// Represents counts of cleared logs by type
    /// </summary>
    public class ClearedLogCounts
    {
        public int ErrorCount { get; set; }
        public int WarningCount { get; set; }
        public int LogCount { get; set; }
        public int TotalCount => ErrorCount + WarningCount + LogCount;
        
        public ClearedLogCounts(int errorCount, int warningCount, int logCount)
        {
            ErrorCount = errorCount;
            WarningCount = warningCount;
            LogCount = logCount;
        }
    }

    /// <summary>
    /// Base class for command schemas (for backward compatibility)
    /// </summary>
    public abstract class BaseCommandSchema
    {
        // Empty base class for legacy command schemas
    }
}