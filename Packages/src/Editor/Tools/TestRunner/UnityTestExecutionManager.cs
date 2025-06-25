using System;
using System.Linq;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Class to manage the execution of the Unity Test Runner.
    /// </summary>
    public class UnityTestExecutionManager : ICallbacks
    {
        private TestRunnerApi testRunnerApi;
        private bool isRunning = false;
        private Action<ITestResultAdaptor> onTestFinished;
        private Action<ITestResultAdaptor> onRunFinished;
        
        public UnityTestExecutionManager()
        {
            // No instance creation is necessary as TestRunnerApi uses static methods.
        }
        
        /// <summary>
        /// Executes EditMode tests.
        /// </summary>
        public void RunEditModeTests(Action<ITestResultAdaptor> onComplete = null)
        {
            RunEditModeTests(null, onComplete);
        }
        
        /// <summary>
        /// Executes specific EditMode tests.
        /// </summary>
        /// <param name="testFilter">Test execution filter (if null, all tests are run).</param>
        /// <param name="onComplete">Callback on completion.</param>
        public void RunEditModeTests(TestExecutionFilter testFilter, Action<ITestResultAdaptor> onComplete = null)
        {
            if (isRunning)
            {
                McpLogger.LogWarning("Tests are already running.");
                return;
            }
            
            isRunning = true;
            onRunFinished = onComplete;
            
            // Create an instance of TestRunnerApi (official method).
            testRunnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();
            
            // Register callbacks.
            testRunnerApi.RegisterCallbacks(this);
            
            // Create a filter for EditMode.
            Filter filter = new Filter()
            {
                testMode = TestMode.EditMode
            };
            
            // Apply custom filter.
            if (testFilter != null)
            {
                switch (testFilter.FilterType)
                {
                    case TestExecutionFilterType.TestName:
                        filter.testNames = new string[] { testFilter.FilterValue };
                        break;
                    case TestExecutionFilterType.ClassName:
                        // Filter by class name - treated as a full name.
                        filter.testNames = new string[] { testFilter.FilterValue };
                        break;
                    case TestExecutionFilterType.Namespace:
                        // Filtering by namespace.
                        filter.testNames = new string[] { testFilter.FilterValue };
                        break;
                    case TestExecutionFilterType.AssemblyName:
                        filter.assemblyNames = new string[] { testFilter.FilterValue };
                        break;
                }
            }
            
            // Execute tests.
            testRunnerApi.Execute(new ExecutionSettings(filter));
        }
        
        // ICallbacks implementation.
        public void RunStarted(ITestAdaptor testsToRun)
        {
            // Test start log removed (unnecessary).
        }
        
        public void RunFinished(ITestResultAdaptor result)
        {
            isRunning = false;
            
            // Unregister callbacks.
            testRunnerApi.UnregisterCallbacks(this);
            
            // Log the results.
            LogTestResults(result);
            
            // Execute completion callback.
            onRunFinished?.Invoke(result);
        }
        
        public void TestStarted(ITestAdaptor test)
        {
            // Individual test start log is unnecessary.
        }
        
        public void TestFinished(ITestResultAdaptor result)
        {
            // Individual test completion log is unnecessary.
            onTestFinished?.Invoke(result);
        }
        
        /// <summary>
        /// Counts the number of tests.
        /// </summary>
        private int CountTests(ITestAdaptor test)
        {
            if (!test.IsSuite)
                return 1;
            
            int count = 0;
            foreach (ITestAdaptor child in test.Children)
            {
                count += CountTests(child);
            }
            return count;
        }
        
        /// <summary>
        /// Logs the test results.
        /// </summary>
        private void LogTestResults(ITestResultAdaptor result)
        {
            int passedCount = 0;
            int failedCount = 0;
            int skippedCount = 0;
            
            CountResults(result, ref passedCount, ref failedCount, ref skippedCount);
            
            // Test complete
        }
        
        /// <summary>
        /// Recursively counts the results.
        /// </summary>
        private void CountResults(ITestResultAdaptor result, ref int passed, ref int failed, ref int skipped)
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
                    CountResults(child, ref passed, ref failed, ref skipped);
                }
            }
        }
    }
} 