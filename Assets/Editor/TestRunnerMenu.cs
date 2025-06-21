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
        private static UnityTestExecutionManager testRunnerController;
        private static bool shouldSaveXml = true;
        
        [MenuItem("uMCP/Test Runner/Run EditMode Tests (Save XML)")]
        public static void RunEditModeTestsAndSaveXml()
        {
            Debug.Log("Masamichi, I'm running the EditMode tests and saving to XML!");
            
            shouldSaveXml = true;
            testRunnerController = new UnityTestExecutionManager();
            testRunnerController.RunEditModeTests(OnTestRunComplete);
        }
        
        [MenuItem("uMCP/Test Runner/Run EditMode Tests (Log XML)")]
        public static void RunEditModeTestsAndLogXml()
        {
            Debug.Log("Masamichi, I'm running the EditMode tests and logging the XML!");
            
            shouldSaveXml = false;
            testRunnerController = new UnityTestExecutionManager();
            testRunnerController.RunEditModeTests(OnTestRunComplete);
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
        public static void RunCompileCommandTests()
        {
            Debug.Log("Running only CompileCommandTests!");
            shouldSaveXml = false;
            testRunnerController = new UnityTestExecutionManager();
            // Tests for the io.github.hatayama.uMCP namespace
            TestExecutionFilter filter = TestExecutionFilter.ByClassName("io.github.hatayama.uMCP.CompileCommandTests");
            testRunnerController.RunEditModeTests(filter, OnTestRunComplete);
        }
        
        [MenuItem("uMCP/Test Runner/Run Specific Test/GetLogsCommandTests")]
        public static void RunGetLogsCommandTests()
        {
            Debug.Log("Running only GetLogsCommandTests!");
            shouldSaveXml = false;
            testRunnerController = new UnityTestExecutionManager();
            TestExecutionFilter filter = TestExecutionFilter.ByClassName("io.github.hatayama.uMCP.GetLogsCommandTests");
            testRunnerController.RunEditModeTests(filter, OnTestRunComplete);
        }
        
        [MenuItem("uMCP/Test Runner/Run Specific Test/MainThreadSwitcherTests")]
        public static void RunMainThreadSwitcherTests()
        {
            Debug.Log("Running only MainThreadSwitcherTests!");
            shouldSaveXml = false;
            testRunnerController = new UnityTestExecutionManager();
            TestExecutionFilter filter = TestExecutionFilter.ByClassName("io.github.hatayama.uMCP.MainThreadSwitcherTests");
            testRunnerController.RunEditModeTests(filter, OnTestRunComplete);
        }
        
        [MenuItem("uMCP/Test Runner/Run Specific Test/SampleEditModeTest")]
        public static void RunSampleEditModeTest()
        {
            Debug.Log("Running only SampleEditModeTest!");
            shouldSaveXml = false;
            testRunnerController = new UnityTestExecutionManager();
            TestExecutionFilter filter = TestExecutionFilter.ByClassName("Tests.SampleEditModeTest");
            testRunnerController.RunEditModeTests(filter, OnTestRunComplete);
        }
        
        /// <summary>
        /// Callback on test run completion.
        /// </summary>
        private static void OnTestRunComplete(ITestResultAdaptor result)
        {
            Debug.Log("Masamichi, the test run is complete!");
            
            if (shouldSaveXml)
            {
                // Call the XML save process
                string savedPath = NUnitXmlResultExporter.SaveTestResultAsXml(result);
                
                if (!string.IsNullOrEmpty(savedPath))
                {
                    Debug.Log($"XML file saved successfully!\nPath: {savedPath}");
                    
                    // Select the file in the Project view
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
                    Debug.LogError("Failed to save the XML file...");
                }
            }
            else
            {
                // Log XML to console
                NUnitXmlResultExporter.LogTestResultAsXml(result);
                Debug.Log("Outputted XML to the console! Check the logs above~");
            }
        }
    }
} 