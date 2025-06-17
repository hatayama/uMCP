using System;
using System.Linq;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Unity Test Runnerの実行を管理するクラス
    /// </summary>
    public class UnityTestExecutionManager : ICallbacks
    {
        private TestRunnerApi testRunnerApi;
        private bool isRunning = false;
        private Action<ITestResultAdaptor> onTestFinished;
        private Action<ITestResultAdaptor> onRunFinished;
        
        public UnityTestExecutionManager()
        {
            // TestRunnerApiはstaticメソッドを使うので、インスタンス作成は不要
        }
        
        /// <summary>
        /// EditModeテストを実行する
        /// </summary>
        public void RunEditModeTests(Action<ITestResultAdaptor> onComplete = null)
        {
            RunEditModeTests(null, onComplete);
        }
        
        /// <summary>
        /// 特定のEditModeテストを実行する
        /// </summary>
        /// <param name="testFilter">テスト実行フィルター（null の場合は全テスト実行）</param>
        /// <param name="onComplete">完了時コールバック</param>
        public void RunEditModeTests(TestExecutionFilter testFilter, Action<ITestResultAdaptor> onComplete = null)
        {
            if (isRunning)
            {
                McpLogger.LogWarning("テスト実行中です");
                return;
            }
            
            isRunning = true;
            onRunFinished = onComplete;
            
            // TestRunnerApiのインスタンスを作成（公式の方法）
            testRunnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();
            
            // コールバックを登録
            testRunnerApi.RegisterCallbacks(this);
            
            // EditModeのフィルターを作成
            Filter filter = new Filter()
            {
                testMode = TestMode.EditMode
            };
            
            // カスタムフィルターを適用
            if (testFilter != null)
            {
                switch (testFilter.FilterType)
                {
                    case TestExecutionFilterType.TestName:
                        filter.testNames = new string[] { testFilter.FilterValue };
                        break;
                    case TestExecutionFilterType.ClassName:
                        // クラス名でフィルタリング - フルネームとして扱う
                        filter.testNames = new string[] { testFilter.FilterValue };
                        break;
                    case TestExecutionFilterType.Namespace:
                        // ネームスペースでのフィルタリング
                        filter.testNames = new string[] { testFilter.FilterValue };
                        break;
                    case TestExecutionFilterType.AssemblyName:
                        filter.assemblyNames = new string[] { testFilter.FilterValue };
                        break;
                }
            }
            
            // テストを実行
            testRunnerApi.Execute(new ExecutionSettings(filter));
        }
        
        // ICallbacks実装
        public void RunStarted(ITestAdaptor testsToRun)
        {
            // テスト開始ログは削除（不要）
        }
        
        public void RunFinished(ITestResultAdaptor result)
        {
            isRunning = false;
            
            // コールバックを解除
            testRunnerApi.UnregisterCallbacks(this);
            
            // 結果をログ出力
            LogTestResults(result);
            
            // 完了コールバックを実行
            onRunFinished?.Invoke(result);
        }
        
        public void TestStarted(ITestAdaptor test)
        {
            // 個別テスト開始ログは不要
        }
        
        public void TestFinished(ITestResultAdaptor result)
        {
            // 個別テスト完了ログは不要
            onTestFinished?.Invoke(result);
        }
        
        /// <summary>
        /// テスト数をカウントする
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
        /// テスト結果をログ出力する
        /// </summary>
        private void LogTestResults(ITestResultAdaptor result)
        {
            int passedCount = 0;
            int failedCount = 0;
            int skippedCount = 0;
            
            CountResults(result, ref passedCount, ref failedCount, ref skippedCount);
            
            McpLogger.LogInfo($"テスト完了 - 成功:{passedCount} 失敗:{failedCount} スキップ:{skippedCount} ({result.Duration:F1}秒)");
        }
        
        /// <summary>
        /// 結果を再帰的にカウントする
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