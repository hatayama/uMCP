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
    /// テスト実行コマンドハンドラー
    /// Unity Test Runnerを使用してテストを実行し、結果を返す
    /// </summary>
    public class RunTestsCommand : IUnityCommand
    {
        public CommandType CommandType => CommandType.RunTests;

        public async Task<object> ExecuteAsync(JToken paramsToken)
        {
            // パラメータを解析
            TestExecutionParameters parameters = ParseParameters(paramsToken);
            
            McpLogger.LogInfo($"テスト実行開始 - フィルター: {parameters.FilterType}, 値: {parameters.FilterValue}, XML保存: {parameters.SaveXml}");

            try
            {
                // メインスレッドでテスト実行
                await MainThreadSwitcher.SwitchToMainThread();
                
                // TaskCompletionSourceを使ってテスト完了まで待機
                TaskCompletionSource<TestExecutionResult> completionSource = new TaskCompletionSource<TestExecutionResult>();
                
                if (parameters.FilterType == "all")
                {
                    // 全テスト実行 (UnityTestExecutionManager使用)
                    UnityTestExecutionManager testManager = new UnityTestExecutionManager();
                    testManager.RunEditModeTests((result) => {
                        ProcessTestResult(result, parameters.SaveXml, completionSource);
                    });
                }
                else
                {
                    // フィルター付きテスト実行 (UnityTestExecutionManager + TestExecutionFilter使用)
                    TestExecutionFilter filter = CreateFilter(parameters.FilterType, parameters.FilterValue);
                    UnityTestExecutionManager testManager = new UnityTestExecutionManager();
                    testManager.RunEditModeTests(filter, (result) => {
                        ProcessTestResult(result, parameters.SaveXml, completionSource);
                    });
                }

                // テスト完了まで待機
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
                McpLogger.LogError($"テスト実行エラー: {ex.Message}");
                return new
                {
                    success = false,
                    message = $"テスト実行エラー: {ex.Message}",
                    error = ex.ToString(),
                    completedAt = DateTime.Now.ToString(McpServerConfig.TIMESTAMP_FORMAT)
                };
            }
        }

        /// <summary>
        /// パラメータを解析する
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
        /// テスト実行フィルターを作成する
        /// </summary>
        private TestExecutionFilter CreateFilter(string filterType, string filterValue)
        {
            return filterType.ToLower() switch
            {
                "fullclassname" => TestExecutionFilter.ByClassName(filterValue), // フルクラス名 (例: io.github.hatayama.uMCP.CompileCommandTests)
                "namespace" => TestExecutionFilter.ByNamespace(filterValue),      // ネームスペース (例: io.github.hatayama.uMCP)
                "testname" => TestExecutionFilter.ByTestName(filterValue),        // 個別テスト名
                "assembly" => TestExecutionFilter.ByAssemblyName(filterValue),    // アセンブリ名
                _ => throw new ArgumentException($"未対応のフィルタータイプ: {filterType}")
            };
        }

        /// <summary>
        /// テスト結果を処理する
        /// </summary>
        private void ProcessTestResult(ITestResultAdaptor result, bool saveXml, TaskCompletionSource<TestExecutionResult> completionSource)
        {
            try
            {
                // テスト結果の統計を取得
                TestResultSummary summary = AnalyzeTestResult(result);
                
                McpLogger.LogInfo($"✅ テスト実行完了");
                McpLogger.LogInfo($"📊 結果: テスト完了 - 成功:{summary.PassedCount} 失敗:{summary.FailedCount} スキップ:{summary.SkippedCount} ({result.Duration:F1}秒)");
                
                string xmlPath = null;
                if (saveXml)
                {
                    // XML保存
                    xmlPath = NUnitXmlResultExporter.SaveTestResultAsXml(result);
                    if (!string.IsNullOrEmpty(xmlPath))
                    {
                        McpLogger.LogInfo($"📄 XMLファイル保存: {xmlPath}");
                    }
                    else
                    {
                        McpLogger.LogError("XMLファイルの保存に失敗しました");
                    }
                }
                else
                {
                    // XMLをログ出力
                    NUnitXmlResultExporter.LogTestResultAsXml(result);
                    McpLogger.LogInfo("📄 XMLをコンソールに出力しました");
                }
                
                TestExecutionResult executionResult = new TestExecutionResult
                {
                    Success = summary.FailedCount == 0,
                    Message = $"テスト完了 - 成功:{summary.PassedCount} 失敗:{summary.FailedCount} スキップ:{summary.SkippedCount} ({result.Duration:F1}秒)",
                    TestResults = summary,
                    XmlPath = xmlPath
                };
                
                completionSource.SetResult(executionResult);
            }
            catch (Exception ex)
            {
                McpLogger.LogError($"テスト結果処理エラー: {ex.Message}");
                completionSource.SetException(ex);
            }
        }

        /// <summary>
        /// テスト結果を分析する
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
        /// 結果を再帰的にカウントする
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
                        // 失敗したテストの詳細を記録
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
    /// テスト実行パラメータ
    /// </summary>
    public class TestExecutionParameters
    {
        private const string DEFAULT_FILTER_TYPE = "all";
        
        public string FilterType { get; set; } = DEFAULT_FILTER_TYPE;
        public string FilterValue { get; set; } = "";
        public bool SaveXml { get; set; } = false;
    }

    /// <summary>
    /// テスト実行結果
    /// </summary>
    public class TestExecutionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public TestResultSummary TestResults { get; set; }
        public string XmlPath { get; set; }
    }

    /// <summary>
    /// テスト結果サマリー
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
    /// 失敗したテスト情報
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