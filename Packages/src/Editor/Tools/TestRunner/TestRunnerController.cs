using System;
using System.Linq;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Unity Test Runnerの実行を制御するクラス
    /// </summary>
    public class TestRunnerController : ICallbacks
    {
        private TestRunnerApi testRunnerApi;
        private bool isRunning = false;
        private Action<ITestResultAdaptor> onTestFinished;
        private Action<ITestResultAdaptor> onRunFinished;
        
        public TestRunnerController()
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
        /// <param name="testFilter">テストフィルター（null の場合は全テスト実行）</param>
        /// <param name="onComplete">完了時コールバック</param>
        public void RunEditModeTests(TestFilter testFilter, Action<ITestResultAdaptor> onComplete = null)
        {
            if (isRunning)
            {
                Debug.LogWarning("まさみち、もうテスト実行中やで！");
                return;
            }
            
            if (testFilter != null)
            {
                Debug.Log($"EditModeテストを開始するで〜 (フィルター: {testFilter.FilterType} = {testFilter.FilterValue})");
            }
            else
            {
                Debug.Log("EditModeテストを開始するで〜（全テスト）");
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
                    case TestFilterType.TestName:
                        filter.testNames = new string[] { testFilter.FilterValue };
                        break;
                    case TestFilterType.ClassName:
                        // クラス名でフィルタリング - フルネームとして扱う
                        filter.testNames = new string[] { testFilter.FilterValue };
                        break;
                    case TestFilterType.Namespace:
                        // ネームスペースでのフィルタリング
                        filter.testNames = new string[] { testFilter.FilterValue };
                        break;
                    case TestFilterType.AssemblyName:
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
            int testCount = CountTests(testsToRun);
            Debug.Log($"まさみち、{testCount}個のテストを実行するで！");
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
            Debug.Log($"テスト開始: {test.FullName}");
        }
        
        public void TestFinished(ITestResultAdaptor result)
        {
            string status = result.TestStatus switch
            {
                TestStatus.Passed => "✓ 成功",
                TestStatus.Failed => "✗ 失敗",
                TestStatus.Skipped => "- スキップ",
                _ => "? 不明"
            };
            
            Debug.Log($"{status}: {result.Test.FullName} ({result.Duration:F3}秒)");
            
            if (result.TestStatus == TestStatus.Failed)
            {
                Debug.LogError($"失敗理由: {result.Message}");
                if (!string.IsNullOrEmpty(result.StackTrace))
                {
                    Debug.LogError($"スタックトレース:\n{result.StackTrace}");
                }
            }
            
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
            
            Debug.Log("========== テスト実行完了 ==========");
            Debug.Log($"成功: {passedCount}");
            Debug.Log($"失敗: {failedCount}");
            Debug.Log($"スキップ: {skippedCount}");
            Debug.Log($"合計実行時間: {result.Duration:F3}秒");
            Debug.Log("==================================");
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