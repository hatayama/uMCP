using System.Collections.Generic;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Extended menu item info with validation function flag
    /// </summary>
    public class MenuItemInfoExtended : MenuItemInfo
    {
        public bool IsValidateFunction { get; set; }
    }

    /// <summary>
    /// Extended Unity search response with additional properties
    /// </summary>
    public class UnitySearchResponseExtended : UnitySearchResponse
    {
        public bool Success { get; set; } = true;
        public string ErrorMessage { get; set; }
        public bool ResultsSavedToFile => SavedToFile;
        public string ResultsFilePath => FilePath;
        public long SearchDurationMs => ExecutionTimeMs;
        public string SearchQuery => Query;
        public string SavedFileFormat { get; set; }
        public string SaveToFileReason { get; set; }
        public string[] AppliedFilters => FilterInfo?.AppliedFilters ?? new string[0];
    }

    /// <summary>
    /// Extended log entry with type property
    /// </summary>
    public class LogEntryExtended : LogEntry
    {
        public string type => LogType.ToString();
    }

    /// <summary>
    /// Extended component info with legacy properties
    /// </summary>
    public class ComponentInfoExtended : ComponentInfo
    {
        public string type => TypeName;
        public string fullTypeName => $"{TypeName}, {AssemblyName}";
        public List<ComponentPropertyInfo> properties => Properties;
    }

    /// <summary>
    /// Extended component property info with legacy properties
    /// </summary>
    public class ComponentPropertyInfoExtended : ComponentPropertyInfo
    {
        public string name => Name;
        public string type => Type;
        public object value => Value;
    }

    /// <summary>
    /// Extended hierarchy node with additional properties
    /// </summary>
    public class HierarchyNodeExtended : HierarchyNode
    {
        public new int depth { get; set; }
        public new int id { get; set; }
    }

    /// <summary>
    /// Extended hierarchy response
    /// </summary>
    public class GetHierarchyResponseExtended : GetHierarchyResponse
    {
        public List<HierarchyNode> hierarchy => RootNodes;
        public object context { get; set; }
        
        public GetHierarchyResponseExtended(List<HierarchyNode> nodes, object ctx)
        {
            RootNodes = nodes;
            context = ctx;
        }
    }
}