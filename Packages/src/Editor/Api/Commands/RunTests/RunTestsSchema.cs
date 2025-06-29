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
    /// Related classes:
    /// - BaseCommandSchema: Provides base timeout functionality
    /// - RunTestsCommand: Uses this schema for test execution parameters
    /// </summary>
    public class RunTestsSchema : BaseCommandSchema
    {
        /// <summary>
        /// Test mode (EditMode or PlayMode)
        /// </summary>
        [Description("Test mode (EditMode or PlayMode)")]
        public TestMode TestMode { get; }

        /// <summary>
        /// Type of test filter
        /// </summary>
        [Description("Type of test filter")]
        public TestFilterType FilterType { get; }

        /// <summary>
        /// Filter value (specify when filterType is not all)
        /// • fullclassname: Full class name (e.g.: io.github.hatayama.uMCP.CompileCommandTests)
        /// • namespace: Namespace (e.g.: io.github.hatayama.uMCP)
        /// • testname: Individual test name
        /// • assembly: Assembly name
        /// </summary>
        [Description("Filter value (specify when filterType is not all)\n• fullclassname: Full class name (e.g.: io.github.hatayama.uMCP.CompileCommandTests)\n• namespace: Namespace (e.g.: io.github.hatayama.uMCP)\n• testname: Individual test name\n• assembly: Assembly name")]
        public string FilterValue { get; }

        /// <summary>
        /// Whether to save test results as XML file
        /// </summary>
        [Description("Whether to save test results as XML file. Test results are saved to external files to avoid massive token consumption when returning results directly. Please read the file if you need detailed test results.")]
        public bool SaveXml { get; }

        /// <summary>
        /// Create RunTestsSchema with all parameters
        /// </summary>
        public RunTestsSchema(TestMode testMode = TestMode.EditMode, TestFilterType filterType = TestFilterType.all, string filterValue = "", bool saveXml = false, int timeoutSeconds = 30)
            : base(timeoutSeconds)
        {
            TestMode = testMode;
            FilterType = filterType;
            FilterValue = filterValue ?? "";
            SaveXml = saveXml;
        }

        /// <summary>
        /// Parameterless constructor for JSON deserialization
        /// </summary>
        public RunTestsSchema() : this(TestMode.EditMode, TestFilterType.all, "", false, 30)
        {
        }
    }
} 