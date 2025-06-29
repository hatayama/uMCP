using System.ComponentModel;
using Newtonsoft.Json;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Schema for GetProviderDetails command parameters
    /// Provides type-safe parameter access with immutable design
    /// Related classes:
    /// - BaseCommandSchema: Provides base timeout functionality
    /// - GetProviderDetailsCommand: Uses this schema for provider details parameters
    /// </summary>
    public class GetProviderDetailsSchema : BaseCommandSchema
    {
        /// <summary>
        /// Specific provider ID to get details for (empty = all providers). Examples: 'asset', 'scene', 'menu', 'settings'
        /// </summary>
        [Description("Specific provider ID to get details for (empty = all providers). Examples: 'asset', 'scene', 'menu', 'settings'")]
        public string ProviderId { get; }

        /// <summary>
        /// Whether to include only active providers
        /// </summary>
        [Description("Whether to include only active providers")]
        public bool ActiveOnly { get; }

        /// <summary>
        /// Include detailed descriptions for each provider
        /// </summary>
        [Description("Include detailed descriptions for each provider")]
        public bool IncludeDescriptions { get; }

        /// <summary>
        /// Sort providers by priority (lower number = higher priority)
        /// </summary>
        [Description("Sort providers by priority (lower number = higher priority)")]
        public bool SortByPriority { get; }

        /// <summary>
        /// Create GetProviderDetailsSchema with all parameters
        /// </summary>
        [JsonConstructor]
        public GetProviderDetailsSchema(string providerId = "", bool activeOnly = false, bool includeDescriptions = true, bool sortByPriority = true, int timeoutSeconds = 10)
            : base(timeoutSeconds)
        {
            ProviderId = providerId ?? "";
            ActiveOnly = activeOnly;
            IncludeDescriptions = includeDescriptions;
            SortByPriority = sortByPriority;
        }

        /// <summary>
        /// Parameterless constructor for new() constraint compatibility
        /// </summary>
        public GetProviderDetailsSchema() : this("", false, true, true, 10)
        {
        }
    }
} 