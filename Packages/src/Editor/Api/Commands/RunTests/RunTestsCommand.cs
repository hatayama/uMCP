using System;
using System.Threading.Tasks;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEditor;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Test execution command handler - Type-safe implementation using Schema and Response
    /// Executes tests using Unity Test Runner and returns the results
    /// </summary>
    [McpTool]
    public class RunTestsCommand : AbstractUnityCommand<RunTestsSchema, RunTestsResponse>
    {
        public override string CommandName => "runtests";
        public override string Description => "Execute Unity tests using Test Runner";

        protected override async Task<RunTestsResponse> ExecuteAsync(RunTestsSchema parameters)
        {
            // Adjust timeout for PlayMode tests if not explicitly set
            int timeoutSeconds = parameters.TimeoutSeconds;
            if (parameters.TestMode == TestMode.PlayMode && timeoutSeconds == 60)
            {
                // Default PlayMode timeout to 120 seconds
                timeoutSeconds = 120;
            }

            // Create filter if specified
            TestExecutionFilter filter = null;
            if (parameters.FilterType != TestFilterType.all)
            {
                filter = CreateFilter(parameters.FilterType.ToString(), parameters.FilterValue);
            }

            // Execute tests using unified PlayModeTestExecuter
            SerializableTestResult result = await PlayModeTestExecuter.ExecuteTests(
                parameters.TestMode, 
                filter, 
                parameters.SaveXml, 
                timeoutSeconds);

            return new RunTestsResponse(
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
        private TestExecutionFilter CreateFilter(string filterType, string filterValue)
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
    }
}