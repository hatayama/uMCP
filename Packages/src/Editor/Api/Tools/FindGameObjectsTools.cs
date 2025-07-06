using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// GameObject search tools for MCP C# SDK format
    /// Related classes:
    /// - FindGameObjectsCommand: Legacy command version (will be deprecated)
    /// - FindGameObjectsSchema: Legacy schema (will be deprecated)
    /// - FindGameObjectsResponse: Legacy response (will be deprecated)
    /// - GameObjectFinderService: Core logic for finding GameObjects
    /// - GameObjectSearchOptions: Search criteria structure
    /// - ComponentSerializer: Serializes component information
    /// </summary>
    [McpServerToolType]
    public static class FindGameObjectsTools
    {
        /// <summary>
        /// Find multiple GameObjects with advanced search criteria (component type, tag, layer, etc.)
        /// </summary>
        [McpServerTool(Name = "find-game-objects")]
        [Description("Find multiple GameObjects with advanced search criteria (component type, tag, layer, etc.)")]
        public static Task<FindGameObjectsToolResult> FindGameObjects(
            [Description("Parameter: NamePattern")] 
            string namePattern = "",
            [Description("Parameter: SearchMode")] 
            SearchMode searchMode = SearchMode.Exact,
            [Description("Parameter: RequiredComponents")] 
            string[] requiredComponents = null,
            [Description("Parameter: Tag")] 
            string tag = "",
            [Description("Parameter: Layer")] 
            int? layer = null,
            [Description("Parameter: IncludeInactive")] 
            bool includeInactive = false,
            [Description("Parameter: MaxResults")] 
            int maxResults = 20,
            [Description("Parameter: IncludeInheritedProperties")] 
            bool includeInheritedProperties = false,
            CancellationToken cancellationToken = default)
        {
            // Initialize default arrays if null
            if (requiredComponents == null)
            {
                requiredComponents = new string[0];
            }
            
            // Validate that at least one search criterion is provided
            if (string.IsNullOrEmpty(namePattern) &&
                requiredComponents.Length == 0 &&
                string.IsNullOrEmpty(tag) &&
                !layer.HasValue)
            {
                return Task.FromResult(new FindGameObjectsToolResult(
                    results: new FindGameObjectResult[0],
                    totalFound: 0,
                    errorMessage: "At least one search criterion must be provided"
                ));
            }
            
            // Convert parameters to search options
            GameObjectSearchOptions options = new GameObjectSearchOptions
            {
                NamePattern = namePattern,
                SearchMode = searchMode,
                RequiredComponents = requiredComponents,
                Tag = tag,
                Layer = layer,
                IncludeInactive = includeInactive,
                MaxResults = maxResults
            };
            
            // Execute search
            GameObjectFinderService service = new GameObjectFinderService();
            GameObjectDetails[] foundObjects = service.FindGameObjectsAdvanced(options);
            
            // Log search results for debugging
            if (foundObjects.Length > maxResults)
            {
                McpLogger.LogWarning($"[FindGameObjectsTools] Found {foundObjects.Length} objects but limited to {maxResults}");
            }
            else
            {
                McpLogger.LogDebug($"[FindGameObjectsTools] Found {foundObjects.Length} objects");
            }
            
            // Convert to response format
            ComponentSerializer serializer = new ComponentSerializer();
            List<FindGameObjectResult> results = new List<FindGameObjectResult>();
            
            foreach (GameObjectDetails details in foundObjects)
            {
                FindGameObjectResult result = new FindGameObjectResult
                {
                    name = details.Name,
                    path = details.Path,
                    isActive = details.IsActive,
                    tag = details.GameObject.tag,
                    layer = details.GameObject.layer,
                    components = serializer.SerializeComponents(details.GameObject)
                };
                
                results.Add(result);
            }
            
            return Task.FromResult(new FindGameObjectsToolResult(
                results: results.ToArray(),
                totalFound: results.Count,
                errorMessage: null
            ));
        }
        
        /// <summary>
        /// Result for find-game-objects tool
        /// Compatible with legacy FindGameObjectsResponse structure
        /// </summary>
        public class FindGameObjectsToolResult : BaseCommandResponse
        {
            [Description("Array of found GameObject information")]
            public FindGameObjectResult[] Results { get; set; }
            
            [Description("Total number of GameObjects found")]
            public int TotalFound { get; set; }
            
            [Description("Error message if search failed")]
            public string ErrorMessage { get; set; }

            public FindGameObjectsToolResult(FindGameObjectResult[] results, int totalFound, string errorMessage)
            {
                Results = results;
                TotalFound = totalFound;
                ErrorMessage = errorMessage;
            }
        }
    }
}