using System;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Response schema for UnitySearch command
    /// Provides type-safe response structure for Unity Search results
    /// Supports both inline results and file-based results for token consumption management
    /// </summary>
    public class UnitySearchResponse : BaseToolResponse
    {
        /// <summary>
        /// Array of search result items (empty if results were saved to file)
        /// </summary>
        public SearchResultItem[] Results { get; set; }

        /// <summary>
        /// Total number of search results found (before MaxResults limit)
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Number of results displayed in this response
        /// </summary>
        public int DisplayedCount { get; set; }

        /// <summary>
        /// Search query that was executed
        /// </summary>
        public string SearchQuery { get; set; }

        /// <summary>
        /// Search providers that were used for the search
        /// </summary>
        public string[] ProvidersUsed { get; set; }

        /// <summary>
        /// Search duration in milliseconds
        /// </summary>
        public long SearchDurationMs { get; set; }

        /// <summary>
        /// Whether the search was completed successfully
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message if search failed
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Path to saved search results file (when SaveToFile is enabled or auto-triggered)
        /// </summary>
        public string ResultsFilePath { get; set; }

        /// <summary>
        /// Whether results were saved to file due to size constraints or user preference
        /// </summary>
        public bool ResultsSavedToFile { get; set; }

        /// <summary>
        /// File format of saved results (JSON, CSV, TSV)
        /// </summary>
        public string SavedFileFormat { get; set; }

        /// <summary>
        /// Reason why results were saved to file (user_request, auto_threshold, error_fallback)
        /// </summary>
        public string SaveToFileReason { get; set; }

        /// <summary>
        /// Applied filters information
        /// </summary>
        public SearchFilterInfo AppliedFilters { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public UnitySearchResponse()
        {
            Results = Array.Empty<SearchResultItem>();
            SearchQuery = string.Empty;
            ProvidersUsed = Array.Empty<string>();
            ErrorMessage = string.Empty;
            ResultsFilePath = string.Empty;
            SavedFileFormat = string.Empty;
            SaveToFileReason = string.Empty;
            Success = true;
            AppliedFilters = new SearchFilterInfo();
        }

        /// <summary>
        /// Constructor for successful inline results
        /// </summary>
        public UnitySearchResponse(SearchResultItem[] results, int totalCount, string searchQuery, 
                                  string[] providersUsed, long searchDurationMs)
        {
            Results = results ?? Array.Empty<SearchResultItem>();
            TotalCount = totalCount;
            DisplayedCount = Results.Length;
            SearchQuery = searchQuery ?? string.Empty;
            ProvidersUsed = providersUsed ?? Array.Empty<string>();
            SearchDurationMs = searchDurationMs;
            Success = true;
            ErrorMessage = string.Empty;
            ResultsFilePath = string.Empty;
            ResultsSavedToFile = false;
            SavedFileFormat = string.Empty;
            SaveToFileReason = string.Empty;
            AppliedFilters = new SearchFilterInfo();
        }

        /// <summary>
        /// Constructor for file-based results
        /// </summary>
        public UnitySearchResponse(string resultsFilePath, string fileFormat, string saveReason,
                                  int totalCount, string searchQuery, string[] providersUsed, 
                                  long searchDurationMs)
        {
            Results = Array.Empty<SearchResultItem>();
            TotalCount = totalCount;
            DisplayedCount = 0;
            SearchQuery = searchQuery ?? string.Empty;
            ProvidersUsed = providersUsed ?? Array.Empty<string>();
            SearchDurationMs = searchDurationMs;
            Success = true;
            ErrorMessage = string.Empty;
            ResultsFilePath = resultsFilePath ?? string.Empty;
            ResultsSavedToFile = true;
            SavedFileFormat = fileFormat ?? string.Empty;
            SaveToFileReason = saveReason ?? string.Empty;
            AppliedFilters = new SearchFilterInfo();
        }

        /// <summary>
        /// Constructor for error cases
        /// </summary>
        public UnitySearchResponse(string errorMessage, string searchQuery)
        {
            Results = Array.Empty<SearchResultItem>();
            TotalCount = 0;
            DisplayedCount = 0;
            SearchQuery = searchQuery ?? string.Empty;
            ProvidersUsed = Array.Empty<string>();
            SearchDurationMs = 0;
            Success = false;
            ErrorMessage = errorMessage ?? string.Empty;
            ResultsFilePath = string.Empty;
            ResultsSavedToFile = false;
            SavedFileFormat = string.Empty;
            SaveToFileReason = string.Empty;
            AppliedFilters = new SearchFilterInfo();
        }
    }

    /// <summary>
    /// Information about applied search filters
    /// </summary>
    [Serializable]
    public class SearchFilterInfo
    {
        /// <summary>
        /// File extensions that were filtered
        /// </summary>
        public string[] FileExtensions { get; set; }

        /// <summary>
        /// Asset types that were filtered
        /// </summary>
        public string[] AssetTypes { get; set; }

        /// <summary>
        /// Path filter pattern that was applied
        /// </summary>
        public string PathFilter { get; set; }

        /// <summary>
        /// Number of results filtered out
        /// </summary>
        public int FilteredOutCount { get; set; }

        public SearchFilterInfo()
        {
            FileExtensions = Array.Empty<string>();
            AssetTypes = Array.Empty<string>();
            PathFilter = string.Empty;
            FilteredOutCount = 0;
        }
    }
} 