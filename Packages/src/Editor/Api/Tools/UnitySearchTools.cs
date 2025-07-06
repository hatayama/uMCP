using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Unity Search tools for MCP C# SDK format
    /// Related classes:
    /// - UnitySearchCommand: Legacy command version (will be deprecated)
    /// - UnitySearchSchema: Legacy schema (will be deprecated)
    /// - UnitySearchResponse: Legacy response (will be deprecated)
    /// - UnitySearchService: Service layer for Unity Search API integration
    /// - SearchResultItem: Individual search result data structure
    /// - SearchResultExporter: File export functionality for large result sets
    /// </summary>
    [McpServerToolType]
    public static class UnitySearchTools
    {
        /// <summary>
        /// Search Unity project using Unity Search API with comprehensive filtering and export options
        /// </summary>
        [McpServerTool(Name = "unity-search")]
        [Description("Search Unity project using Unity Search API with comprehensive filtering and export options")]
        public static async Task<UnitySearchToolResult> UnitySearch(
            [Description("Search query string (supports Unity Search syntax). Examples: '*.cs', 't:Texture2D', 'ref:MyScript', 'p:MyPackage'. For detailed Unity Search documentation see: https://docs.unity3d.com/6000.1/Documentation/Manual/search-expressions.html and https://docs.unity3d.com/6000.0/Documentation/Manual/search-query-operators.html. Common queries: '*.cs' (all C# files), 't:Texture2D' (Texture2D assets), 'ref:MyScript' (assets referencing MyScript), 'p:MyPackage' (search in package), 't:MonoScript *.cs' (C# scripts only), 'Assets/Scripts/*.cs' (C# files in specific folder). Japanese guide: https://light11.hatenadiary.com/entry/2022/12/12/193119")] 
            string searchQuery = "",
            [Description("(Optional) Specific search providers to use (empty = all active providers). Common providers: 'asset', 'scene', 'menu', 'settings', 'packages'")] 
            string[] providers = null,
            [Description("(Optional) Maximum number of search results to return (default: 50)")] 
            int maxResults = 50,
            [Description("(Optional) Whether to include detailed descriptions in results (default: true)")] 
            bool includeDescription = true,
            [Description("(Optional) Whether to include file metadata like file size and last modified date (default: false)")] 
            bool includeMetadata = false,
            [Description("(Optional) Search flags for controlling Unity Search behavior (default: Default)")] 
            UnitySearchFlags searchFlags = UnitySearchFlags.Default,
            [Description("(Optional) Whether to save search results to external file to avoid massive token consumption when dealing with large result sets. Results are saved as JSON/CSV files for external reading (default: false)")] 
            bool saveToFile = false,
            [Description("(Optional) Output file format when SaveToFile is enabled (default: JSON)")] 
            SearchOutputFormat outputFormat = SearchOutputFormat.JSON,
            [Description("(Optional) Threshold for automatic file saving (if result count exceeds this, automatically save to file). Set to 0 to disable automatic file saving (default: 100)")] 
            int autoSaveThreshold = 100,
            [Description("(Optional) Filter results by file extension (e.g., 'cs', 'prefab', 'mat')")] 
            string[] fileExtensions = null,
            [Description("(Optional) Filter results by asset type (e.g., 'Texture2D', 'GameObject', 'MonoScript')")] 
            string[] assetTypes = null,
            [Description("(Optional) Filter results by path pattern (supports wildcards)")] 
            string pathFilter = "",
            [Description("Timeout for command execution in seconds (default: 15 seconds)")] 
            int timeoutSeconds = 15,
            CancellationToken cancellationToken = default)
        {
            // Create schema from parameters
            UnitySearchSchema schema = new UnitySearchSchema
            {
                SearchQuery = searchQuery ?? "",
                Providers = providers ?? new string[0],
                MaxResults = maxResults,
                IncludeDescription = includeDescription,
                IncludeMetadata = includeMetadata,
                SearchFlags = searchFlags,
                SaveToFile = saveToFile,
                OutputFormat = outputFormat,
                AutoSaveThreshold = autoSaveThreshold,
                FileExtensions = fileExtensions ?? new string[0],
                AssetTypes = assetTypes ?? new string[0],
                PathFilter = pathFilter ?? "",
                TimeoutSeconds = timeoutSeconds
            };

            // Apply default values
            schema = ApplyDefaultValues(schema);

            // Clean up old export files before executing new search
            UnitySearchService.CleanupOldExports();

            // Execute search using service layer
            UnitySearchResponse response = await UnitySearchService.ExecuteSearchAsync(schema);

            // Log search execution for debugging
            if (response.Success)
            {
                string resultInfo = response.ResultsSavedToFile 
                    ? $"Results saved to file: {response.ResultsFilePath} ({response.TotalCount} items)"
                    : $"Returned {response.DisplayedCount} of {response.TotalCount} results inline";
                    
                McpLogger.LogDebug($"Unity Search completed: '{schema.SearchQuery}' - {resultInfo} in {response.SearchDurationMs}ms");
            }
            else
            {
                McpLogger.LogError($"Unity Search failed: '{schema.SearchQuery}' - {response.ErrorMessage}");
            }

            // Convert response to tool result
            return new UnitySearchToolResult(response);
        }

        /// <summary>
        /// Apply default values for schema properties if they are null
        /// Ensures reasonable defaults for Unity Search parameters
        /// </summary>
        private static UnitySearchSchema ApplyDefaultValues(UnitySearchSchema schema)
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
        
        /// <summary>
        /// Result for unity-search tool
        /// Compatible with legacy UnitySearchResponse structure
        /// </summary>
        public class UnitySearchToolResult : BaseCommandResponse
        {
            [Description("Array of search result items (empty if results were saved to file)")]
            public SearchResultItem[] Results { get; set; }

            [Description("Total number of search results found (before MaxResults limit)")]
            public int TotalCount { get; set; }

            [Description("Number of results displayed in this response")]
            public int DisplayedCount { get; set; }

            [Description("Search query that was executed")]
            public string SearchQuery { get; set; }

            [Description("Search providers that were used for the search")]
            public string[] ProvidersUsed { get; set; }

            [Description("Search duration in milliseconds")]
            public long SearchDurationMs { get; set; }

            [Description("Whether the search was completed successfully")]
            public bool Success { get; set; }

            [Description("Error message if search failed")]
            public string ErrorMessage { get; set; }

            [Description("Path to saved search results file (when SaveToFile is enabled or auto-triggered)")]
            public string ResultsFilePath { get; set; }

            [Description("Whether results were saved to file due to size constraints or user preference")]
            public bool ResultsSavedToFile { get; set; }

            [Description("File format of saved results (JSON, CSV, TSV)")]
            public string SavedFileFormat { get; set; }

            [Description("Reason why results were saved to file (user_request, auto_threshold, error_fallback)")]
            public string SaveToFileReason { get; set; }

            [Description("Applied filters information")]
            public SearchFilterInfo AppliedFilters { get; set; }

            public UnitySearchToolResult(UnitySearchResponse response)
            {
                Results = response.Results;
                TotalCount = response.TotalCount;
                DisplayedCount = response.DisplayedCount;
                SearchQuery = response.SearchQuery;
                ProvidersUsed = response.ProvidersUsed;
                SearchDurationMs = response.SearchDurationMs;
                Success = response.Success;
                ErrorMessage = response.ErrorMessage;
                ResultsFilePath = response.ResultsFilePath;
                ResultsSavedToFile = response.ResultsSavedToFile;
                SavedFileFormat = response.SavedFileFormat;
                SaveToFileReason = response.SaveToFileReason;
                AppliedFilters = response.AppliedFilters;
            }
        }
    }
}