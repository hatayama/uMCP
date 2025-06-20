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
        public CommandType CommandType => CommandType.RunTests;

        public async Task<object> ExecuteAsync(JToken paramsToken)
        {
            // Parse parameters
            TestExecutionParameters parameters = ParseParameters(paramsToken);
            
            McpLogger.LogInfo($"Test execution started - Filter: {parameters.FilterType}, Value: {parameters.FilterValue}, Save XML: {parameters.SaveXml}");

            try
            {
                // Execute tests on main thread
                await MainThreadSwitcher.SwitchToMainThread();
                
                // Wait for test completion using TaskCompletionSource
                TaskCompletionSource<TestExecutionResult> completionSource = new TaskCompletionSource<TestExecutionResult>();
                
                if (parameters.FilterType == "all")
                {
                    // Execute all tests (using UnityTestExecutionManager)
                    UnityTestExecutionManager testManager = new UnityTestExecutionManager();
                    testManager.RunEditModeTests((result) => {
                        ProcessTestResult(result, parameters.SaveXml, completionSource);
                    });
                }
                else
                {
                    // Execute filtered tests (using UnityTestExecutionManager + TestExecutionFilter)
                    TestExecutionFilter filter = CreateFilter(parameters.FilterType, parameters.FilterValue);
                    UnityTestExecutionManager testManager = new UnityTestExecutionManager();
                    testManager.RunEditModeTests(filter, (result) => {
                        ProcessTestResult(result, parameters.SaveXml, completionSource);
                    });
                }

                // Wait for test completion
                TestExecutionResult executionResult = await completionSource.Task;
                
                return new
                {
                    success = executionResult.Success,
                    message = executionResult.Message,
                    testResults = executionResult.TestResults,
                    xmlPath = executionResult.XmlPath,
                    filterType = parameters.FilterType,
                    filterValue = parameters.FilterValue,
                    saveXml = parameters.SaveXml,
                    completedAt = DateTime.Now.ToString(McpServerConfig.TIMESTAMP_FORMAT)
                };
            }
            catch (Exception ex)
            {
                McpLogger.LogError($"Test execution error: {ex.Message}");
                return new
                {
                    success = false,
                    message = $"Test execution error: {ex.Message}",
                    error = ex.ToString(),
                    completedAt = DateTime.Now.ToString(McpServerConfig.TIMESTAMP_FORMAT)
                };
            }
        }

        /// <summary>
        /// Parse parameters
        /// </summary>
        private TestExecutionParameters ParseParameters(JToken paramsToken)
        {
            TestExecutionParameters parameters = new TestExecutionParameters();

            if (paramsToken != null)
            {
                parameters.FilterType = paramsToken["filterType"]?.ToString() ?? "all";
                parameters.FilterValue = paramsToken["filterValue"]?.ToString() ?? "";
                parameters.SaveXml = paramsToken["saveXml"]?.ToObject<bool>() ?? false;
            }

            return parameters;
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
                // Get test result statistics
                TestResultSummary summary = AnalyzeTestResult(result);
                
                McpLogger.LogInfo($"✅ Test execution completed");
                McpLogger.LogInfo($"📊 Results: Test completed - Passed:{summary.PassedCount} Failed:{summary.FailedCount} Skipped:{summary.SkippedCount} ({result.Duration:F1}s)");
                
                string xmlPath = null;
                if (saveXml)
                {
                    // Save XML
                    xmlPath = NUnitXmlResultExporter.SaveTestResultAsXml(result);
                    if (!string.IsNullOrEmpty(xmlPath))
                    {
                        McpLogger.LogInfo($"📄 XML file saved: {xmlPath}");
                    }
                    else
                    {
                        McpLogger.LogError("Failed to save XML file");
                    }
                }
                else
                {
                    // Output XML to log
                    NUnitXmlResultExporter.LogTestResultAsXml(result);
                    McpLogger.LogInfo("📄 XML output to console");
                }
                
                TestExecutionResult executionResult = new TestExecutionResult
                {
                    Success = summary.FailedCount == 0,
                    Message = $"Test completed - Passed:{summary.PassedCount} Failed:{summary.FailedCount} Skipped:{summary.SkippedCount} ({result.Duration:F1}s)",
                    TestResults = summary,
                    XmlPath = xmlPath
                };
                
                completionSource.SetResult(executionResult);
            }
            catch (Exception ex)
            {
                McpLogger.LogError($"Test result processing error: {ex.Message}");
                completionSource.SetException(ex);
            }
        }

        /// <summary>
        /// Analyze test results
        /// </summary>
        private TestResultSummary AnalyzeTestResult(ITestResultAdaptor result)
        {
            int passedCount = 0;
            int failedCount = 0;
            int skippedCount = 0;
            List<FailedTestInfo> failedTests = new List<FailedTestInfo>();

            CountResults(result, ref passedCount, ref failedCount, ref skippedCount, failedTests);

            return new TestResultSummary
            {
                PassedCount = passedCount,
                FailedCount = failedCount,
                SkippedCount = skippedCount,
                TotalCount = passedCount + failedCount + skippedCount,
                Duration = result.Duration,
                FailedTests = failedTests.ToArray()
            };
        }

        /// <summary>
        /// Recursively count results
        /// </summary>
        private void CountResults(ITestResultAdaptor result, ref int passed, ref int failed, ref int skipped, List<FailedTestInfo> failedTests)
        {
            if (!result.Test.IsSuite)
            {
                switch (result.TestStatus)
                {
                    case TestStatus.Passed:
                        passed++;
                        break;
                    case TestStatus.Failed:
                        failed++;
                        // Record details of failed tests
                        failedTests.Add(new FailedTestInfo
                        {
                            TestName = result.Test.Name,
                            FullName = result.Test.FullName,
                            Message = result.Message,
                            StackTrace = result.StackTrace,
                            Duration = result.Duration
                        });
                        break;
                    case TestStatus.Skipped:
                        skipped++;
                        break;
                }
                return;
            }

            if (result.Children != null)
            {
                foreach (ITestResultAdaptor child in result.Children)
                {
                    CountResults(child, ref passed, ref failed, ref skipped, failedTests);
                }
            }
        }
    }

    /// <summary>
    /// Test execution parameters
    /// </summary>
    public class TestExecutionParameters
    {
        private const string DEFAULT_FILTER_TYPE = "all";
        
        public string FilterType { get; set; } = DEFAULT_FILTER_TYPE;
        public string FilterValue { get; set; } = "";
        public bool SaveXml { get; set; } = false;
    }

    /// <summary>
    /// Test execution result
    /// </summary>
    public class TestExecutionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public TestResultSummary TestResults { get; set; }
        public string XmlPath { get; set; }
    }

    /// <summary>
    /// Test result summary
    /// </summary>
    public class TestResultSummary
    {
        public int PassedCount { get; set; }
        public int FailedCount { get; set; }
        public int SkippedCount { get; set; }
        public int TotalCount { get; set; }
        public double Duration { get; set; }
        public FailedTestInfo[] FailedTests { get; set; }
    }

    /// <summary>
    /// Failed test information
    /// </summary>
    public class FailedTestInfo
    {
        public string TestName { get; set; }
        public string FullName { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }
        public double Duration { get; set; }
    }
} 