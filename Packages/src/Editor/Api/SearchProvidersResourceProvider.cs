using System;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace io.github.hatayama.uLoopMCP
{
    /// <summary>
    /// Unity Search Providers resource provider
    /// 
    /// Design document reference: Provides Unity Search Providers as MCP Resources
    /// 
    /// Related classes:
    /// - UnitySearchService: Unity Search service
    /// - ProviderInfo: Data class for storing provider information
    /// - McpResourceProvider: Base class for resource providers
    /// - McpResourceManager: Resource management class
    /// </summary>
    [McpResource(Description = "Unity Search providers with detailed information including display names, descriptions, active status, and capabilities")]
    public class SearchProvidersResourceProvider : McpResourceProvider
    {
        private const string SEARCH_PROVIDERS_URI = "unity://search-providers";
        private const string SEARCH_PROVIDERS_MIME_TYPE = "application/json";

        /// <summary>
        /// Resource URI
        /// </summary>
        public override string ResourceUri => SEARCH_PROVIDERS_URI;

        /// <summary>
        /// Get resource list
        /// </summary>
        /// <returns>Resource list</returns>
        public override Task<McpResourceInfo[]> GetResourcesAsync()
        {
            try
            {
                // Search Providersの総数を取得
                ProviderInfo[] providers = UnitySearchService.GetProviderDetails();
                
                McpResourceInfo resourceInfo = new McpResourceInfo
                {
                    Uri = SEARCH_PROVIDERS_URI,
                    Name = "Unity Search Providers",
                    Description = "Detailed information about Unity Search providers including display names, descriptions, active status, and capabilities",
                    MimeType = SEARCH_PROVIDERS_MIME_TYPE
                };

                return Task.FromResult(new[] { resourceInfo });
            }
            catch (Exception ex)
            {
                McpLogger.LogError($"Error getting search providers resources: {ex.Message}");
                return Task.FromResult(new McpResourceInfo[0]);
            }
        }

        /// <summary>
        /// Read resource contents
        /// </summary>
        /// <param name="uri">Resource URI</param>
        /// <returns>Resource contents</returns>
        public override Task<McpResourceContent> ReadResourceAsync(string uri)
        {
            try
            {
                if (uri != SEARCH_PROVIDERS_URI)
                {
                    throw new ArgumentException($"Invalid URI: {uri}");
                }

                // Search Providersを取得
                ProviderInfo[] providers = UnitySearchService.GetProviderDetails();
                
                // JSON形式に変換
                SearchProvidersResourceData resourceData = new SearchProvidersResourceData
                {
                    Providers = providers,
                    TotalCount = providers.Length,
                    GeneratedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                };

                string jsonContent = JsonConvert.SerializeObject(resourceData, Formatting.Indented);

                McpResourceContent content = new McpResourceContent
                {
                    Uri = uri,
                    MimeType = SEARCH_PROVIDERS_MIME_TYPE,
                    Text = jsonContent
                };

                return Task.FromResult(content);
            }
            catch (Exception ex)
            {
                McpLogger.LogError($"Error reading search providers resource: {ex.Message}");
                throw;
            }
        }

    }

    /// <summary>
    /// Search Providers resource data
    /// </summary>
    [Serializable]
    public class SearchProvidersResourceData
    {
        [JsonProperty("providers")]
        public ProviderInfo[] Providers { get; set; }

        [JsonProperty("totalCount")]
        public int TotalCount { get; set; }

        [JsonProperty("generatedAt")]
        public string GeneratedAt { get; set; }
    }
}