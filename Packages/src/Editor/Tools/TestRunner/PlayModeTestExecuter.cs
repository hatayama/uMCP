using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Unified test executor with domain reload control and filtering support
    /// Supports both EditMode and PlayMode tests with advanced filtering capabilities
    /// </summary>
    public static class PlayModeTestExecuter
    {
        /// <summary>
        /// Execute tests asynchronously with domain reload control
        /// </summary>
        /// <param name="testMode">Test mode (EditMode or PlayMode)</param>
        /// <param name="filter">Test execution filter (null for all tests)</param>
        /// <param name="saveXml">Whether to save results as XML</param>
        /// <param name="timeoutSeconds">Timeout in seconds</param>
        /// <returns>Test execution result</returns>
        public static async Task<SerializableTestResult> ExecuteTests(
            TestMode testMode, 
            TestExecutionFilter filter = null, 
            bool saveXml = false, 
            int timeoutSeconds = 60)
        {
            if (testMode == TestMode.PlayMode)
            {
                using DomainReloadDisableScope scope = new DomainReloadDisableScope();
                return await ExecuteTestsWithTimeout(testMode, filter, saveXml, timeoutSeconds);
            }
            else
            {
                return await ExecuteTestsWithTimeout(testMode, filter, saveXml, timeoutSeconds);
            }
        }

        /// <summary>
        /// Execute PlayMode tests asynchronously with domain reload control (backward compatibility)
        /// </summary>
        /// <returns>Test execution result</returns>
        public static async Task<SerializableTestResult> StartPlayModeTests()
        {
            return await ExecuteTests(TestMode.PlayMode);
        }

        /// <summary>
        /// Execute tests with timeout control
        /// </summary>
        private static async Task<SerializableTestResult> ExecuteTestsWithTimeout(
            TestMode testMode, 
            TestExecutionFilter filter, 
            bool saveXml, 
            int timeoutSeconds)
        {
            using CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            
            try
            {
                return await ExecuteTestWithEventNotification(testMode, filter, saveXml, cts.Token);
            }
            catch (OperationCanceledException)
            {
                McpLogger.LogWarning($"{testMode} test execution timed out after {timeoutSeconds} seconds");
                return CreateTimeoutResult(testMode, timeoutSeconds);
            }
        }

        /// <summary>
        /// Execute test with event-based result notification
        /// </summary>
        private static async Task<SerializableTestResult> ExecuteTestWithEventNotification(
            TestMode testMode, 
            TestExecutionFilter filter, 
            bool saveXml, 
            CancellationToken cancellationToken)
        {
            TaskCompletionSource<SerializableTestResult> tcs = new TaskCompletionSource<SerializableTestResult>();
            
            using UnifiedTestCallback callback = new();
            callback.OnTestCompleted += (result, rawResult) =>
            {
                if (!tcs.Task.IsCompleted)
                {
                    // Apply XML export if requested
                    if (saveXml && rawResult != null)
                    {
                        result.xmlPath = ExportResultToXml(rawResult);
                    }
                    tcs.SetResult(result);
                }
            };

            // Register cancellation
            cancellationToken.Register(() =>
            {
                if (!tcs.Task.IsCompleted)
                {
                    tcs.SetCanceled();
                }
            });

            StartTestExecution(testMode, filter, callback);
            return await tcs.Task;
        }

        /// <summary>
        /// Start test execution with callback handler
        /// </summary>
        private static void StartTestExecution(TestMode testMode, TestExecutionFilter filter, UnifiedTestCallback callback)
        {
            TestRunnerApi testRunnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();
            callback.SetTestRunnerApi(testRunnerApi);
            testRunnerApi.RegisterCallbacks(callback);
            
            Filter unityFilter = CreateUnityFilter(testMode, filter);
            testRunnerApi.Execute(new ExecutionSettings(unityFilter));
        }

        /// <summary>
        /// Create Unity Filter from TestExecutionFilter
        /// </summary>
        private static Filter CreateUnityFilter(TestMode testMode, TestExecutionFilter filter)
        {
            Filter unityFilter = new Filter
            {
                testMode = testMode == TestMode.PlayMode 
                    ? UnityEditor.TestTools.TestRunner.Api.TestMode.PlayMode 
                    : UnityEditor.TestTools.TestRunner.Api.TestMode.EditMode
            };

            if (filter != null && filter.FilterType != TestExecutionFilterType.All)
            {
                switch (filter.FilterType)
                {
                    case TestExecutionFilterType.TestName:
                        unityFilter.testNames = new string[] { filter.FilterValue };
                        break;
                    case TestExecutionFilterType.ClassName:
                        unityFilter.testNames = new string[] { filter.FilterValue };
                        break;
                    case TestExecutionFilterType.Namespace:
                        unityFilter.testNames = new string[] { filter.FilterValue };
                        break;
                    case TestExecutionFilterType.AssemblyName:
                        unityFilter.assemblyNames = new string[] { filter.FilterValue };
                        break;
                }
            }

            return unityFilter;
        }

        /// <summary>
        /// Export test result to XML file
        /// </summary>
        private static string ExportResultToXml(ITestResultAdaptor result)
        {
            try
            {
                return NUnitXmlResultExporter.SaveTestResultAsXml(result);
            }
            catch (Exception ex)
            {
                McpLogger.LogError($"Failed to export test result to XML: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Create timeout result
        /// </summary>
        private static SerializableTestResult CreateTimeoutResult(TestMode testMode, int timeoutSeconds)
        {
            return new SerializableTestResult
            {
                success = false,
                message = $"{testMode} test execution timed out after {timeoutSeconds} seconds",
                completedAt = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                testCount = 0,
                passedCount = 0,
                failedCount = 0,
                skippedCount = 0,
                xmlPath = null
            };
        }
    }

    /// <summary>
    /// Unified callback handler for both EditMode and PlayMode test execution
    /// </summary>
    internal class UnifiedTestCallback : ICallbacks, IDisposable
    {
        public event Action<SerializableTestResult, ITestResultAdaptor> OnTestCompleted;

        private TestRunnerApi _testRunnerApi;
        private ITestResultAdaptor _lastResult;

        public void SetTestRunnerApi(TestRunnerApi testRunnerApi)
        {
            _testRunnerApi = testRunnerApi;
        }

        public void RunStarted(ITestAdaptor tests)
        {
            McpLogger.LogInfo($"Test run started: {tests.Name}");
        }

        public void RunFinished(ITestResultAdaptor result)
        {
            _lastResult = result;
            SerializableTestResult serializableResult = SerializableTestResult.FromTestResult(result);
            OnTestCompleted?.Invoke(serializableResult, result);
        }

        public void TestStarted(ITestAdaptor test) { }

        public void TestFinished(ITestResultAdaptor result) { }

        /// <summary>
        /// Get the last test result for XML export
        /// </summary>
        public ITestResultAdaptor GetLastResult()
        {
            return _lastResult;
        }

        public void Dispose()
        {
            OnTestCompleted = null;
            _testRunnerApi?.UnregisterCallbacks(this);
            _lastResult = null;
        }
    }

    /// <summary>
    /// Legacy callback handler for backward compatibility
    /// </summary>
    internal class PlayModeTestCallback : ICallbacks, IDisposable
    {
        public event Action<SerializableTestResult> OnTestCompleted;

        private TestRunnerApi _testRunnerApi;

        public void SetTestRunnerApi(TestRunnerApi testRunnerApi)
        {
            _testRunnerApi = testRunnerApi;
        }

        public void RunStarted(ITestAdaptor tests)
        {
            McpLogger.LogInfo("PlayMode test run started");
        }

        public void RunFinished(ITestResultAdaptor result)
        {
            SerializableTestResult serializableResult = SerializableTestResult.FromTestResult(result);
            OnTestCompleted?.Invoke(serializableResult);
        }

        public void TestStarted(ITestAdaptor test) { }

        public void TestFinished(ITestResultAdaptor result) { }

        public void Dispose()
        {
            OnTestCompleted = null;
            _testRunnerApi?.UnregisterCallbacks(this);
        }
    }
}