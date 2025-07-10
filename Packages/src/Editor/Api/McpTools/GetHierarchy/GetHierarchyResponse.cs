using System;
using System.Collections.Generic;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Response structure for GetHierarchy tool
    /// Supports both nested hierarchy data and file export responses
    /// </summary>
    [Serializable]
    public class GetHierarchyResponse : BaseToolResponse
    {
        /// <summary>
        /// List of hierarchy nodes (nested structure)
        /// </summary>
        public List<HierarchyNodeNested> hierarchy { get; }
        
        /// <summary>
        /// Context information about the hierarchy
        /// </summary>
        public HierarchyContext context { get; }
        
        /// <summary>
        /// Whether hierarchy data was saved to file instead of returning directly
        /// </summary>
        public bool hierarchySavedToFile { get; }
        
        /// <summary>
        /// File path where hierarchy data was saved (if saved to file)
        /// </summary>
        public string hierarchyFilePath { get; }
        
        /// <summary>
        /// Reason why data was saved to file
        /// </summary>
        public string saveToFileReason { get; }
        
        /// <summary>
        /// Constructor for direct hierarchy response
        /// </summary>
        public GetHierarchyResponse(List<HierarchyNodeNested> hierarchy, HierarchyContext context)
        {
            this.hierarchy = hierarchy ?? new List<HierarchyNodeNested>();
            this.context = context;
            this.hierarchySavedToFile = false;
            this.hierarchyFilePath = null;
            this.saveToFileReason = null;
        }
        
        /// <summary>
        /// Constructor for file export response
        /// </summary>
        public GetHierarchyResponse(string filePath, string saveReason, HierarchyContext context)
        {
            this.hierarchy = new List<HierarchyNodeNested>();
            this.context = context;
            this.hierarchySavedToFile = true;
            this.hierarchyFilePath = filePath;
            this.saveToFileReason = saveReason;
        }
    }
}