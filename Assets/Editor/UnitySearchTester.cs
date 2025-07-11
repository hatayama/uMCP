using UnityEngine;
using UnityEditor;
using System.Threading.Tasks;
using io.github.hatayama.uLoopMCP;
using Newtonsoft.Json.Linq;

/// <summary>
/// Manual testing window for UnitySearchTool
/// Provides a simple UI to test various search scenarios
/// </summary>
public class UnitySearchTester : EditorWindow
{
    private string searchQuery = "*.cs";
    private int maxResults = 10;
    private bool saveToFile = false;
    private string lastResult = "";
    private Vector2 scrollPosition;
    
    [MenuItem("uLoopMCP/Windows/Unity Search Tester")]
    public static void ShowWindow()
    {
        GetWindow<UnitySearchTester>("Unity Search Tester");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("Unity Search Command Tester", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        // Input fields
        searchQuery = EditorGUILayout.TextField("Search Query:", searchQuery);
        maxResults = EditorGUILayout.IntField("Max Results:", maxResults);
        saveToFile = EditorGUILayout.Toggle("Save to File:", saveToFile);
        
        GUILayout.Space(10);
        
        // Test buttons
        if (GUILayout.Button("Test Basic Search"))
        {
            TestBasicSearch();
        }
        
        if (GUILayout.Button("Test File Extension Filter"))
        {
            TestFileExtensionFilter();
        }
        
        if (GUILayout.Button("Test Asset Type Filter"))
        {
            TestAssetTypeFilter();
        }
        
        if (GUILayout.Button("Test Empty Query (Should Fail)"))
        {
            TestEmptyQuery();
        }
        
        GUILayout.Space(10);
        
        // Results display
        GUILayout.Label("Last Result:", EditorStyles.boldLabel);
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
        EditorGUILayout.TextArea(lastResult, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
    }
    
    private async void TestBasicSearch()
    {
        UnitySearchTool tool = new UnitySearchTool();
        UnitySearchSchema schema = new UnitySearchSchema
        {
            SearchQuery = searchQuery,
            MaxResults = maxResults,
            SaveToFile = saveToFile
        };
        
        await ExecuteTest(tool, schema, "Basic Search");
    }
    
    private async void TestFileExtensionFilter()
    {
        UnitySearchTool tool = new UnitySearchTool();
        UnitySearchSchema schema = new UnitySearchSchema
        {
            SearchQuery = "*",
            FileExtensions = new string[] { "cs" },
            MaxResults = maxResults,
            SaveToFile = saveToFile
        };
        
        await ExecuteTest(tool, schema, "File Extension Filter");
    }
    
    private async void TestAssetTypeFilter()
    {
        UnitySearchTool tool = new UnitySearchTool();
        UnitySearchSchema schema = new UnitySearchSchema
        {
            SearchQuery = "*",
            AssetTypes = new string[] { "MonoScript" },
            MaxResults = maxResults,
            SaveToFile = saveToFile
        };
        
        await ExecuteTest(tool, schema, "Asset Type Filter");
    }
    
    private async void TestEmptyQuery()
    {
        UnitySearchTool tool = new UnitySearchTool();
        UnitySearchSchema schema = new UnitySearchSchema
        {
            SearchQuery = "",
            MaxResults = maxResults
        };
        
        await ExecuteTest(tool, schema, "Empty Query Test");
    }
    
    private async Task ExecuteTest(UnitySearchTool tool, UnitySearchSchema schema, string testName)
    {
        try
        {
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(schema);
            JToken token = JToken.Parse(json);
            
            BaseToolResponse baseResponse = await tool.ExecuteAsync(token);
            UnitySearchResponse response = baseResponse as UnitySearchResponse;
            
            if (response != null)
            {
                string result = $"=== {testName} ===\n";
                result += $"Success: {response.Success}\n";
                result += $"Total Count: {response.TotalCount}\n";
                result += $"Displayed Count: {response.DisplayedCount}\n";
                result += $"Search Duration: {response.SearchDurationMs}ms\n";
                result += $"Results Saved to File: {response.ResultsSavedToFile}\n";
                
                if (response.ResultsSavedToFile)
                {
                    result += $"File Path: {response.ResultsFilePath}\n";
                    result += $"Save Reason: {response.SaveToFileReason}\n";
                }
                
                if (!response.Success)
                {
                    result += $"Error: {response.ErrorMessage}\n";
                }
                
                if (!response.ResultsSavedToFile && response.Results != null)
                {
                    result += $"\nFirst few results:\n";
                    for (int i = 0; i < Mathf.Min(3, response.Results.Length); i++)
                    {
                        SearchResultItem item = response.Results[i];
                        result += $"- {item.Label} ({item.Type}) - {item.Path}\n";
                    }
                }
                
                lastResult = result;
                Debug.Log($"Unity Search Test: {testName} completed - {(response.Success ? "SUCCESS" : "FAILED")}");
            }
            else
            {
                lastResult = $"=== {testName} ===\nFailed to cast response to UnitySearchResponse";
                Debug.LogError($"Unity Search Test: {testName} - Failed to cast response");
            }
        }
        catch (System.Exception ex)
        {
            lastResult = $"=== {testName} ===\nException: {ex.Message}\nStackTrace: {ex.StackTrace}";
            Debug.LogError($"Unity Search Test: {testName} - Exception: {ex.Message}");
        }
        
        Repaint();
    }
} 