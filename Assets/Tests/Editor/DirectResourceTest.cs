using System;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using io.github.hatayama.uLoopMCP;
using Newtonsoft.Json.Linq;

namespace uLoopMCP.Tests.Editor
{
    /// <summary>
    /// Direct test for UnityApiHandler resources handling
    /// </summary>
    public class DirectResourceTest
    {
        [Test]
        public async Task TestDirectResourcesListCall()
        {
            try 
            {
                Debug.Log("Testing direct resources/list call...");
                
                // Call UnityApiHandler directly
                BaseToolResponse response = await UnityApiHandler.ExecuteCommandAsync("resources/list", null);
                
                Assert.IsNotNull(response, "Response should not be null");
                Debug.Log($"Response type: {response.GetType().Name}");
                
                if (response is McpResourceListResponse listResponse)
                {
                    Debug.Log($"Found {listResponse.Resources?.Length ?? 0} resources via direct call");
                    
                    // Test JSON serialization
                    string json = Newtonsoft.Json.JsonConvert.SerializeObject(listResponse);
                    Debug.Log($"Serialized JSON: {json}");
                    
                    if (listResponse.Resources != null)
                    {
                        foreach (McpResourceInfo resource in listResponse.Resources)
                        {
                            Debug.Log($"Direct Resource: {resource.Uri} - {resource.Name}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Direct test failed with exception: {ex}");
                throw;
            }
        }

        [Test]
        public async Task TestDirectResourcesReadCall()
        {
            try 
            {
                Debug.Log("Testing direct resources/read call...");
                
                // Create params for reading menu items resource
                JObject paramsObj = new JObject();
                paramsObj["uri"] = "unity://menu-items";
                
                BaseToolResponse response = await UnityApiHandler.ExecuteCommandAsync("resources/read", paramsObj);
                
                Assert.IsNotNull(response, "Response should not be null");
                Debug.Log($"Response type: {response.GetType().Name}");
                
                if (response is McpResourceReadResponse readResponse)
                {
                    Debug.Log($"Contents count: {readResponse.Contents?.Length ?? 0}");
                    if (readResponse.Error != null)
                    {
                        Debug.LogError($"Direct read error: {readResponse.Error}");
                    }
                    else if (readResponse.Contents != null && readResponse.Contents.Length > 0)
                    {
                        McpResourceContent content = readResponse.Contents[0];
                        Debug.Log($"Direct content MIME: {content.MimeType}");
                        Debug.Log($"Direct content length: {content.Text?.Length ?? 0}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Direct read test failed with exception: {ex}");
                throw;
            }
        }
    }
}