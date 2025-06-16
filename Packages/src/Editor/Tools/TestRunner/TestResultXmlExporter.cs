using System;
using System.IO;
using System.Text;
using System.Xml;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// テスト結果をXML形式でエクスポートするクラス
    /// </summary>
    public static class TestResultXmlExporter
    {
        /// <summary>
        /// テスト結果のXMLをログ出力する
        /// </summary>
        public static void LogTestResultAsXml(ITestResultAdaptor testResult)
        {
            try
            {
                string xmlContent = GenerateNUnitXml(testResult);
                Debug.Log("========== テスト結果XML ==========");
                Debug.Log(xmlContent);
                Debug.Log("==================================");
            }
            catch (Exception ex)
            {
                Debug.LogError($"XML生成に失敗したわ...: {ex.Message}");
            }
        }
        
        /// <summary>
        /// テスト結果をXMLファイルとして保存する
        /// </summary>
        public static string SaveTestResultAsXml(ITestResultAdaptor testResult)
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string fileName = $"TestResults_{timestamp}.xml";
            string filePath = Path.Combine(Application.dataPath, fileName);
            
            try
            {
                string xmlContent = GenerateNUnitXml(testResult);
                File.WriteAllText(filePath, xmlContent, Encoding.UTF8);
                
                // Assetsフォルダをリフレッシュ
                AssetDatabase.Refresh();
                
                Debug.Log($"まさみち、テスト結果をXMLに保存したで！\nパス: {filePath}");
                return filePath;
            }
            catch (Exception ex)
            {
                Debug.LogError($"XMLファイルの保存に失敗したわ...: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// NUnit形式のXMLを生成する
        /// </summary>
        private static string GenerateNUnitXml(ITestResultAdaptor testResult)
        {
            XmlDocument doc = new XmlDocument();
            
            // XML宣言を追加
            XmlDeclaration declaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            doc.AppendChild(declaration);
            
            // ルート要素を作成
            XmlElement testRun = doc.CreateElement("test-run");
            testRun.SetAttribute("id", "2");
            testRun.SetAttribute("testcasecount", CountTestCases(testResult).ToString());
            testRun.SetAttribute("result", GetOverallResult(testResult));
            testRun.SetAttribute("total", CountTestCases(testResult).ToString());
            testRun.SetAttribute("passed", CountPassed(testResult).ToString());
            testRun.SetAttribute("failed", CountFailed(testResult).ToString());
            testRun.SetAttribute("skipped", CountSkipped(testResult).ToString());
            testRun.SetAttribute("start-time", testResult.StartTime.ToString("yyyy-MM-dd HH:mm:ss"));
            testRun.SetAttribute("end-time", testResult.EndTime.ToString("yyyy-MM-dd HH:mm:ss"));
            testRun.SetAttribute("duration", testResult.Duration.ToString("F3"));
            doc.AppendChild(testRun);
            
            // テストスイートを追加
            XmlElement testSuite = CreateTestSuiteElement(doc, testResult);
            testRun.AppendChild(testSuite);
            
            // フォーマット済みのXMLを返す
            using (StringWriter stringWriter = new StringWriter())
            {
                XmlWriterSettings settings = new XmlWriterSettings
                {
                    Indent = true,
                    IndentChars = "  ",
                    NewLineChars = "\n",
                    NewLineHandling = NewLineHandling.Replace
                };
                
                using (XmlWriter xmlWriter = XmlWriter.Create(stringWriter, settings))
                {
                    doc.Save(xmlWriter);
                }
                
                return stringWriter.ToString();
            }
        }
        
        /// <summary>
        /// テストスイート要素を作成する（再帰的）
        /// </summary>
        private static XmlElement CreateTestSuiteElement(XmlDocument doc, ITestResultAdaptor result)
        {
            if (result.Test.IsSuite)
            {
                XmlElement suite = doc.CreateElement("test-suite");
                suite.SetAttribute("type", result.Test.TypeInfo?.FullName ?? "TestSuite");
                suite.SetAttribute("name", result.Test.Name);
                suite.SetAttribute("fullname", result.Test.FullName);
                suite.SetAttribute("result", result.TestStatus.ToString());
                suite.SetAttribute("start-time", result.StartTime.ToString("yyyy-MM-dd HH:mm:ss"));
                suite.SetAttribute("end-time", result.EndTime.ToString("yyyy-MM-dd HH:mm:ss"));
                suite.SetAttribute("duration", result.Duration.ToString("F3"));
                suite.SetAttribute("total", CountTestCases(result).ToString());
                suite.SetAttribute("passed", CountPassed(result).ToString());
                suite.SetAttribute("failed", CountFailed(result).ToString());
                suite.SetAttribute("skipped", CountSkipped(result).ToString());
                
                // 子要素を追加
                if (result.Children != null)
                {
                    foreach (ITestResultAdaptor child in result.Children)
                    {
                        XmlElement childElement = CreateTestSuiteElement(doc, child);
                        suite.AppendChild(childElement);
                    }
                }
                
                return suite;
            }
            else
            {
                // テストケース
                XmlElement testCase = doc.CreateElement("test-case");
                testCase.SetAttribute("name", result.Test.Name);
                testCase.SetAttribute("fullname", result.Test.FullName);
                testCase.SetAttribute("result", result.TestStatus.ToString());
                testCase.SetAttribute("start-time", result.StartTime.ToString("yyyy-MM-dd HH:mm:ss"));
                testCase.SetAttribute("end-time", result.EndTime.ToString("yyyy-MM-dd HH:mm:ss"));
                testCase.SetAttribute("duration", result.Duration.ToString("F3"));
                
                // 失敗の場合はメッセージとスタックトレースを追加
                if (result.TestStatus == TestStatus.Failed)
                {
                    XmlElement failure = doc.CreateElement("failure");
                    
                    if (!string.IsNullOrEmpty(result.Message))
                    {
                        XmlElement message = doc.CreateElement("message");
                        message.InnerText = result.Message;
                        failure.AppendChild(message);
                    }
                    
                    if (!string.IsNullOrEmpty(result.StackTrace))
                    {
                        XmlElement stackTrace = doc.CreateElement("stack-trace");
                        stackTrace.InnerText = result.StackTrace;
                        failure.AppendChild(stackTrace);
                    }
                    
                    testCase.AppendChild(failure);
                }
                
                return testCase;
            }
        }
        
        /// <summary>
        /// 全体の結果を取得する
        /// </summary>
        private static string GetOverallResult(ITestResultAdaptor result)
        {
            if (CountFailed(result) > 0)
                return "Failed";
            if (CountSkipped(result) > 0 && CountPassed(result) == 0)
                return "Skipped";
            return "Passed";
        }
        
        /// <summary>
        /// テストケース数をカウントする
        /// </summary>
        private static int CountTestCases(ITestResultAdaptor result)
        {
            if (!result.Test.IsSuite)
                return 1;
            
            int count = 0;
            if (result.Children != null)
            {
                foreach (ITestResultAdaptor child in result.Children)
                {
                    count += CountTestCases(child);
                }
            }
            return count;
        }
        
        /// <summary>
        /// 成功したテスト数をカウントする
        /// </summary>
        private static int CountPassed(ITestResultAdaptor result)
        {
            if (!result.Test.IsSuite)
                return result.TestStatus == TestStatus.Passed ? 1 : 0;
            
            int count = 0;
            if (result.Children != null)
            {
                foreach (ITestResultAdaptor child in result.Children)
                {
                    count += CountPassed(child);
                }
            }
            return count;
        }
        
        /// <summary>
        /// 失敗したテスト数をカウントする
        /// </summary>
        private static int CountFailed(ITestResultAdaptor result)
        {
            if (!result.Test.IsSuite)
                return result.TestStatus == TestStatus.Failed ? 1 : 0;
            
            int count = 0;
            if (result.Children != null)
            {
                foreach (ITestResultAdaptor child in result.Children)
                {
                    count += CountFailed(child);
                }
            }
            return count;
        }
        
        /// <summary>
        /// スキップされたテスト数をカウントする
        /// </summary>
        private static int CountSkipped(ITestResultAdaptor result)
        {
            if (!result.Test.IsSuite)
                return result.TestStatus == TestStatus.Skipped ? 1 : 0;
            
            int count = 0;
            if (result.Children != null)
            {
                foreach (ITestResultAdaptor child in result.Children)
                {
                    count += CountSkipped(child);
                }
            }
            return count;
        }
    }
} 