using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEditor;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Test execution command handler
    /// Executes tests using Unity Test Runner and returns the results
    /// </summary>
    public class RunTestsCommand : IUnityCommand
    {
        public string CommandName => "runtests";
        public string Description => "Execute Unity tests using Test Runner";

        public CommandParameterSchema ParameterSchema => new CommandParameterSchema(
            new Dictionary<string, ParameterInfo>
            {
                ["filterType"] = new ParameterInfo("string", "Type of test filter", "all", new[] { "all", "fullclassname", "namespace", "testname", "assembly" }),
                ["filterValue"] = new ParameterInfo("string", "Filter value (specify when filterType is not all)\n• fullclassname: Full class name (e.g.: io.github.hatayama.uMCP.CompileCommandTests)\n• namespace: Namespace (e.g.: io.github.hatayama.uMCP)\n• testname: Individual test name\n• assembly: Assembly name", ""),
                ["saveXml"] = new ParameterInfo("boolean", "Whether to save test results as XML file", false)
            }
        );

        public async Task<object> ExecuteAsync(JToken paramsToken)
        {
            // Parse parameters
            TestExecutionParameters parameters = ParseParameters(paramsToken);
            
            McpLogger.LogInfo($"Test execution started - Filter: {parameters.FilterType}, Value: {parameters.FilterValue}, Save XML: {parameters.SaveXml}");

            try
            {
                // Create test execution manager
                UnityTestExecutionManager testManager = new UnityTestExecutionManager();
                
                // Create completion source for async operation
                TaskCompletionSource<TestExecutionResult> completionSource = new TaskCompletionSource<TestExecutionResult>();
                
                // Set up result processing
                void ProcessResult(ITestResultAdaptor result)
                {
                    ProcessTestResult(result, parameters.SaveXml, completionSource);
                }
                
                // Execute tests based on filter
                if (string.IsNullOrEmpty(parameters.FilterValue))
                {
                    testManager.RunEditModeTests(ProcessResult);
                }
                else
                {
                    TestExecutionFilter filter = CreateFilter(parameters.FilterType, parameters.FilterValue);
                    testManager.RunEditModeTests(filter, ProcessResult);
                }
                
                // Wait for completion
                TestExecutionResult result = await completionSource.Task;
                
                McpLogger.LogInfo($"Test execution completed: Success={result.Success}, Message={result.Message}");
                
                return new
                {
                    success = result.Success,
                    message = result.Message,
                    completedAt = result.CompletedAt,
                    testCount = result.TestCount,
                    passedCount = result.PassedCount,
                    failedCount = result.FailedCount,
                    skippedCount = result.SkippedCount,
                    xmlPath = result.XmlPath
                };
            }
            catch (Exception ex)
            {
                McpLogger.LogError($"Test execution failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Parse parameters from JSON token
        /// </summary>
        private TestExecutionParameters ParseParameters(JToken paramsToken)
        {
            string filterType = paramsToken?["filterType"]?.ToString() ?? "all";
            string filterValue = paramsToken?["filterValue"]?.ToString() ?? "";
            bool saveXml = paramsToken?["saveXml"]?.ToObject<bool>() ?? false;
            
            return new TestExecutionParameters(filterType, filterValue, saveXml);
        }

        /// <summary>
        /// Create test execution filter
        /// </summary>
        private TestExecutionFilter CreateFilter(string filterType, string filterValue)
        {
            return filterType.ToLower() switch
            {
                "fullclassname" => TestExecutionFilter.ByClassName(filterValue), // Full class name (e.g.: io.github.hatayama.uMCP.CompileCommandTests)
                "namespace" => TestExecutionFilter.ByNamespace(filterValue),      // Namespace (e.g.: io.github.hatayama.uMCP)
                "testname" => TestExecutionFilter.ByTestName(filterValue),        // Individual test name
                "assembly" => TestExecutionFilter.ByAssemblyName(filterValue),    // Assembly name
                _ => throw new ArgumentException($"Unsupported filter type: {filterType}")
            };
        }

        /// <summary>
        /// Process test results
        /// </summary>
        private void ProcessTestResult(ITestResultAdaptor result, bool saveXml, TaskCompletionSource<TestExecutionResult> completionSource)
        {
            try
            {
                TestExecutionResult testResult = new TestExecutionResult
                {
                    Success = result.TestStatus == TestStatus.Passed,
                    Message = $"Test execution completed with status: {result.TestStatus}",
                    CompletedAt = DateTime.Now,
                    TestCount = CountTotalTests(result),
                    PassedCount = CountPassedTests(result),
                    FailedCount = CountFailedTests(result),
                    SkippedCount = CountSkippedTests(result)
                };

                // Save XML if requested
                if (saveXml)
                {
                    string xmlPath = NUnitXmlResultExporter.SaveTestResultAsXml(result);
                    testResult.XmlPath = xmlPath;
                    McpLogger.LogInfo($"Test results saved to XML: {xmlPath}");
                }

                completionSource.SetResult(testResult);
            }
            catch (Exception ex)
            {
                completionSource.SetException(ex);
            }
        }

        /// <summary>
        /// Count total tests
        /// </summary>
        private int CountTotalTests(ITestResultAdaptor result)
        {
            int count = 0;
            CountTestsByStatus(result, ref count, null);
            return count;
        }

        /// <summary>
        /// Count passed tests
        /// </summary>
        private int CountPassedTests(ITestResultAdaptor result)
        {
            int count = 0;
            CountTestsByStatus(result, ref count, TestStatus.Passed);
            return count;
        }

        /// <summary>
        /// Count failed tests
        /// </summary>
        private int CountFailedTests(ITestResultAdaptor result)
        {
            int count = 0;
            CountTestsByStatus(result, ref count, TestStatus.Failed);
            return count;
        }

        /// <summary>
        /// Count skipped tests
        /// </summary>
        private int CountSkippedTests(ITestResultAdaptor result)
        {
            int count = 0;
            CountTestsByStatus(result, ref count, TestStatus.Skipped);
            return count;
        }

        /// <summary>
        /// Recursively count tests by status
        /// </summary>
        private void CountTestsByStatus(ITestResultAdaptor result, ref int count, TestStatus? targetStatus)
        {
            if (!result.Test.IsSuite)
            {
                if (targetStatus == null || result.TestStatus == targetStatus)
                {
                    count++;
                }
                return;
            }

            if (result.Children != null)
            {
                foreach (ITestResultAdaptor child in result.Children)
                {
                    CountTestsByStatus(child, ref count, targetStatus);
                }
            }
        }
    }

    /// <summary>
    /// Test execution parameters
    /// </summary>
    public class TestExecutionParameters
    {
        public string FilterType { get; }
        public string FilterValue { get; }
        public bool SaveXml { get; }

        public TestExecutionParameters(string filterType, string filterValue, bool saveXml)
        {
            FilterType = filterType;
            FilterValue = filterValue;
            SaveXml = saveXml;
        }
    }

    /// <summary>
    /// Test execution result
    /// </summary>
    public class TestExecutionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public DateTime CompletedAt { get; set; }
        public int TestCount { get; set; }
        public int PassedCount { get; set; }
        public int FailedCount { get; set; }
        public int SkippedCount { get; set; }
        public string XmlPath { get; set; }
    }
} 