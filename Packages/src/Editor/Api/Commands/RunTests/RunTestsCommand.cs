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
            // Type-safe parameter access - no more string parsing!
            
            McpLogger.LogInfo($"Test execution started - Filter: {parameters.FilterType}, Value: {parameters.FilterValue}, Save XML: {parameters.SaveXml}, Timeout: {parameters.TimeoutSeconds}s");

            try
            {
                McpLogger.LogInfo("Creating test execution manager...");
                // Create test execution manager
                UnityTestExecutionManager testManager = new UnityTestExecutionManager();
                
                // Create completion source for async operation
                TaskCompletionSource<TestExecutionResult> completionSource = new TaskCompletionSource<TestExecutionResult>();
                
                // Set up result processing
                void ProcessResult(ITestResultAdaptor result)
                {
                    McpLogger.LogInfo("Test execution completed, processing results...");
                    ProcessTestResult(result, parameters.SaveXml, completionSource);
                }
                
                McpLogger.LogInfo("Starting test execution...");
                // Execute tests based on filter
                if (string.IsNullOrEmpty(parameters.FilterValue))
                {
                    McpLogger.LogInfo("Running all EditMode tests...");
                    testManager.RunEditModeTests(ProcessResult);
                }
                else
                {
                    McpLogger.LogInfo($"Running filtered tests: {parameters.FilterType} = {parameters.FilterValue}");
                    TestExecutionFilter filter = CreateFilter(parameters.FilterType.ToString(), parameters.FilterValue);
                    testManager.RunEditModeTests(filter, ProcessResult);
                }
                
                McpLogger.LogInfo("Test execution initiated, waiting for completion...");
                
                // Wait for completion with timeout
                using (var timeoutCts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(parameters.TimeoutSeconds)))
                {
                    var timeoutTask = Task.Delay(TimeSpan.FromSeconds(parameters.TimeoutSeconds), timeoutCts.Token);
                    var completedTask = await Task.WhenAny(completionSource.Task, timeoutTask);
                    
                    if (completedTask == timeoutTask)
                    {
                        McpLogger.LogWarning($"Test execution timed out after {parameters.TimeoutSeconds} seconds");
                        return CreateTimeoutResponse(parameters.TimeoutSeconds);
                    }
                    
                    TestExecutionResult result = await completionSource.Task;
                    McpLogger.LogInfo($"Test execution completed: Success={result.Success}, Message={result.Message}");
                    
                    // Create type-safe response
                    return new RunTestsResponse(
                        success: result.Success,
                        message: result.Message,
                        completedAt: result.CompletedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                        testCount: result.TestCount,
                        passedCount: result.PassedCount,
                        failedCount: result.FailedCount,
                        skippedCount: result.SkippedCount,
                        xmlPath: result.XmlPath
                    );
                }
            }
            catch (Exception ex)
            {
                McpLogger.LogError($"Test execution failed: {ex.Message}");
                throw;
            }
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

        /// <summary>
        /// Create timeout response
        /// </summary>
        private RunTestsResponse CreateTimeoutResponse(int timeoutSeconds)
        {
            return new RunTestsResponse(
                success: false,
                message: $"Test execution timed out after {timeoutSeconds} seconds",
                completedAt: DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                testCount: 0,
                passedCount: 0,
                failedCount: 0,
                skippedCount: 0,
                xmlPath: null
            );
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