using System;
using System.Linq;

namespace io.github.hatayama.uLoopMCP
{
    /// <summary>
    /// Response schema for GetProviderDetails command
    /// Provides type-safe response structure for Unity Search provider information
    /// </summary>
    public class GetProviderDetailsResponse : BaseToolResponse
    {
        /// <summary>
        /// Array of provider information
        /// </summary>
        public ProviderInfo[] Providers { get; set; }

        /// <summary>
        /// Total number of providers found
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Number of active providers
        /// </summary>
        public int ActiveCount { get; set; }

        /// <summary>
        /// Number of inactive providers
        /// </summary>
        public int InactiveCount { get; set; }

        /// <summary>
        /// Whether the request was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message if request failed
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Filter applied (specific provider ID or "all")
        /// </summary>
        public string AppliedFilter { get; set; }

        /// <summary>
        /// Whether results are sorted by priority
        /// </summary>
        public bool SortedByPriority { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public GetProviderDetailsResponse()
        {
            Providers = Array.Empty<ProviderInfo>();
            ErrorMessage = string.Empty;
            AppliedFilter = string.Empty;
            Success = true;
        }

        /// <summary>
        /// Constructor for successful response
        /// </summary>
        public GetProviderDetailsResponse(ProviderInfo[] providers, string appliedFilter, bool sortedByPriority)
        {
            Providers = providers ?? Array.Empty<ProviderInfo>();
            TotalCount = Providers.Length;
            ActiveCount = Providers.Count(p => p.IsActive);
            InactiveCount = TotalCount - ActiveCount;
            Success = true;
            ErrorMessage = string.Empty;
            AppliedFilter = appliedFilter ?? "all";
            SortedByPriority = sortedByPriority;
        }

        /// <summary>
        /// Constructor for error response
        /// </summary>
        public GetProviderDetailsResponse(string errorMessage)
        {
            Providers = Array.Empty<ProviderInfo>();
            TotalCount = 0;
            ActiveCount = 0;
            InactiveCount = 0;
            Success = false;
            ErrorMessage = errorMessage ?? string.Empty;
            AppliedFilter = string.Empty;
            SortedByPriority = false;
        }
    }
} 