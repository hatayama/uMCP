using System;
using System.Collections.Generic;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Response structure for GetHierarchy command
    /// </summary>
    [Serializable]
    public class GetHierarchyResponse : BaseCommandResponse
    {
        /// <summary>
        /// List of hierarchy nodes
        /// </summary>
        public List<HierarchyNode> hierarchy { get; }
        
        /// <summary>
        /// Context information about the hierarchy
        /// </summary>
        public HierarchyContext context { get; }
        
        /// <summary>
        /// Constructor for GetHierarchyResponse
        /// </summary>
        public GetHierarchyResponse(List<HierarchyNode> hierarchy, HierarchyContext context)
        {
            this.hierarchy = hierarchy ?? new List<HierarchyNode>();
            this.context = context;
        }
    }
}