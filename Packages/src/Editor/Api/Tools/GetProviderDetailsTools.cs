using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Provider details retrieval tools for MCP C# SDK format
    /// Related classes:
    /// - GetProviderDetailsCommand: Legacy command version (will be deprecated)
    /// - GetProviderDetailsSchema: Legacy schema (will be deprecated)
    /// - GetProviderDetailsResponse: Legacy response (will be deprecated)
    /// - UnitySearchService: Service layer for provider information retrieval
    /// - ProviderInfo: Data structure for provider information
    /// </summary>
    [McpServerToolType]
    public static class GetProviderDetailsTools
    {
        /// <summary>
        /// Get detailed information about Unity Search providers including display names, descriptions, active status, and capabilities
        /// </summary>
        [McpServerTool(Name = "get-provider-details")]
        [Description("Get detailed information about Unity Search providers including display names, descriptions, active status, and capabilities")]
        public static Task<GetProviderDetailsToolResult> GetProviderDetails(
            [Description("Specific provider ID to get details for (empty = all providers). Examples: 'asset', 'scene', 'menu', 'settings'")] 
            string providerId = "",
            [Description("Whether to include only active providers")] 
            bool activeOnly = false,
            [Description("Sort providers by priority (lower number = higher priority)")] 
            bool sortByPriority = true,
            [Description("Include detailed descriptions for each provider")] 
            bool includeDescriptions = true,
            CancellationToken cancellationToken = default)
        {
            ProviderInfo[] providers;
            string appliedFilter;

            // Get provider details based on parameters
            if (!string.IsNullOrWhiteSpace(providerId))
            {
                // Get specific provider
                ProviderInfo provider = UnitySearchService.GetProviderDetails(providerId);
                if (provider == null)
                {
                    return Task.FromResult(new GetProviderDetailsToolResult(
                        providers: System.Array.Empty<ProviderInfo>(),
                        totalCount: 0,
                        activeCount: 0,
                        inactiveCount: 0,
                        success: false,
                        errorMessage: $"Provider '{providerId}' not found",
                        appliedFilter: providerId,
                        sortedByPriority: false
                    ));
                }
                providers = new[] { provider };
                appliedFilter = providerId;
            }
            else
            {
                // Get all providers
                providers = UnitySearchService.GetProviderDetails();
                appliedFilter = "all";
            }

            // Apply active-only filter if requested
            if (activeOnly)
            {
                providers = providers.Where(p => p.IsActive).ToArray();
                appliedFilter += " (active only)";
            }

            // Remove descriptions if not requested
            if (!includeDescriptions)
            {
                foreach (ProviderInfo provider in providers)
                {
                    provider.Description = "";
                }
            }

            // Sort by priority if requested
            if (sortByPriority)
            {
                providers = providers.OrderBy(p => p.Priority).ToArray();
            }

            // Calculate counts
            int totalCount = providers.Length;
            int activeCount = providers.Count(p => p.IsActive);
            int inactiveCount = totalCount - activeCount;

            // Log command execution for debugging
            McpLogger.LogDebug($"GetProviderDetails completed: Found {totalCount} providers" +
                             $" (filter: {appliedFilter}, sorted: {sortByPriority})");

            return Task.FromResult(new GetProviderDetailsToolResult(
                providers: providers,
                totalCount: totalCount,
                activeCount: activeCount,
                inactiveCount: inactiveCount,
                success: true,
                errorMessage: string.Empty,
                appliedFilter: appliedFilter,
                sortedByPriority: sortByPriority
            ));
        }
        
        /// <summary>
        /// Result for get-provider-details tool
        /// Compatible with legacy GetProviderDetailsResponse structure
        /// </summary>
        public class GetProviderDetailsToolResult : BaseCommandResponse
        {
            [Description("Array of provider information")]
            public ProviderInfo[] Providers { get; set; }
            
            [Description("Total number of providers found")]
            public int TotalCount { get; set; }
            
            [Description("Number of active providers")]
            public int ActiveCount { get; set; }
            
            [Description("Number of inactive providers")]
            public int InactiveCount { get; set; }
            
            [Description("Whether the request was successful")]
            public bool Success { get; set; }
            
            [Description("Error message if request failed")]
            public string ErrorMessage { get; set; }
            
            [Description("Filter applied (specific provider ID or \"all\")")]
            public string AppliedFilter { get; set; }
            
            [Description("Whether results are sorted by priority")]
            public bool SortedByPriority { get; set; }

            public GetProviderDetailsToolResult(
                ProviderInfo[] providers, 
                int totalCount, 
                int activeCount, 
                int inactiveCount,
                bool success, 
                string errorMessage, 
                string appliedFilter, 
                bool sortedByPriority)
            {
                Providers = providers;
                TotalCount = totalCount;
                ActiveCount = activeCount;
                InactiveCount = inactiveCount;
                Success = success;
                ErrorMessage = errorMessage;
                AppliedFilter = appliedFilter;
                SortedByPriority = sortedByPriority;
            }
        }
    }
}