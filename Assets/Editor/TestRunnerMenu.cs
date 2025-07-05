using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Class that provides menu items related to the Test Runner.
    /// </summary>
    public static class TestRunnerMenu
    {
        [MenuItem("uMCP/Tools/Test Runner/Run EditMode Tests/All Tests (Save XML)")]
        public static async void RunEditModeTestsAndSaveXml()
        {
            Debug.Log("Running EditMode tests and saving to XML!");
            
            SerializableTestResult result = await PlayModeTestExecuter.ExecuteEditModeTest(
                null, 
                true);
                
            LogTestResult(result);
        }
        
        [MenuItem("uMCP/Tools/Test Runner/Run EditMode Tests/All Tests (Log Only)")]
        public static async void RunEditModeTestsAndLogOnly()
        {
            Debug.Log("Running EditMode tests (log only)!");
            
            SerializableTestResult result = await PlayModeTestExecuter.ExecuteEditModeTest(
                null, 
                false);
                
            LogTestResult(result);
        }

        [MenuItem("uMCP/Tools/Test Runner/Run PlayMode Tests/All Tests (Save XML)")]
        public static async void RunPlayModeTestsAndSaveXml()
        {
            Debug.Log("Running PlayMode tests and saving to XML!");
            
            SerializableTestResult result = await PlayModeTestExecuter.ExecutePlayModeTest(
                null, 
                true);
                
            LogTestResult(result);
        }
        
        [MenuItem("uMCP/Tools/Test Runner/Run PlayMode Tests/All Tests (Log Only)")]
        public static async void RunPlayModeTestsAndLogOnly()
        {
            Debug.Log("Running PlayMode tests (log only)!");
            
            SerializableTestResult result = await PlayModeTestExecuter.ExecutePlayModeTest(
                null, 
                false);
                
            LogTestResult(result);
        }
        
        [MenuItem("uMCP/Tools/Test Runner/Open Test Runner Window")]
        public static void OpenTestRunnerWindow()
        {
            // Open Unity's Test Runner Window
            EditorApplication.ExecuteMenuItem("Window/General/Test Runner");
            
            Debug.Log("Opened the Test Runner Window!");
        }
        
        // ===== Menu to run a specific test class =====
        
        [MenuItem("uMCP/Tools/Test Runner/Run Specific Tests/CompileCommandTests")]
        public static async void RunCompileCommandTests()
        {
            Debug.Log("Running only CompileCommandTests!");
            
            TestExecutionFilter filter = TestExecutionFilter.ByClassName("io.github.hatayama.uMCP.CompileCommandTests");
            SerializableTestResult result = await PlayModeTestExecuter.ExecuteEditModeTest(
                filter, 
                false);
                
            LogTestResult(result);
        }
        
        [MenuItem("uMCP/Tools/Test Runner/Run Specific Tests/GetLogsCommandTests")]
        public static async void RunGetLogsCommandTests()
        {
            Debug.Log("Running only GetLogsCommandTests!");
            
            TestExecutionFilter filter = TestExecutionFilter.ByClassName("io.github.hatayama.uMCP.GetLogsCommandTests");
            SerializableTestResult result = await PlayModeTestExecuter.ExecuteEditModeTest(
                filter, 
                false);
                
            LogTestResult(result);
        }
        
        [MenuItem("uMCP/Tools/Test Runner/Run Specific Tests/MainThreadSwitcherTests")]
        public static async void RunMainThreadSwitcherTests()
        {
            Debug.Log("Running only MainThreadSwitcherTests!");
            
            TestExecutionFilter filter = TestExecutionFilter.ByClassName("io.github.hatayama.uMCP.MainThreadSwitcherTests");
            SerializableTestResult result = await PlayModeTestExecuter.ExecuteEditModeTest(
                filter, 
                false);
                
            LogTestResult(result);
        }
        
        [MenuItem("uMCP/Tools/Test Runner/Run Specific Tests/SampleEditModeTest")]
        public static async void RunSampleEditModeTest()
        {
            Debug.Log("Running only SampleEditModeTest!");
            
            TestExecutionFilter filter = TestExecutionFilter.ByClassName("Tests.SampleEditModeTest");
            SerializableTestResult result = await PlayModeTestExecuter.ExecuteEditModeTest(
                filter, 
                false);
                
            LogTestResult(result);
        }
        
        /// <summary>
        /// Log test result
        /// </summary>
        private static void LogTestResult(SerializableTestResult result)
        {
            if (result.success)
            {
                Debug.Log($"Test completed successfully! " +
                         $"Passed: {result.passedCount}, " +
                         $"Failed: {result.failedCount}, " +
                         $"Skipped: {result.skippedCount}");
            }
            else
            {
                Debug.LogError($"Test failed: {result.message}");
            }
            
            if (!string.IsNullOrEmpty(result.xmlPath))
            {
                Debug.Log($"XML file saved to: {result.xmlPath}");
                
                // Select the file in the Project view if it exists
                Object xmlAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                    result.xmlPath.Replace(Application.dataPath, "Assets"));
                if (xmlAsset != null)
                {
                    EditorGUIUtility.PingObject(xmlAsset);
                    Selection.activeObject = xmlAsset;
                }
            }
        }
    }
} 