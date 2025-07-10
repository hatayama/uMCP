using System.ComponentModel;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Input parameters for GetHierarchy command
    /// </summary>
    public class GetHierarchySchema : BaseToolSchema
    {
        [Description("Whether to include inactive GameObjects in the hierarchy result")]
        public bool IncludeInactive { get; set; } = true;
        
        [Description("Maximum depth to traverse the hierarchy (-1 for unlimited depth)")]
        public int MaxDepth { get; set; } = -1;
        
        [Description("Root GameObject path to start hierarchy traversal from (empty/null for all root objects)")]
        public string RootPath { get; set; }
        
        [Description("Whether to include component information for each GameObject in the hierarchy")]
        public bool IncludeComponents { get; set; } = true;
        
        [Description("Maximum response size in KB before saving to file (default: 100KB)")]
        public int MaxResponseSizeKB { get; set; } = 100;
    }
}