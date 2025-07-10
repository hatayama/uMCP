using System.Collections.Generic;
using System.Threading.Tasks;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Command to find multiple GameObjects with advanced search criteria
    /// Related classes:
    /// - GameObjectFinderService: Core logic for finding GameObjects
    /// - FindGameObjectsSchema: Search parameters
    /// </summary>
    [McpTool]
    public class FindGameObjectsCommand : AbstractUnityCommand<FindGameObjectsSchema, FindGameObjectsResponse>
    {
        public override string CommandName => "find-game-objects";
        public override string Description => "Find multiple GameObjects with advanced search criteria (component type, tag, layer, etc.)";
        
        protected override Task<FindGameObjectsResponse> ExecuteAsync(FindGameObjectsSchema parameters)
        {
            // Validate that at least one search criterion is provided
            if (string.IsNullOrEmpty(parameters.NamePattern) &&
                (parameters.RequiredComponents == null || parameters.RequiredComponents.Length == 0) &&
                string.IsNullOrEmpty(parameters.Tag) &&
                !parameters.Layer.HasValue)
            {
                return Task.FromResult(new FindGameObjectsResponse
                {
                    results = new FindGameObjectResult[0],
                    totalFound = 0,
                    errorMessage = "At least one search criterion must be provided"
                });
            }
            
            // Convert schema to search options
            GameObjectSearchOptions options = new GameObjectSearchOptions
            {
                NamePattern = parameters.NamePattern,
                SearchMode = parameters.SearchMode,
                RequiredComponents = parameters.RequiredComponents,
                Tag = parameters.Tag,
                Layer = parameters.Layer,
                IncludeInactive = parameters.IncludeInactive,
                MaxResults = parameters.MaxResults
            };
            
            // Execute search
            GameObjectFinderService service = new GameObjectFinderService();
            GameObjectDetails[] foundObjects = service.FindGameObjectsAdvanced(options);
            
            // Log search results for debugging
            if (foundObjects.Length > parameters.MaxResults)
            {
                McpLogger.LogWarning($"[FindGameObjectsCommand] Found {foundObjects.Length} objects but limited to {parameters.MaxResults}");
            }
            else
            {
                McpLogger.LogDebug($"[FindGameObjectsCommand] Found {foundObjects.Length} objects");
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
            
            FindGameObjectsResponse response = new FindGameObjectsResponse
            {
                results = results.ToArray(),
                totalFound = results.Count
            };
            
            return Task.FromResult(response);
        }
    }
}