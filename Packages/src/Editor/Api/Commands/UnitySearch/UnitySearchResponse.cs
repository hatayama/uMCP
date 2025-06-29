using System;
using Newtonsoft.Json;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Response schema for UnitySearch command
    /// Provides type-safe response structure for Unity Search results
    /// Supports both inline results and file-based results for token consumption management
    /// Related classes:
    /// - BaseCommandResponse: Provides timing information
    /// - UnitySearchCommand: Creates instances of this response
    /// - SearchResultItem: Individual search result data structure
    /// - SearchFilterInfo: Filter information structure
    /// </summary>
    public class UnitySearchResponse : BaseCommandResponse
    {
        /// <summary>
        /// Array of search result items (empty if results were saved to file)
        /// </summary>
        public SearchResultItem[] Results { get; }

        /// <summary>
        /// Total number of search results found (before MaxResults limit)
        /// </summary>
        public int TotalCount { get; }

        /// <summary>
        /// Number of results displayed in this response
        /// </summary>
        public int DisplayedCount { get; }

        /// <summary>
        /// Search query that was executed
        /// </summary>
        public string SearchQuery { get; }

        /// <summary>
        /// Search providers that were used for the search
        /// </summary>
        public string[] ProvidersUsed { get; }

        /// <summary>
        /// Search duration in milliseconds
        /// </summary>
        public long SearchDurationMs { get; }

        /// <summary>
        /// Whether the search was completed successfully
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// Error message if search failed
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// Path to saved search results file (when SaveToFile is enabled or auto-triggered)
        /// </summary>
        public string ResultsFilePath { get; }

        /// <summary>
        /// Whether results were saved to file due to size constraints or user preference
        /// </summary>
        public bool ResultsSavedToFile { get; }

        /// <summary>
        /// File format of saved results (JSON, CSV, TSV)
        /// </summary>
        public string SavedFileFormat { get; }

        /// <summary>
        /// Reason why results were saved to file (user_request, auto_threshold, error_fallback)
        /// </summary>
        public string SaveToFileReason { get; }

        /// <summary>
        /// Applied filters information
        /// </summary>
        public SearchFilterInfo AppliedFilters { get; }

        /// <summary>
        /// Main constructor for UnitySearchResponse
        /// </summary>
        [JsonConstructor]
        public UnitySearchResponse(SearchResultItem[] results, int totalCount, int displayedCount, 
                                  string searchQuery, string[] providersUsed, long searchDurationMs,
                                  bool success, string errorMessage, string resultsFilePath, 
                                  bool resultsSavedToFile, string savedFileFormat, string saveToFileReason,
                                  SearchFilterInfo appliedFilters)
        {
            Results = results ?? Array.Empty<SearchResultItem>();
            TotalCount = totalCount;
            DisplayedCount = displayedCount;
            SearchQuery = searchQuery ?? string.Empty;
            ProvidersUsed = providersUsed ?? Array.Empty<string>();
            SearchDurationMs = searchDurationMs;
            Success = success;
            ErrorMessage = errorMessage ?? string.Empty;
            ResultsFilePath = resultsFilePath ?? string.Empty;
            ResultsSavedToFile = resultsSavedToFile;
            SavedFileFormat = savedFileFormat ?? string.Empty;
            SaveToFileReason = saveToFileReason ?? string.Empty;
            AppliedFilters = appliedFilters ?? new SearchFilterInfo(Array.Empty<string>(), Array.Empty<string>(), string.Empty, 0);
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
        public string[] FileExtensions { get; }

        /// <summary>
        /// Asset types that were filtered
        /// </summary>
        public string[] AssetTypes { get; }

        /// <summary>
        /// Path filter pattern that was applied
        /// </summary>
        public string PathFilter { get; }

        /// <summary>
        /// Number of results filtered out
        /// </summary>
        public int FilteredOutCount { get; }

        [JsonConstructor]
        public SearchFilterInfo(string[] fileExtensions, string[] assetTypes, string pathFilter, int filteredOutCount)
        {
            FileExtensions = fileExtensions ?? Array.Empty<string>();
            AssetTypes = assetTypes ?? Array.Empty<string>();
            PathFilter = pathFilter ?? string.Empty;
            FilteredOutCount = filteredOutCount;
        }
    }
} 