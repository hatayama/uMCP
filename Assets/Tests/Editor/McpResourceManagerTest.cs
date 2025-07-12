using System;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using io.github.hatayama.uLoopMCP;

namespace uLoopMCP.Tests.Editor
{
    /// <summary>
    /// Test for MCP Resource Manager functionality
    /// </summary>
    public class McpResourceManagerTest
    {
        [Test]
        public void TestResourceManagerInitialization()
        {
            // Test singleton initialization
            McpResourceManager manager = McpResourceManager.Instance;
            Assert.IsNotNull(manager, "McpResourceManager should be initialized");
            
            // Test that same instance is returned
            McpResourceManager manager2 = McpResourceManager.Instance;
            Assert.AreSame(manager, manager2, "Should return same singleton instance");
        }

        [Test]
        public async Task TestResourcesListAsync()
        {
            try 
            {
                McpResourceManager manager = McpResourceManager.Instance;
                BaseToolResponse response = await manager.HandleResourcesListAsync(null);
                
                Assert.IsNotNull(response, "Response should not be null");
                Debug.Log($"Response type: {response.GetType().Name}");
                
                if (response is McpResourceListResponse listResponse)
                {
                    Debug.Log($"Found {listResponse.Resources?.Length ?? 0} resources");
                    if (listResponse.Resources != null)
                    {
                        foreach (McpResourceInfo resource in listResponse.Resources)
                        {
                            Debug.Log($"Resource: {resource.Uri} - {resource.Name}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Test failed with exception: {ex}");
                throw;
            }
        }

        [Test]
        public async Task TestMenuItemsResourceRead()
        {
            try 
            {
                McpResourceManager manager = McpResourceManager.Instance;
                
                // Create params for reading menu items resource
                Newtonsoft.Json.Linq.JObject paramsObj = new Newtonsoft.Json.Linq.JObject();
                paramsObj["uri"] = "unity://menu-items";
                
                BaseToolResponse response = await manager.HandleResourcesReadAsync(paramsObj);
                
                Assert.IsNotNull(response, "Response should not be null");
                Debug.Log($"Response type: {response.GetType().Name}");
                
                if (response is McpResourceReadResponse readResponse)
                {
                    Debug.Log($"Contents count: {readResponse.Contents?.Length ?? 0}");
                    if (readResponse.Error != null)
                    {
                        Debug.LogError($"Read error: {readResponse.Error}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Test failed with exception: {ex}");
                throw;
            }
        }

        [Test]
        public async Task TestSearchProvidersResourceRead()
        {
            try 
            {
                McpResourceManager manager = McpResourceManager.Instance;
                
                // Create params for reading search providers resource
                Newtonsoft.Json.Linq.JObject paramsObj = new Newtonsoft.Json.Linq.JObject();
                paramsObj["uri"] = "unity://search-providers";
                
                BaseToolResponse response = await manager.HandleResourcesReadAsync(paramsObj);
                
                Assert.IsNotNull(response, "Response should not be null");
                Debug.Log($"Response type: {response.GetType().Name}");
                
                if (response is McpResourceReadResponse readResponse)
                {
                    Debug.Log($"Contents count: {readResponse.Contents?.Length ?? 0}");
                    if (readResponse.Error != null)
                    {
                        Debug.LogError($"Read error: {readResponse.Error}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Test failed with exception: {ex}");
                throw;
            }
        }
    }
}