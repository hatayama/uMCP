namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Type of test execution filter.
    /// </summary>
    public enum TestExecutionFilterType
    {
        /// <summary>
        /// Run all tests without any filter.
        /// </summary>
        All,
        
        /// <summary>
        /// Filter by a specific test name.
        /// </summary>
        TestName,
        
        /// <summary>
        /// Filter by class name.
        /// </summary>
        ClassName,
        
        /// <summary>
        /// Filter by namespace.
        /// </summary>
        Namespace,
        
        /// <summary>
        /// Filter by assembly name.
        /// </summary>
        AssemblyName
    }
    
    /// <summary>
    /// Class that holds test execution filter information.
    /// </summary>
    public class TestExecutionFilter
    {
        /// <summary>
        /// The type of filter to apply (TestName, ClassName, Namespace, or AssemblyName).
        /// This determines how the FilterValue will be interpreted.
        /// </summary>
        public TestExecutionFilterType FilterType { get; }
        
        /// <summary>
        /// The specific value to filter tests by, based on the FilterType.
        /// Examples:
        /// • When FilterType is TestName: "MySpecificTestMethod"
        /// • When FilterType is ClassName: "io.github.hatayama.uMCP.CompileCommandTests"
        /// • When FilterType is Namespace: "io.github.hatayama.uMCP"
        /// • When FilterType is AssemblyName: "uMCP.Tests.Editor"
        /// </summary>
        public string FilterValue { get; }
        
        /// <summary>
        /// Creates a test execution filter.
        /// </summary>
        public TestExecutionFilter(TestExecutionFilterType filterType, string filterValue)
        {
            FilterType = filterType;
            FilterValue = filterValue;
        }
        
        /// <summary>
        /// Creates a filter by class name.
        /// </summary>
        public static TestExecutionFilter ByClassName(string className)
        {
            return new TestExecutionFilter(TestExecutionFilterType.ClassName, className);
        }
        
        /// <summary>
        /// Creates a filter by namespace.
        /// </summary>
        public static TestExecutionFilter ByNamespace(string namespaceName)
        {
            return new TestExecutionFilter(TestExecutionFilterType.Namespace, namespaceName);
        }
        
        /// <summary>
        /// Creates a filter by test name.
        /// </summary>
        public static TestExecutionFilter ByTestName(string testName)
        {
            return new TestExecutionFilter(TestExecutionFilterType.TestName, testName);
        }
        
        /// <summary>
        /// Creates a filter by assembly name.
        /// </summary>
        public static TestExecutionFilter ByAssemblyName(string assemblyName)
        {
            return new TestExecutionFilter(TestExecutionFilterType.AssemblyName, assemblyName);
        }
        
        /// <summary>
        /// Creates a filter to run all tests without any filter.
        /// </summary>
        public static TestExecutionFilter All()
        {
            return new TestExecutionFilter(TestExecutionFilterType.All, string.Empty);
        }
    }
} 