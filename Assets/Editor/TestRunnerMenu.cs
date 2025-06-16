using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Test Runner関連のメニューアイテムを提供するクラス
    /// </summary>
    public static class TestRunnerMenu
    {
        private static UnityTestExecutionManager testRunnerController;
        private static bool shouldSaveXml = true;
        
        [MenuItem("uMCP/Test Runner/Run EditMode Tests (Save XML)")]
        public static void RunEditModeTestsAndSaveXml()
        {
            Debug.Log("まさみち、EditModeテストを実行してXMLに保存するで！");
            
            shouldSaveXml = true;
            testRunnerController = new UnityTestExecutionManager();
            testRunnerController.RunEditModeTests(OnTestRunComplete);
        }
        
        [MenuItem("uMCP/Test Runner/Run EditMode Tests (Log XML)")]
        public static void RunEditModeTestsAndLogXml()
        {
            Debug.Log("まさみち、EditModeテストを実行してXMLをログ出力するで！");
            
            shouldSaveXml = false;
            testRunnerController = new UnityTestExecutionManager();
            testRunnerController.RunEditModeTests(OnTestRunComplete);
        }
        
        [MenuItem("uMCP/Test Runner/Open Test Runner Window")]
        public static void OpenTestRunnerWindow()
        {
            // UnityのTest Runner Windowを開く
            EditorApplication.ExecuteMenuItem("Window/General/Test Runner");
            
            Debug.Log("Test Runner Window開いたで！");
        }
        
        // ===== 特定のテストクラスを実行するメニュー =====
        
        [MenuItem("uMCP/Test Runner/Run Specific Test/CompileCommandTests")]
        public static void RunCompileCommandTests()
        {
            Debug.Log("CompileCommandTestsだけ実行するで！");
            shouldSaveXml = false;
            testRunnerController = new UnityTestExecutionManager();
            // io.github.hatayama.uMCP ネームスペースのテスト
            TestExecutionFilter filter = TestExecutionFilter.ByClassName("io.github.hatayama.uMCP.CompileCommandTests");
            testRunnerController.RunEditModeTests(filter, OnTestRunComplete);
        }
        
        [MenuItem("uMCP/Test Runner/Run Specific Test/GetLogsCommandTests")]
        public static void RunGetLogsCommandTests()
        {
            Debug.Log("GetLogsCommandTestsだけ実行するで！");
            shouldSaveXml = false;
            testRunnerController = new UnityTestExecutionManager();
            TestExecutionFilter filter = TestExecutionFilter.ByClassName("io.github.hatayama.uMCP.GetLogsCommandTests");
            testRunnerController.RunEditModeTests(filter, OnTestRunComplete);
        }
        
        [MenuItem("uMCP/Test Runner/Run Specific Test/MainThreadSwitcherTests")]
        public static void RunMainThreadSwitcherTests()
        {
            Debug.Log("MainThreadSwitcherTestsだけ実行するで！");
            shouldSaveXml = false;
            testRunnerController = new UnityTestExecutionManager();
            TestExecutionFilter filter = TestExecutionFilter.ByClassName("io.github.hatayama.uMCP.MainThreadSwitcherTests");
            testRunnerController.RunEditModeTests(filter, OnTestRunComplete);
        }
        
        [MenuItem("uMCP/Test Runner/Run Specific Test/SampleEditModeTest")]
        public static void RunSampleEditModeTest()
        {
            Debug.Log("SampleEditModeTestだけ実行するで！");
            shouldSaveXml = false;
            testRunnerController = new UnityTestExecutionManager();
            TestExecutionFilter filter = TestExecutionFilter.ByClassName("Tests.SampleEditModeTest");
            testRunnerController.RunEditModeTests(filter, OnTestRunComplete);
        }
        
        /// <summary>
        /// テスト実行完了時のコールバック
        /// </summary>
        private static void OnTestRunComplete(ITestResultAdaptor result)
        {
            Debug.Log("まさみち、テスト実行完了したで！");
            
            if (shouldSaveXml)
            {
                // XML保存処理を呼び出す
                string savedPath = NUnitXmlResultExporter.SaveTestResultAsXml(result);
                
                if (!string.IsNullOrEmpty(savedPath))
                {
                    Debug.Log($"XMLファイルが正常に保存されたで！\nパス: {savedPath}");
                    
                    // ファイルをProjectビューで選択
                    UnityEngine.Object xmlAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                        savedPath.Replace(Application.dataPath, "Assets"));
                    if (xmlAsset != null)
                    {
                        EditorGUIUtility.PingObject(xmlAsset);
                        Selection.activeObject = xmlAsset;
                    }
                }
                else
                {
                    Debug.LogError("XMLファイルの保存に失敗したわ...");
                }
            }
            else
            {
                // XMLをログ出力
                NUnitXmlResultExporter.LogTestResultAsXml(result);
                Debug.Log("XMLをコンソールに出力したで！上のログを確認してや〜");
            }
        }
    }
} 