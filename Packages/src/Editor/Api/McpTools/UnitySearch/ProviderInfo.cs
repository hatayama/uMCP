using System;

namespace io.github.hatayama.uLoopMCP
{
    /// <summary>
    /// Detailed information about a Unity Search provider
    /// Contains comprehensive metadata about search provider capabilities
    /// Related classes:
    /// - UnitySearchService: Service that provides provider information
    /// - SearchResultItem: Individual search result data structure
    /// </summary>
    [Serializable]
    public class ProviderInfo
    {
        /// <summary>
        /// Unique identifier of the search provider
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Display name of the provider (user-friendly name)
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Description of what this provider searches
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Whether this provider is currently active/enabled
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Priority of this provider (lower number = higher priority)
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// Filter ID/prefix used for this provider (e.g., "p:", "h:", "m:")
        /// </summary>
        public string FilterId { get; set; }

        /// <summary>
        /// Whether this provider supports showing detailed information
        /// </summary>
        public bool ShowDetails { get; set; }

        /// <summary>
        /// Options for showing details (Preview, Inspector, Actions, etc.)
        /// </summary>
        public string ShowDetailsOptions { get; set; }

        /// <summary>
        /// Types of content this provider can search (e.g., "asset", "scene", "menu")
        /// </summary>
        public string[] SupportedTypes { get; set; }

        /// <summary>
        /// Number of available actions for this provider
        /// </summary>
        public int ActionCount { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public ProviderInfo()
        {
            Id = string.Empty;
            DisplayName = string.Empty;
            Description = string.Empty;
            FilterId = string.Empty;
            ShowDetailsOptions = string.Empty;
            SupportedTypes = Array.Empty<string>();
        }

        /// <summary>
        /// Constructor with essential properties
        /// </summary>
        public ProviderInfo(string id, string displayName, bool isActive, int priority)
        {
            Id = id ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Description = displayName ?? string.Empty;
            IsActive = isActive;
            Priority = priority;
            FilterId = string.Empty;
            ShowDetailsOptions = string.Empty;
            SupportedTypes = Array.Empty<string>();
        }

        public override string ToString()
        {
            return $"ProviderInfo: {DisplayName} ({Id}) - Active: {IsActive}, Priority: {Priority}";
        }
    }
} 