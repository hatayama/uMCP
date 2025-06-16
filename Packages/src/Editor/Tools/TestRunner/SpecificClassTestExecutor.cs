using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// SpecificClassTestExecutor用のコールバッククラス
    /// </summary>
    internal class SpecificClassTestCallbacks : ICallbacks
    {
        private readonly string className;
        private readonly bool saveXml;
        private int passedCount = 0;
        private int failedCount = 0;
        private int skippedCount = 0;
        
        public SpecificClassTestCallbacks(string className, bool saveXml)
        {
            this.className = className;
            this.saveXml = saveXml;
        }
        
        public void RunStarted(ITestAdaptor testsToRun)
        {
            // テスト開始ログは不要
        }
        
        public void RunFinished(ITestResultAdaptor result)
        {
            McpLogger.LogInfo($"{className} - 成功:{passedCount} 失敗:{failedCount} スキップ:{skippedCount} ({result.Duration:F1}秒)");
            
            if (saveXml)
            {
                NUnitXmlResultExporter.SaveTestResultAsXml(result);
            }
            else
            {
                NUnitXmlResultExporter.LogTestResultAsXml(result);
            }
        }
        
        public void TestStarted(ITestAdaptor test)
        {
            // 個別テスト開始ログは不要
        }
        
        public void TestFinished(ITestResultAdaptor result)
        {
            if (result.Test.FullName.StartsWith(className + "."))
            {
                switch (result.TestStatus)
                {
                    case TestStatus.Passed:
                        passedCount++;
                        break;
                    case TestStatus.Failed:
                        failedCount++;
                        McpLogger.LogError($"テスト失敗: {result.Test.Name} - {result.Message}");
                        break;
                    case TestStatus.Skipped:
                        skippedCount++;
                        break;
                }
            }
        }
    }
    
    /// <summary>
    /// 特定のテストクラスを実行するためのヘルパークラス
    /// </summary>
    public static class SpecificClassTestExecutor
    {
        /// <summary>
        /// 指定したフルクラス名のテストを実行する
        /// </summary>
        /// <param name="fullClassName">フルクラス名（ネームスペース含む）</param>
        /// <param name="saveXml">XMLとして保存するかどうか</param>
        public static void RunTestsByFullClassName(string fullClassName, bool saveXml = false)
        {
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
                    McpLogger.LogWarning($"{fullClassName}のテストが見つかりません");
                    return;
                }
                
                // 見つかったテストを実行
                // 新しいTestRunnerApiインスタンスを作成
                TestRunnerApi runnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();
                
                // コールバックを作成
                SpecificClassTestCallbacks callbacks = new SpecificClassTestCallbacks(fullClassName, saveXml);
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