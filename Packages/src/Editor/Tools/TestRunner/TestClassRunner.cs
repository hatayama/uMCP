using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// TestClassRunner用のコールバッククラス
    /// </summary>
    internal class TestClassRunnerCallbacks : ICallbacks
    {
        private readonly string className;
        private readonly bool saveXml;
        private int passedCount = 0;
        private int failedCount = 0;
        private int skippedCount = 0;
        
        public TestClassRunnerCallbacks(string className, bool saveXml)
        {
            this.className = className;
            this.saveXml = saveXml;
        }
        
        public void RunStarted(ITestAdaptor testsToRun)
        {
            Debug.Log($"{className}のテスト実行開始したで！");
        }
        
        public void RunFinished(ITestResultAdaptor result)
        {
            Debug.Log($"========== {className} テスト実行完了 ==========");
            Debug.Log($"成功: {passedCount}");
            Debug.Log($"失敗: {failedCount}");
            Debug.Log($"スキップ: {skippedCount}");
            Debug.Log($"合計実行時間: {result.Duration:F3}秒");
            Debug.Log("==========================================");
            
            if (saveXml)
            {
                TestResultXmlExporter.SaveTestResultAsXml(result);
            }
            else
            {
                TestResultXmlExporter.LogTestResultAsXml(result);
            }
        }
        
        public void TestStarted(ITestAdaptor test)
        {
            if (test.FullName.StartsWith(className + "."))
            {
                Debug.Log($"テスト開始: {test.Name}");
            }
        }
        
        public void TestFinished(ITestResultAdaptor result)
        {
            if (result.Test.FullName.StartsWith(className + "."))
            {
                switch (result.TestStatus)
                {
                    case TestStatus.Passed:
                        passedCount++;
                        Debug.Log($"✓ 成功: {result.Test.Name} ({result.Duration:F3}秒)");
                        break;
                    case TestStatus.Failed:
                        failedCount++;
                        Debug.LogError($"✗ 失敗: {result.Test.Name} ({result.Duration:F3}秒)");
                        if (!string.IsNullOrEmpty(result.Message))
                        {
                            Debug.LogError($"失敗理由: {result.Message}");
                        }
                        break;
                    case TestStatus.Skipped:
                        skippedCount++;
                        Debug.Log($"- スキップ: {result.Test.Name}");
                        break;
                }
            }
        }
    }
    
    /// <summary>
    /// 特定のテストクラスを実行するためのヘルパークラス
    /// </summary>
    public static class TestClassRunner
    {
        /// <summary>
        /// 指定したフルクラス名のテストを実行する
        /// </summary>
        /// <param name="fullClassName">フルクラス名（ネームスペース含む）</param>
        /// <param name="saveXml">XMLとして保存するかどうか</param>
        public static void RunTestsByFullClassName(string fullClassName, bool saveXml = false)
        {
            Debug.Log($"{fullClassName}のテストを実行するで！");
            
            // TestRunnerApiのインスタンスを作成
            TestRunnerApi testRunnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();
            
            // まずテスト一覧を取得
            testRunnerApi.RetrieveTestList(TestMode.EditMode, (testRoot) =>
            {
                // クラス名に一致するテストメソッドを探す
                List<string> testFullNames = new List<string>();
                FindTestsByFullClassName(testRoot, fullClassName, testFullNames);
                
                if (testFullNames.Count == 0)
                {
                    Debug.LogWarning($"まさみち、{fullClassName}のテストが見つからへんで...");
                    return;
                }
                
                Debug.Log($"{fullClassName}で{testFullNames.Count}個のテストメソッド見つけたで！");
                foreach (string testName in testFullNames)
                {
                    Debug.Log($"  - {testName}");
                }
                
                // 見つかったテストを実行
                // 新しいTestRunnerApiインスタンスを作成
                TestRunnerApi runnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();
                
                // コールバックを作成
                TestClassRunnerCallbacks callbacks = new TestClassRunnerCallbacks(fullClassName, saveXml);
                runnerApi.RegisterCallbacks(callbacks);
                
                // 複数のテスト名を指定してFilterを作成
                Filter apiFilter = new Filter()
                {
                    testMode = TestMode.EditMode,
                    testNames = testFullNames.ToArray()
                };
                
                // テストを実行
                runnerApi.Execute(new ExecutionSettings(apiFilter));
            });
        }
        
        /// <summary>
        /// テストツリーから指定したフルクラス名のテストを再帰的に探す
        /// </summary>
        private static void FindTestsByFullClassName(ITestAdaptor test, string fullClassName, List<string> foundTests)
        {
            if (test == null) return;
            
            // テストメソッドの場合
            if (!test.IsSuite && test.Method != null)
            {
                // フルクラス名で始まるかチェック（例: Tests.SampleEditModeTest.TestMethod）
                if (test.FullName.StartsWith(fullClassName + "."))
                {
                    foundTests.Add(test.FullName);
                }
            }
            
            // 子要素を再帰的に探索
            if (test.Children != null)
            {
                foreach (ITestAdaptor child in test.Children)
                {
                    FindTestsByFullClassName(child, fullClassName, foundTests);
                }
            }
        }

    }
} 