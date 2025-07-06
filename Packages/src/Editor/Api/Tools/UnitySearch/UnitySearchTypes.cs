using System;
using System.Collections.Generic;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Unity Search flags for controlling search behavior
    /// </summary>
    [Flags]
    public enum UnitySearchFlags
    {
        /// <summary>
        /// Default search behavior
        /// </summary>
        Default = 0,
        
        /// <summary>
        /// Include packages in search
        /// </summary>
        Packages = 1,
        
        /// <summary>
        /// Synchronous search
        /// </summary>
        Synchronous = 2,
        
        /// <summary>
        /// No indexing
        /// </summary>
        NoIndexing = 4,
        
        /// <summary>
        /// Want more results
        /// </summary>
        WantsMore = 8,
        
        /// <summary>
        /// Debug mode
        /// </summary>
        Debug = 16
    }

    /// <summary>
    /// Search output format
    /// </summary>
    public enum SearchOutputFormat
    {
        /// <summary>
        /// JSON format
        /// </summary>
        JSON = 0,
        
        /// <summary>
        /// CSV format
        /// </summary>
        CSV = 1,
        
        /// <summary>
        /// XML format
        /// </summary>
        XML = 2
    }

    /// <summary>
    /// Unity Search schema
    /// </summary>
    public class UnitySearchSchema : BaseCommandSchema
    {
        public string SearchQuery { get; set; }
        public int MaxResults { get; set; } = 50;
        public string[] Providers { get; set; }
        public int SearchFlags { get; set; } = 0;
        public bool SaveToFile { get; set; } = false;
        public bool IncludeDescription { get; set; } = true;
        public bool IncludeMetadata { get; set; } = false;
        public string PathFilter { get; set; } = "";
        public string[] FileExtensions { get; set; }
        public string[] AssetTypes { get; set; }
        public int AutoSaveThreshold { get; set; } = 100;
        public int OutputFormat { get; set; } = 0;
        public int TimeoutSeconds { get; set; } = 15;
    }

    /// <summary>
    /// Search filter information
    /// </summary>
    public class SearchFilterInfo
    {
        public int TotalMatches { get; set; }
        public int FilteredByPath { get; set; }
        public int FilteredByExtension { get; set; }
        public int FilteredByType { get; set; }
        public string[] AppliedFilters { get; set; }
    }

    /// <summary>
    /// Unity Search response
    /// </summary>
    public class UnitySearchResponse : BaseCommandResponse
    {
        public List<SearchResultItem> Results { get; set; }
        public int TotalCount { get; set; }
        public int DisplayedCount { get; set; }
        public string Query { get; set; }
        public string[] ProvidersUsed { get; set; }
        public bool SavedToFile { get; set; }
        public string FilePath { get; set; }
        public SearchFilterInfo FilterInfo { get; set; }
        
        public UnitySearchResponse()
        {
            Results = new List<SearchResultItem>();
        }
    }
    
    /// <summary>
    /// Provider details response
    /// </summary>
    public class ProviderDetailsResponse : BaseCommandResponse
    {
        public ProviderInfo[] Providers { get; set; }
        public int TotalCount { get; set; }
    }
}