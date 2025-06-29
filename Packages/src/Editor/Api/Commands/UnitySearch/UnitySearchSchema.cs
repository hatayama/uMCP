using System.ComponentModel;
using Newtonsoft.Json;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Output format for search results
    /// </summary>
    public enum SearchOutputFormat
    {
        JSON = 0,
        CSV = 1,
        TSV = 2
    }

    /// <summary>
    /// Search flags for controlling Unity Search behavior
    /// </summary>
    public enum SearchFlags
    {
        Default = 0,
        Synchronous = 1,
        WantsMore = 2,
        Packages = 4,
        Sorted = 8
    }

    /// <summary>
    /// Schema for UnitySearch command parameters
    /// Provides type-safe parameter access with immutable design
    /// Related classes:
    /// - BaseCommandSchema: Provides base timeout functionality
    /// - UnitySearchCommand: Uses this schema for search parameters
    /// </summary>
    public class UnitySearchSchema : BaseCommandSchema
    {
        /// <summary>
        /// Search query string (supports Unity Search syntax)
        /// Examples: '*.cs', 't:Texture2D', 'ref:MyScript', 'p:MyPackage'
        /// </summary>
        [Description("Search query string (supports Unity Search syntax). Examples: '*.cs', 't:Texture2D', 'ref:MyScript', 'p:MyPackage'")]
        public string SearchQuery { get; }

        /// <summary>
        /// Maximum number of search results to return
        /// </summary>
        [Description("Maximum number of search results to return")]
        public int MaxResults { get; }

        /// <summary>
        /// Specific search providers to use (empty = all active providers)
        /// Common providers: 'asset', 'scene', 'menu', 'settings', 'packages'
        /// </summary>
        [Description("Specific search providers to use (empty = all active providers). Common providers: 'asset', 'scene', 'menu', 'settings', 'packages'")]
        public string[] Providers { get; }

        /// <summary>
        /// Filter results by asset type (e.g., 'Texture2D', 'GameObject', 'MonoScript')
        /// </summary>
        [Description("Filter results by asset type (e.g., 'Texture2D', 'GameObject', 'MonoScript')")]
        public string[] AssetTypes { get; }

        /// <summary>
        /// Filter results by file extension (e.g., 'cs', 'prefab', 'mat')
        /// </summary>
        [Description("Filter results by file extension (e.g., 'cs', 'prefab', 'mat')")]
        public string[] FileExtensions { get; }

        /// <summary>
        /// Filter results by path pattern (supports wildcards)
        /// </summary>
        [Description("Filter results by path pattern (supports wildcards)")]
        public string PathFilter { get; }

        /// <summary>
        /// Whether to include detailed descriptions in results
        /// </summary>
        [Description("Whether to include detailed descriptions in results")]
        public bool IncludeDescription { get; }

        /// <summary>
        /// Whether to include thumbnail/preview information
        /// </summary>
        [Description("Whether to include thumbnail/preview information")]
        public bool IncludeThumbnails { get; }

        /// <summary>
        /// Whether to include file metadata (size, modified date)
        /// </summary>
        [Description("Whether to include file metadata (size, modified date)")]
        public bool IncludeMetadata { get; }

        /// <summary>
        /// Whether to save search results to external file
        /// </summary>
        [Description("Whether to save search results to external file to avoid massive token consumption when dealing with large result sets. Results are saved as JSON/CSV files for external reading.")]
        public bool SaveToFile { get; }

        /// <summary>
        /// Output file format when SaveToFile is enabled (JSON, CSV, TSV)
        /// </summary>
        [Description("Output file format when SaveToFile is enabled (JSON, CSV, TSV)")]
        public SearchOutputFormat OutputFormat { get; }

        /// <summary>
        /// Threshold for automatic file saving (if result count exceeds this, automatically save to file)
        /// </summary>
        [Description("Threshold for automatic file saving (if result count exceeds this, automatically save to file). Set to 0 to disable automatic file saving.")]
        public int AutoSaveThreshold { get; }

        /// <summary>
        /// Search flags for controlling Unity Search behavior
        /// </summary>
        [Description("Search flags for controlling Unity Search behavior")]
        public SearchFlags SearchFlags { get; }

        /// <summary>
        /// Create UnitySearchSchema with all parameters
        /// </summary>
        [JsonConstructor]
        public UnitySearchSchema(
            string searchQuery = "",
            int maxResults = 50,
            string[] providers = null,
            string[] assetTypes = null,
            string[] fileExtensions = null,
            string pathFilter = "",
            bool includeDescription = true,
            bool includeThumbnails = false,
            bool includeMetadata = false,
            bool saveToFile = false,
            SearchOutputFormat outputFormat = SearchOutputFormat.JSON,
            int autoSaveThreshold = 100,
            SearchFlags searchFlags = SearchFlags.Default,
            int timeoutSeconds = 10)
            : base(timeoutSeconds)
        {
            SearchQuery = searchQuery ?? "";
            MaxResults = maxResults;
            Providers = providers ?? new string[0];
            AssetTypes = assetTypes ?? new string[0];
            FileExtensions = fileExtensions ?? new string[0];
            PathFilter = pathFilter ?? "";
            IncludeDescription = includeDescription;
            IncludeThumbnails = includeThumbnails;
            IncludeMetadata = includeMetadata;
            SaveToFile = saveToFile;
            OutputFormat = outputFormat;
            AutoSaveThreshold = autoSaveThreshold;
            SearchFlags = searchFlags;
        }

        /// <summary>
        /// Parameterless constructor for new() constraint compatibility
        /// </summary>
        public UnitySearchSchema() : this("", 50, null, null, null, "", true, false, false, false, SearchOutputFormat.JSON, 100, SearchFlags.Default, 10)
        {
        }
    }
} 