using System;
using System.Linq;
using Newtonsoft.Json;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Response schema for GetProviderDetails command
    /// Provides type-safe response structure for Unity Search provider information
    /// Related classes:
    /// - BaseCommandResponse: Provides timing information
    /// - GetProviderDetailsCommand: Creates instances of this response
    /// - ProviderInfo: Individual provider information structure
    /// </summary>
    public class GetProviderDetailsResponse : BaseCommandResponse
    {
        /// <summary>
        /// Array of provider information
        /// </summary>
        public ProviderInfo[] Providers { get; }

        /// <summary>
        /// Total number of providers found
        /// </summary>
        public int TotalCount { get; }

        /// <summary>
        /// Number of active providers
        /// </summary>
        public int ActiveCount { get; }

        /// <summary>
        /// Number of inactive providers
        /// </summary>
        public int InactiveCount { get; }

        /// <summary>
        /// Whether the request was successful
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// Error message if request failed
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// Filter applied (specific provider ID or "all")
        /// </summary>
        public string AppliedFilter { get; }

        /// <summary>
        /// Whether results are sorted by priority
        /// </summary>
        public bool SortedByPriority { get; }

        /// <summary>
        /// Main constructor for GetProviderDetailsResponse
        /// </summary>
        [JsonConstructor]
        public GetProviderDetailsResponse(ProviderInfo[] providers, int totalCount, int activeCount, 
                                        int inactiveCount, bool success, string errorMessage, 
                                        string appliedFilter, bool sortedByPriority)
        {
            Providers = providers ?? Array.Empty<ProviderInfo>();
            TotalCount = totalCount;
            ActiveCount = activeCount;
            InactiveCount = inactiveCount;
            Success = success;
            ErrorMessage = errorMessage ?? string.Empty;
            AppliedFilter = appliedFilter ?? string.Empty;
            SortedByPriority = sortedByPriority;
        }
    }
} 