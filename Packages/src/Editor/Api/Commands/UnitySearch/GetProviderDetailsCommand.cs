using System;
using System.Linq;
using System.Threading.Tasks;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Command to retrieve detailed information about Unity Search providers
    /// Provides comprehensive metadata about available search providers
    /// Related classes:
    /// - UnitySearchService: Service layer for provider information retrieval
    /// - ProviderInfo: Data structure for provider information
    /// - GetProviderDetailsSchema: Input parameters schema
    /// - GetProviderDetailsResponse: Output response schema
    /// </summary>
    [McpTool]
    public class GetProviderDetailsCommand : AbstractUnityCommand<GetProviderDetailsSchema, GetProviderDetailsResponse>
    {
        /// <summary>
        /// Command name for MCP tool registration
        /// </summary>
        public override string CommandName => "getproviderdetails";

        /// <summary>
        /// Command description for MCP tool registration
        /// </summary>
        public override string Description => "Get detailed information about Unity Search providers including display names, descriptions, active status, and capabilities";

        /// <summary>
        /// Execute the provider details retrieval command
        /// </summary>
        /// <param name="parameters">Command parameters</param>
        /// <returns>Provider details response</returns>
        protected override Task<GetProviderDetailsResponse> ExecuteAsync(GetProviderDetailsSchema parameters)
        {
            ProviderInfo[] providers;
            string appliedFilter;

            // Get provider details based on parameters
            if (!string.IsNullOrWhiteSpace(parameters.ProviderId))
            {
                // Get specific provider
                ProviderInfo provider = UnitySearchService.GetProviderDetails(parameters.ProviderId);
                if (provider == null)
                {
                    return Task.FromResult(new GetProviderDetailsResponse(
                        providers: Array.Empty<ProviderInfo>(),
                        totalCount: 0,
                        activeCount: 0,
                        inactiveCount: 0,
                        success: false,
                        errorMessage: $"Provider '{parameters.ProviderId}' not found",
                        appliedFilter: parameters.ProviderId,
                        sortedByPriority: false
                    ));
                }
                providers = new[] { provider };
                appliedFilter = parameters.ProviderId;
            }
            else
            {
                // Get all providers
                providers = UnitySearchService.GetProviderDetails();
                appliedFilter = "all";
            }

            // Apply active-only filter if requested
            if (parameters.ActiveOnly)
            {
                providers = providers.Where(p => p.IsActive).ToArray();
                appliedFilter += " (active only)";
            }

            // Remove descriptions if not requested
            if (!parameters.IncludeDescriptions)
            {
                foreach (ProviderInfo provider in providers)
                {
                    provider.Description = "";
                }
            }

            // Sort by priority if requested
            if (parameters.SortByPriority)
            {
                providers = providers.OrderBy(p => p.Priority).ToArray();
            }

            // Calculate counts
            int totalCount = providers.Length;
            int activeCount = providers.Count(p => p.IsActive);
            int inactiveCount = totalCount - activeCount;

            // Log command execution for debugging
            McpLogger.LogDebug($"GetProviderDetails completed: Found {providers.Length} providers" +
                             $" (filter: {appliedFilter}, sorted: {parameters.SortByPriority})");

            return Task.FromResult(new GetProviderDetailsResponse(
                providers: providers,
                totalCount: totalCount,
                activeCount: activeCount,
                inactiveCount: inactiveCount,
                success: true,
                errorMessage: string.Empty,
                appliedFilter: appliedFilter,
                sortedByPriority: parameters.SortByPriority
            ));
        }

        /// <summary>
        /// Apply default values for schema properties if they are null
        /// Ensures reasonable defaults for provider details parameters
        /// </summary>
        protected override GetProviderDetailsSchema ApplyDefaultValues(GetProviderDetailsSchema schema)
        {
            // Ensure string properties are not null
            schema.ProviderId ??= "";

            return schema;
        }
    }
} 