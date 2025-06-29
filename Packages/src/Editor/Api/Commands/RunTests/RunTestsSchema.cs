using System.ComponentModel;
using Newtonsoft.Json;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Supported test modes for Unity Test Runner
    /// </summary>
    public enum McpTestMode
    {
        EditMode = 1,
        PlayMode = 2
    }

    /// <summary>
    /// Supported filter types for test execution
    /// </summary>
    public enum McpTestFilterType
    {
        all = 0,
        fullclassname = 1
    }

    /// <summary>
    /// Schema for RunTests command parameters
    /// Provides type-safe parameter access with immutable design
    /// Related classes:
    /// - BaseCommandSchema: Provides base timeout functionality
    /// - RunTestsCommand: Uses this schema for test execution parameters
    /// </summary>
    public class RunTestsSchema : BaseCommandSchema
    {
        /// <summary>
        /// Type of test filter
        /// </summary>
        [Description("Type of test filter")]
        public McpTestFilterType FilterType { get; }

        /// <summary>
        /// Filter value (specify when filterType is not all)
        /// </summary>
        [Description("Filter value (specify when filterType is not all)")]
        public string FilterValue { get; }

        /// <summary>
        /// Test mode (EditMode or PlayMode)
        /// </summary>
        [Description("Test mode (EditMode or PlayMode)")]
        public McpTestMode TestMode { get; }

        /// <summary>
        /// Whether to save test results as XML file
        /// </summary>
        [Description("Whether to save test results as XML file")]
        public bool SaveXml { get; }

        /// <summary>
        /// Create RunTestsSchema with all parameters
        /// </summary>
        [JsonConstructor]
        public RunTestsSchema(McpTestFilterType filterType = McpTestFilterType.all, string filterValue = "", McpTestMode testMode = McpTestMode.EditMode, bool saveXml = false, int timeoutSeconds = 30)
            : base(timeoutSeconds)
        {
            FilterType = filterType;
            FilterValue = filterValue ?? "";
            TestMode = testMode;
            SaveXml = saveXml;
        }

        /// <summary>
        /// Parameterless constructor for new() constraint compatibility
        /// </summary>
        public RunTestsSchema() : this(McpTestFilterType.all, "", McpTestMode.EditMode, false, 30)
        {
        }
    }
} 