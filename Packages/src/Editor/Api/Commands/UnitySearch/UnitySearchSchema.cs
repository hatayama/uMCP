using System.ComponentModel;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Output format options for search results when saved to file
    /// </summary>
    public enum SearchOutputFormat
    {
        JSON,
        CSV,
        TSV
    }

    /// <summary>
    /// Unity Search flags for controlling search behavior
    /// </summary>
    public enum UnitySearchFlags
    {
        Default = 0,
        Synchronous = 1,
        WantsMore = 2,
        Packages = 4,
        Sorted = 8
    }

    /// <summary>
    /// Schema for UnitySearch command parameters
    /// Provides type-safe parameter access for Unity Search functionality
    /// Related classes:
    /// - SearchResultItem: Individual search result data structure
    /// - UnitySearchResponse: Response containing search results or file path
    /// - UnitySearchService: Service layer for Unity Search API integration
    /// </summary>
    public class UnitySearchSchema : BaseCommandSchema
    {
        /// <summary>
        /// Search query string (supports Unity Search syntax)
        /// Examples: "*.cs", "t:Texture2D", "ref:MyScript", "p:MyPackage"
        /// </summary>
        [Description("Search query string (supports Unity Search syntax). Examples: '*.cs', 't:Texture2D', 'ref:MyScript', 'p:MyPackage'")]
        public string SearchQuery { get; set; } = "";

        /// <summary>
        /// Specific search providers to use (empty = all active providers)
        /// Common providers: "asset", "scene", "menu", "settings", "packages"
        /// </summary>
        [Description("Specific search providers to use (empty = all active providers). Common providers: 'asset', 'scene', 'menu', 'settings', 'packages'")]
        public string[] Providers { get; set; } = new string[0];

        /// <summary>
        /// Maximum number of search results to return
        /// </summary>
        [Description("Maximum number of search results to return")]
        public int MaxResults { get; set; } = 50;

        /// <summary>
        /// Whether to include detailed descriptions in results
        /// </summary>
        [Description("Whether to include detailed descriptions in results")]
        public bool IncludeDescription { get; set; } = true;

        /// <summary>
        /// Whether to include thumbnail/preview information
        /// </summary>
        [Description("Whether to include thumbnail/preview information")]
        public bool IncludeThumbnails { get; set; } = false;

        /// <summary>
        /// Whether to include file metadata (size, modified date)
        /// </summary>
        [Description("Whether to include file metadata (size, modified date)")]
        public bool IncludeMetadata { get; set; } = false;

        /// <summary>
        /// Search flags for controlling Unity Search behavior
        /// </summary>
        [Description("Search flags for controlling Unity Search behavior")]
        public UnitySearchFlags SearchFlags { get; set; } = UnitySearchFlags.Default;

        /// <summary>
        /// Whether to save search results to external file to avoid massive token consumption
        /// When enabled, results are saved as JSON/CSV files and only the file path is returned
        /// </summary>
        [Description("Whether to save search results to external file to avoid massive token consumption when dealing with large result sets. Results are saved as JSON/CSV files for external reading.")]
        public bool SaveToFile { get; set; } = false;

        /// <summary>
        /// Output file format when SaveToFile is enabled (JSON, CSV, TSV)
        /// </summary>
        [Description("Output file format when SaveToFile is enabled (JSON, CSV, TSV)")]
        public SearchOutputFormat OutputFormat { get; set; } = SearchOutputFormat.JSON;

        /// <summary>
        /// Threshold for automatic file saving (if result count exceeds this, automatically save to file)
        /// Set to 0 to disable automatic file saving
        /// </summary>
        [Description("Threshold for automatic file saving (if result count exceeds this, automatically save to file). Set to 0 to disable automatic file saving.")]
        public int AutoSaveThreshold { get; set; } = 100;

        /// <summary>
        /// Filter results by file extension (e.g., "cs", "prefab", "mat")
        /// </summary>
        [Description("Filter results by file extension (e.g., 'cs', 'prefab', 'mat')")]
        public string[] FileExtensions { get; set; } = new string[0];

        /// <summary>
        /// Filter results by asset type (e.g., "Texture2D", "GameObject", "MonoScript")
        /// </summary>
        [Description("Filter results by asset type (e.g., 'Texture2D', 'GameObject', 'MonoScript')")]
        public string[] AssetTypes { get; set; } = new string[0];

        /// <summary>
        /// Filter results by path pattern (supports wildcards)
        /// </summary>
        [Description("Filter results by path pattern (supports wildcards)")]
        public string PathFilter { get; set; } = "";
    }
} 