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
        public override string CommandName => "run-tests";
        public override string Description => "Execute Unity tests using Test Runner";

        protected override async Task<RunTestsResponse> ExecuteAsync(RunTestsSchema parameters)
        {
            // Create filter if specified
            TestExecutionFilter filter = null;
            if (parameters.FilterType != TestFilterType.all)
            {
                filter = CreateFilter(parameters.FilterType.ToString(), parameters.FilterValue);
            }

            // Execute tests using appropriate method
            SerializableTestResult result;
            if (parameters.TestMode == TestMode.PlayMode)
            {
                result = await PlayModeTestExecuter.ExecutePlayModeTest(
                    filter, 
                    parameters.SaveXml);
            }
            else
            {
                result = await PlayModeTestExecuter.ExecuteEditModeTest(
                    filter, 
                    parameters.SaveXml);
            }

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