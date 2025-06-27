using System;
using System.Threading.Tasks;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// PlayMode test executor with domain reload control
    /// </summary>
    public static class PlayModeTestExecuter
    {
        /// <summary>
        /// Execute PlayMode tests asynchronously with domain reload control
        /// </summary>
        /// <returns>Test execution result</returns>
        public static async Task<SerializableTestResult> StartPlayModeTests()
        {
            using DomainReloadDisableScope scope = new DomainReloadDisableScope();
            // await EditorDelay.DelayFrame(1);
            return await ExecuteTestWithEventNotification();
        }

        /// <summary>
        /// Execute test with event-based result notification
        /// </summary>
        private static async Task<SerializableTestResult> ExecuteTestWithEventNotification()
        {
            TaskCompletionSource<SerializableTestResult> tcs = new TaskCompletionSource<SerializableTestResult>();
            using PlayModeTestCallback callback = new();
            callback.OnTestCompleted += (result) =>
            {
                if (!tcs.Task.IsCompleted)
                {
                    tcs.SetResult(result);
                }
            };

            StartTestExecution(callback);
            return await tcs.Task;
        }

        /// <summary>
        /// Start test execution with callback handler
        /// </summary>
        private static void StartTestExecution(PlayModeTestCallback callback)
        {
            TestRunnerApi testRunnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();
            callback.SetTestRunnerApi(testRunnerApi);
            testRunnerApi.RegisterCallbacks(callback);
            Filter filter = new Filter
            {
                testMode = UnityEditor.TestTools.TestRunner.Api.TestMode.PlayMode
            };

            testRunnerApi.Execute(new ExecutionSettings(filter));
        }
    }

    /// <summary>
    /// Callback handler for PlayMode test execution
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