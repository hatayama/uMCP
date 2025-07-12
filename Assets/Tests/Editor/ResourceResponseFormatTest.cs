using System;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using io.github.hatayama.uLoopMCP;
using Newtonsoft.Json.Linq;

namespace uLoopMCP.Tests.Editor
{
    /// <summary>
    /// Test to verify the exact response format for resources/list
    /// </summary>
    public class ResourceResponseFormatTest
    {
        [Test]
        public async Task TestResourcesListResponseFormat()
        {
            try 
            {
                Debug.Log("=== Testing resources/list response format ===");
                
                // Test direct call to UnityApiHandler
                BaseToolResponse response = await UnityApiHandler.ExecuteCommandAsync("resources/list", null);
                
                Assert.IsNotNull(response, "Response should not be null");
                Debug.Log($"Response type: {response.GetType().Name}");
                
                // Test JSON serialization (this is what gets sent to TSServer)
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(response);
                Debug.Log($"Full JSON serialization: {json}");
                
                // Parse the JSON back to verify structure
                JObject parsed = JObject.Parse(json);
                Debug.Log($"JSON has 'resources' field: {parsed.ContainsKey("resources")}");
                
                if (parsed.ContainsKey("resources"))
                {
                    JArray resourcesArray = parsed["resources"] as JArray;
                    Debug.Log($"Resources array length: {resourcesArray?.Count ?? 0}");
                    
                    if (resourcesArray != null && resourcesArray.Count > 0)
                    {
                        for (int i = 0; i < resourcesArray.Count; i++)
                        {
                            JObject resource = resourcesArray[i] as JObject;
                            string uri = resource?["uri"]?.ToString();
                            string name = resource?["name"]?.ToString();
                            Debug.Log($"Resource {i}: {uri} - {name}");
                        }
                    }
                }
                
                // Verify the response as McpResourceListResponse
                if (response is McpResourceListResponse listResponse)
                {
                    Debug.Log($"McpResourceListResponse.Resources count: {listResponse.Resources?.Length ?? 0}");
                    Debug.Log($"McpResourceListResponse.NextCursor: {listResponse.NextCursor ?? "null"}");
                }
                
                Debug.Log("=== Test completed successfully ===");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Test failed with exception: {ex}");
                throw;
            }
        }
    }
}