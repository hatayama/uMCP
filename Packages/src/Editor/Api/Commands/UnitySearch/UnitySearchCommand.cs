using System.Threading.Tasks;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Unity Search command handler - Type-safe implementation using Schema and Response
    /// Provides comprehensive Unity Search functionality via MCP interface
    /// Related classes:
    /// - UnitySearchService: Service layer for Unity Search API integration
    /// - SearchResultItem: Individual search result data structure
    /// - SearchResultExporter: File export functionality for large result sets
    /// - UnitySearchSchema: Type-safe parameter schema
    /// - UnitySearchResponse: Type-safe response structure
    /// </summary>
    // [McpTool]  // Disabled to prevent registration - use UnitySearchTools instead
    public class UnitySearchCommand : AbstractUnityCommand<UnitySearchSchema, UnitySearchResponse>
    {
        public override string CommandName => "unity-search";
        public override string Description => "Search Unity project using Unity Search API with comprehensive filtering and export options";

        /// <summary>
        /// Execute Unity search command with type-safe parameters
        /// </summary>
        /// <param name="parameters">Type-safe search parameters</param>
        /// <returns>Search results or file path if exported</returns>
        protected override async Task<UnitySearchResponse> ExecuteAsync(UnitySearchSchema parameters)
        {
            // Clean up old export files before executing new search
            UnitySearchService.CleanupOldExports();

            // Execute search using service layer
            UnitySearchResponse response = await UnitySearchService.ExecuteSearchAsync(parameters);

            // Log search execution for debugging
            if (response.Success)
            {
                string resultInfo = response.ResultsSavedToFile 
                    ? $"Results saved to file: {response.ResultsFilePath} ({response.TotalCount} items)"
                    : $"Returned {response.DisplayedCount} of {response.TotalCount} results inline";
                    
                McpLogger.LogDebug($"Unity Search completed: '{parameters.SearchQuery}' - {resultInfo} in {response.SearchDurationMs}ms");
            }
            else
            {
                McpLogger.LogError($"Unity Search failed: '{parameters.SearchQuery}' - {response.ErrorMessage}");
            }

            return response;
        }

        /// <summary>
        /// Apply default values for schema properties if they are null
        /// Ensures reasonable defaults for Unity Search parameters
        /// </summary>
        protected override UnitySearchSchema ApplyDefaultValues(UnitySearchSchema schema)
        {
            // Ensure arrays are not null
            schema.Providers ??= new string[0];
            schema.FileExtensions ??= new string[0];
            schema.AssetTypes ??= new string[0];

            // Apply reasonable defaults
            if (schema.MaxResults <= 0)
                schema.MaxResults = 50;

            if (schema.AutoSaveThreshold < 0)
                schema.AutoSaveThreshold = 100;

            // Ensure search query is not null
            schema.SearchQuery ??= "";
            schema.PathFilter ??= "";

            return schema;
        }
    }
} 