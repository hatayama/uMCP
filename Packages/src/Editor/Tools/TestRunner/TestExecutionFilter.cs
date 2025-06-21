namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Type of test execution filter.
    /// </summary>
    public enum TestExecutionFilterType
    {
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
        /// Filter type.
        /// </summary>
        public TestExecutionFilterType FilterType { get; }
        
        /// <summary>
        /// Filter value.
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
    }
} 