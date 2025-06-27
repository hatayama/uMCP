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
        [MenuItem("uMCP/Test Runner/EditMode/Run EditMode Tests (Save XML)")]
        public static async void RunEditModeTestsAndSaveXml()
        {
            Debug.Log("Running EditMode tests and saving to XML!");
            
            SerializableTestResult result = await PlayModeTestExecuter.ExecuteTests(
                TestMode.EditMode, 
                null, 
                true, 
                60);
                
            LogTestResult(result);
        }
        
        [MenuItem("uMCP/Test Runner/EditMode/Run EditMode Tests (Log Only)")]
        public static async void RunEditModeTestsAndLogOnly()
        {
            Debug.Log("Running EditMode tests (log only)!");
            
            SerializableTestResult result = await PlayModeTestExecuter.ExecuteTests(
                TestMode.EditMode, 
                null, 
                false, 
                60);
                
            LogTestResult(result);
        }

        [MenuItem("uMCP/Test Runner/PlayMode/Run PlayMode Tests (Save XML)")]
        public static async void RunPlayModeTestsAndSaveXml()
        {
            Debug.Log("Running PlayMode tests and saving to XML!");
            
            SerializableTestResult result = await PlayModeTestExecuter.ExecuteTests(
                TestMode.PlayMode, 
                null, 
                true, 
                120);
                
            LogTestResult(result);
        }
        
        [MenuItem("uMCP/Test Runner/PlayMode/Run PlayMode Tests (Log Only)")]
        public static async void RunPlayModeTestsAndLogOnly()
        {
            Debug.Log("Running PlayMode tests (log only)!");
            
            SerializableTestResult result = await PlayModeTestExecuter.ExecuteTests(
                TestMode.PlayMode, 
                null, 
                false, 
                120);
                
            LogTestResult(result);
        }
        
        [MenuItem("uMCP/Test Runner/Open Test Runner Window")]
        public static void OpenTestRunnerWindow()
        {
            // Open Unity's Test Runner Window
            EditorApplication.ExecuteMenuItem("Window/General/Test Runner");
            
            Debug.Log("Opened the Test Runner Window!");
        }
        
        // ===== Menu to run a specific test class =====
        
        [MenuItem("uMCP/Test Runner/Run Specific Test/CompileCommandTests")]
        public static async void RunCompileCommandTests()
        {
            Debug.Log("Running only CompileCommandTests!");
            
            TestExecutionFilter filter = TestExecutionFilter.ByClassName("io.github.hatayama.uMCP.CompileCommandTests");
            SerializableTestResult result = await PlayModeTestExecuter.ExecuteTests(
                TestMode.EditMode, 
                filter, 
                false, 
                60);
                
            LogTestResult(result);
        }
        
        [MenuItem("uMCP/Test Runner/Run Specific Test/GetLogsCommandTests")]
        public static async void RunGetLogsCommandTests()
        {
            Debug.Log("Running only GetLogsCommandTests!");
            
            TestExecutionFilter filter = TestExecutionFilter.ByClassName("io.github.hatayama.uMCP.GetLogsCommandTests");
            SerializableTestResult result = await PlayModeTestExecuter.ExecuteTests(
                TestMode.EditMode, 
                filter, 
                false, 
                60);
                
            LogTestResult(result);
        }
        
        [MenuItem("uMCP/Test Runner/Run Specific Test/MainThreadSwitcherTests")]
        public static async void RunMainThreadSwitcherTests()
        {
            Debug.Log("Running only MainThreadSwitcherTests!");
            
            TestExecutionFilter filter = TestExecutionFilter.ByClassName("io.github.hatayama.uMCP.MainThreadSwitcherTests");
            SerializableTestResult result = await PlayModeTestExecuter.ExecuteTests(
                TestMode.EditMode, 
                filter, 
                false, 
                60);
                
            LogTestResult(result);
        }
        
        [MenuItem("uMCP/Test Runner/Run Specific Test/SampleEditModeTest")]
        public static async void RunSampleEditModeTest()
        {
            Debug.Log("Running only SampleEditModeTest!");
            
            TestExecutionFilter filter = TestExecutionFilter.ByClassName("Tests.SampleEditModeTest");
            SerializableTestResult result = await PlayModeTestExecuter.ExecuteTests(
                TestMode.EditMode, 
                filter, 
                false, 
                60);
                
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