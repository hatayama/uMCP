using System.ComponentModel;
using Newtonsoft.Json;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Supported log types for filtering
    /// </summary>
    public enum McpLogType
    {
        None = -1,
        Log = 0,
        Warning = 1,
        Error = 2,
        All = 3,
    }

    /// <summary>
    /// Schema for GetLogs command parameters
    /// Provides type-safe parameter access with immutable design
    /// Related classes:
    /// - BaseCommandSchema: Provides base timeout functionality
    /// - GetLogsCommand: Uses this schema for log retrieval parameters
    /// </summary>
    public class GetLogsSchema : BaseCommandSchema
    {
        /// <summary>
        /// Log type to filter (Error, Warning, Log, All)
        /// </summary>
        [Description("Log type to filter (Error, Warning, Log, All)")]
        public McpLogType LogType { get; }

        /// <summary>
        /// Maximum number of logs to retrieve
        /// </summary>
        [Description("Maximum number of logs to retrieve")]
        public int MaxCount { get; }

        /// <summary>
        /// Text to search within log messages (retrieve all if empty)
        /// </summary>
        [Description("Text to search within log messages (retrieve all if empty)")]
        public string SearchText { get; }

        /// <summary>
        /// Whether to display stack trace
        /// </summary>
        [Description("Whether to display stack trace")]
        public bool IncludeStackTrace { get; }

        /// <summary>
        /// Create GetLogsSchema with all parameters
        /// </summary>
        [JsonConstructor]
        public GetLogsSchema(McpLogType logType = McpLogType.All, int maxCount = 100, string searchText = "", bool includeStackTrace = true, int timeoutSeconds = 10)
            : base(timeoutSeconds)
        {
            LogType = logType;
            MaxCount = maxCount;
            SearchText = searchText ?? "";
            IncludeStackTrace = includeStackTrace;
        }

        /// <summary>
        /// Parameterless constructor for new() constraint compatibility
        /// </summary>
        public GetLogsSchema() : this(McpLogType.All, 100, "", true, 10)
        {
        }
    }
} 