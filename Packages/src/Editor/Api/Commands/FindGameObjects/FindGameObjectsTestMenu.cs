using UnityEditor;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace io.github.hatayama.uMCP
{
    public static class FindGameObjectsTestMenu
    {
        [MenuItem("uMCP/Test/Test FindGameObjects - Camera")]
        public static async void TestFindGameObjectsCamera()
        {
            FindGameObjectsCommand command = new FindGameObjectsCommand();
            
            JObject parameters = new JObject
            {
                ["RequiredComponents"] = new JArray { "Camera" },
                ["MaxResults"] = 1,
                ["IncludeInheritedProperties"] = true
            };
            
            try
            {
                BaseCommandResponse response = await command.ExecuteAsync(parameters);
                
                if (response is FindGameObjectsResponse findResponse)
                {
                    Debug.Log($"Found {findResponse.totalFound} objects with Camera");
                    
                    foreach (var result in findResponse.results)
                    {
                        Debug.Log($"- {result.name}: {result.components.Length} components");
                        
                        foreach (var component in result.components)
                        {
                            if (component.type == "Camera")
                            {
                                Debug.Log($"  Camera: {component.properties?.Length ?? 0} properties");
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error: {ex.Message}");
            }
        }
        
        [MenuItem("uMCP/Test/Test FindGameObjects - Main Camera by Path")]
        public static async void TestFindMainCameraByPath()
        {
            Debug.Log("[FindGameObjectsTestMenu] Starting Main Camera path search test...");
            
            FindGameObjectsCommand command = new FindGameObjectsCommand();
            
            // Search for Main Camera by path
            JObject parameters = new JObject
            {
                ["NamePattern"] = "Main Camera",
                ["SearchMode"] = "Path",
                ["MaxResults"] = 1
            };
            
            try
            {
                Debug.Log("[FindGameObjectsTestMenu] Executing search for Main Camera...");
                BaseCommandResponse response = await command.ExecuteAsync(parameters);
                
                if (response is FindGameObjectsResponse findResponse)
                {
                    Debug.Log($"[FindGameObjectsTestMenu] Found {findResponse.totalFound} objects");
                    
                    foreach (var result in findResponse.results)
                    {
                        Debug.Log($"[FindGameObjectsTestMenu] - {result.name} at {result.path}");
                        Debug.Log($"[FindGameObjectsTestMenu]   Components: {result.components.Length}");
                        
                        foreach (var component in result.components)
                        {
                            Debug.Log($"[FindGameObjectsTestMenu]   - {component.type}: {component.properties?.Length ?? 0} properties");
                        }
                    }
                }
                else
                {
                    Debug.LogError("[FindGameObjectsTestMenu] Unexpected response type");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[FindGameObjectsTestMenu] Error: {ex.Message}");
                Debug.LogError($"[FindGameObjectsTestMenu] StackTrace: {ex.StackTrace}");
            }
            
            Debug.Log("[FindGameObjectsTestMenu] Test completed");
        }
    }
}