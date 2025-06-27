using System;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace io.github.hatayama.uMCP
{
    public class PlayModeTestExecuter : ScriptableSingleton<PlayModeTestExecuter>, ICallbacks
    {
        [SerializeField] private bool _isRunning;
        private ITestResultAdaptor _result;
        
        public event Action<ITestAdaptor> OnRunStarted;
        public event Action<ITestResultAdaptor> OnRunFinished;
        
        public async Task<ITestResultAdaptor> RunPlayModeTests()
        {
            if (_isRunning)
            {
                McpLogger.LogWarning("Tests are already running.");
                return;
            }
            
            TestRunnerApi testRunnerApi = CreateInstance<TestRunnerApi>();
            Filter filter = new Filter
            {
                testMode = UnityEditor.TestTools.TestRunner.Api.TestMode.PlayMode
            };
            testRunnerApi.Execute(new ExecutionSettings(filter));
        }
 
        private void OnEnable()
        {
            Debug.Log($"UTEST】OnEnable");
            var api = CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(this);
        }

        private void OnDisable()
        {
            Debug.Log($"UTEST】OnDisable");
            var api = CreateInstance<TestRunnerApi>();
            api.UnregisterCallbacks(this);
        }

        public void RunStarted(ITestAdaptor tests)
        {
            _isRunning = true;
            Debug.Log($"UTEST】RunStarted: {OnRunStarted}");
            OnRunStarted?.Invoke(tests);
        }
        
        public void RunFinished(ITestResultAdaptor result)
        {
            _result  = result;
            _isRunning = false;
            var api = CreateInstance<TestRunnerApi>();
            api.UnregisterCallbacks(this);
            OnRunFinished?.Invoke(result);
            Debug.Log($"UTEST】RunFinished: {OnRunFinished}");
        }

        public void TestStarted(ITestAdaptor test) {}
        public void TestFinished(ITestResultAdaptor result) {}
    }
}