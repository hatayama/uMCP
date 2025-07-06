namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Log type enumeration for filtering console logs
    /// </summary>
    public enum McpLogType
    {
        /// <summary>
        /// Error logs
        /// </summary>
        Error = 0,
        
        /// <summary>
        /// Warning logs
        /// </summary>
        Warning = 1,
        
        /// <summary>
        /// Information logs
        /// </summary>
        Log = 2,
        
        /// <summary>
        /// All log types
        /// </summary>
        All = 3,
        
        /// <summary>
        /// No log type
        /// </summary>
        None = 4
    }

    /// <summary>
    /// Log entry data structure
    /// </summary>
    public class LogEntry
    {
        public string Message { get; set; }
        public string StackTrace { get; set; }
        public McpLogType LogType { get; set; }
        public int InstanceId { get; set; }
        public string Timestamp { get; set; }
    }
}