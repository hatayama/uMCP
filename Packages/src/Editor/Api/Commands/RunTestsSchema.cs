using System.ComponentModel;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Supported test filter types
    /// </summary>
    public enum TestFilterType
    {
        all,
        fullclassname,
        @namespace,
        testname,
        assembly
    }

    /// <summary>
    /// Schema for RunTests command parameters
    /// Provides type-safe parameter access with default values
    /// </summary>
    public class RunTestsSchema : BaseCommandSchema
    {
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
        [Description("Whether to save test results as XML file")]
        public bool SaveXml { get; set; } = false;
    }
} 