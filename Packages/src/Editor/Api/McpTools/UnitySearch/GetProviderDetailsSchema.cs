using System.ComponentModel;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Schema for GetProviderDetails command parameters
    /// Provides type-safe parameter access for Unity Search provider information
    /// Related classes:
    /// - ProviderInfo: Provider information data structure
    /// - GetProviderDetailsResponse: Response containing provider details
    /// - UnitySearchService: Service layer for provider information retrieval
    /// </summary>
    public class GetProviderDetailsSchema : BaseToolSchema
    {
        /// <summary>
        /// Specific provider ID to get details for (empty = all providers)
        /// </summary>
        [Description("Specific provider ID to get details for (empty = all providers). Examples: 'asset', 'scene', 'menu', 'settings'")]
        public string ProviderId { get; set; } = "";

        /// <summary>
        /// Whether to include only active providers
        /// </summary>
        [Description("Whether to include only active providers")]
        public bool ActiveOnly { get; set; } = false;

        /// <summary>
        /// Sort providers by priority (lower number = higher priority)
        /// </summary>
        [Description("Sort providers by priority (lower number = higher priority)")]
        public bool SortByPriority { get; set; } = true;

        /// <summary>
        /// Include detailed descriptions for each provider
        /// </summary>
        [Description("Include detailed descriptions for each provider")]
        public bool IncludeDescriptions { get; set; } = true;
    }
} 