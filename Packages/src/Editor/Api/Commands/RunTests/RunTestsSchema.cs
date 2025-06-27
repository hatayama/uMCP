using System.ComponentModel;
using UnityEditor.TestTools.TestRunner.Api;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Supported test filter types
    /// </summary>
    public enum TestFilterType
    {
        all,
        fullclassname
    }

    /// <summary>
    /// Schema for RunTests command parameters
    /// Provides type-safe parameter access with default values
    /// </summary>
    public class RunTestsSchema : BaseCommandSchema
    {
        /// <summary>
        /// Test mode (EditMode or PlayMode)
        /// </summary>
        [Description("Test mode (EditMode or PlayMode)")]
        public TestMode TestMode { get; set; } = TestMode.EditMode;

        /// <summary>
        /// Type of test filter
        /// </summary>
        [Description("Type of test filter")]
        public TestFilterType FilterType { get; set; } = TestFilterType.all;

        /// <summary>
        /// Filter value (specify when filterType is not all)
        /// • fullclassname: Full class name (e.g.: io.github.hatayama.uMCP.CompileCommandTests)
        /// • namespace: Namespace (e.g.: io.github.hatayama.uMCP)
        /// • testname: Individual test name
        /// • assembly: Assembly name
        /// </summary>
        [Description("Filter value (specify when filterType is not all)\n• fullclassname: Full class name (e.g.: io.github.hatayama.uMCP.CompileCommandTests)\n• namespace: Namespace (e.g.: io.github.hatayama.uMCP)\n• testname: Individual test name\n• assembly: Assembly name")]
        public string FilterValue { get; set; } = "";

        /// <summary>
        /// Whether to save test results as XML file
        /// </summary>
        [Description("Whether to save test results as XML file. Test results are saved to external files to avoid massive token consumption when returning results directly. Please read the file if you need detailed test results.")]
        public bool SaveXml { get; set; } = false;

        /// <summary>
        /// Timeout for test execution in seconds (default: 60 seconds for EditMode, 120 seconds for PlayMode)
        /// </summary>
        [Description("Timeout for test execution in seconds (default: 60 seconds for EditMode, 120 seconds for PlayMode)")]
        public override int TimeoutSeconds { get; set; } = 60;
    }
} 