using System.Collections.Generic;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Search mode for finding GameObjects
    /// </summary>
    public enum SearchMode
    {
        /// <summary>
        /// Search all GameObjects
        /// </summary>
        All = 0,
        
        /// <summary>
        /// Search only root GameObjects
        /// </summary>
        RootOnly = 1,
        
        /// <summary>
        /// Search only child GameObjects
        /// </summary>
        ChildrenOnly = 2,
        
        /// <summary>
        /// Exact name match
        /// </summary>
        Exact = 3,
        
        /// <summary>
        /// Search by path
        /// </summary>
        Path = 4,
        
        /// <summary>
        /// Regular expression search
        /// </summary>
        Regex = 5,
        
        /// <summary>
        /// Contains search
        /// </summary>
        Contains = 6
    }

    /// <summary>
    /// Result of finding GameObjects
    /// </summary>
    public class FindGameObjectResult
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public bool IsActive { get; set; }
        public string Tag { get; set; }
        public int Layer { get; set; }
        public List<ComponentInfo> Components { get; set; }
        public int InstanceId { get; set; }
        
        public FindGameObjectResult()
        {
            Components = new List<ComponentInfo>();
        }
    }

    /// <summary>
    /// Component information
    /// </summary>
    public class ComponentInfo
    {
        public string TypeName { get; set; }
        public string AssemblyName { get; set; }
        public bool IsEnabled { get; set; }
        public List<ComponentPropertyInfo> Properties { get; set; }
        
        public ComponentInfo()
        {
            Properties = new List<ComponentPropertyInfo>();
        }
    }

    /// <summary>
    /// Component property information
    /// </summary>
    public class ComponentPropertyInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public object Value { get; set; }
        public bool IsReadOnly { get; set; }
    }

    /// <summary>
    /// Hierarchy response data
    /// </summary>
    public class GetHierarchyResponse : BaseCommandResponse
    {
        public List<HierarchyNode> RootNodes { get; set; }
        public int TotalGameObjectCount { get; set; }
        public int TotalComponentCount { get; set; }
        
        public GetHierarchyResponse()
        {
            RootNodes = new List<HierarchyNode>();
        }
        
        public GetHierarchyResponse(List<HierarchyNode> nodes, object context)
        {
            RootNodes = nodes ?? new List<HierarchyNode>();
        }
    }

    /// <summary>
    /// Hierarchy node data
    /// </summary>
    public class HierarchyNode
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public bool IsActive { get; set; }
        public List<ComponentInfo> Components { get; set; }
        public List<HierarchyNode> Children { get; set; }
        public int id { get; set; }
        public int depth { get; set; }
        
        public HierarchyNode()
        {
            Components = new List<ComponentInfo>();
            Children = new List<HierarchyNode>();
        }
    }
}