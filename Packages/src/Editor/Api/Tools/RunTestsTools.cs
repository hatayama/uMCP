using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor.TestTools.TestRunner.Api;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Unity test execution tools for MCP C# SDK format
    /// Related classes:
    /// - RunTestsCommand: Legacy command version (will be deprecated)
    /// - RunTestsSchema: Legacy schema (will be deprecated)
    /// - RunTestsResponse: Legacy response (will be deprecated)
    /// - PlayModeTestExecuter: Core test execution logic
    /// - SerializableTestResult: Result data structure
    /// - TestFilterType: Filter type enumeration
    /// </summary>
    [McpServerToolType]
    public static class RunTestsTools
    {
        /// <summary>
        /// Execute Unity tests using Test Runner
        /// </summary>
        [McpServerTool(Name = "run-tests")]
        [Description("Execute Unity tests using Test Runner")]
        public static async Task<RunTestsToolResult> RunTests(
            [Description("Test mode (EditMode or PlayMode)")] 
            TestMode TestMode = TestMode.EditMode,
            [Description("Type of test filter")]
            TestFilterType FilterType = TestFilterType.All,
            [Description("Filter value (specify when filterType is not all)\n• fullclassname: Full class name (e.g.: io.github.hatayama.uMCP.CompileCommandTests)\n• namespace: Namespace (e.g.: io.github.hatayama.uMCP)\n• testname: Individual test name\n• assembly: Assembly name")]
            string FilterValue = "",
            [Description("Whether to save test results as XML file. Test results are saved to external files to avoid massive token consumption when returning results directly. Please read the file if you need detailed test results.")]
            bool SaveXml = false,
            [Description("Timeout for test execution in seconds (default: 30 seconds)")]
            int TimeoutSeconds = 30,
            CancellationToken cancellationToken = default)
        {
            // Create filter if specified
            TestExecutionFilter filter = null;
            if (FilterType != TestFilterType.All)
            {
                filter = CreateFilter(FilterType.ToString(), FilterValue);
            }

            // Execute tests using appropriate method
            SerializableTestResult result;
            if (TestMode == TestMode.PlayMode)
            {
                result = await PlayModeTestExecuter.ExecutePlayModeTest(
                    filter, 
                    SaveXml);
            }
            else
            {
                result = await PlayModeTestExecuter.ExecuteEditModeTest(
                    filter, 
                    SaveXml);
            }

            return new RunTestsToolResult(
                success: result.success,
                message: result.message,
                completedAt: result.completedAt,
                testCount: result.testCount,
                passedCount: result.passedCount,
                failedCount: result.failedCount,
                skippedCount: result.skippedCount,
                xmlPath: result.xmlPath
            );
        }
        
        /// <summary>
        /// Create test execution filter
        /// </summary>
        private static TestExecutionFilter CreateFilter(string filterType, string filterValue)
        {
            return filterType.ToLower() switch
            {
                "all" => TestExecutionFilter.All(), // Run all tests
                "fullclassname" => TestExecutionFilter.ByClassName(filterValue), // Full class name (e.g.: io.github.hatayama.uMCP.CompileCommandTests)
                "namespace" => TestExecutionFilter.ByNamespace(filterValue), // Namespace (e.g.: io.github.hatayama.uMCP)
                "testname" => TestExecutionFilter.ByTestName(filterValue), // Individual test name
                "assembly" => TestExecutionFilter.ByAssemblyName(filterValue), // Assembly name
                _ => throw new ArgumentException($"Unsupported filter type: {filterType}")
            };
        }
        
        /// <summary>
        /// Result for run-tests tool
        /// Compatible with legacy RunTestsResponse structure
        /// </summary>
        public class RunTestsToolResult : BaseCommandResponse
        {
            [Description("Whether test execution was successful")]
            public bool Success { get; set; }
            
            [Description("Test execution message")]
            public string Message { get; set; }
            
            [Description("Test execution completion timestamp")]
            public string CompletedAt { get; set; }
            
            [Description("Total number of tests executed")]
            public int TestCount { get; set; }
            
            [Description("Number of tests that passed")]
            public int PassedCount { get; set; }
            
            [Description("Number of tests that failed")]
            public int FailedCount { get; set; }
            
            [Description("Number of tests that were skipped")]
            public int SkippedCount { get; set; }
            
            [Description("Path to XML test results file (if SaveXml was true)")]
            public string XmlPath { get; set; }

            public RunTestsToolResult(bool success, string message, string completedAt, int testCount,
                                    int passedCount, int failedCount, int skippedCount, string xmlPath = null)
            {
                Success = success;
                Message = message;
                CompletedAt = completedAt;
                TestCount = testCount;
                PassedCount = passedCount;
                FailedCount = failedCount;
                SkippedCount = skippedCount;
                XmlPath = xmlPath;
            }
        }
    }
}