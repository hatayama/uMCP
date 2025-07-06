using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Unity hierarchy retrieval tools for MCP C# SDK format
    /// Related classes:
    /// - GetHierarchyCommand: Legacy command version (will be deprecated)
    /// - GetHierarchySchema: Legacy schema (will be deprecated)
    /// - GetHierarchyResponse: Legacy response (will be deprecated)
    /// - HierarchyService: Core hierarchy traversal logic
    /// - HierarchySerializer: JSON formatting logic
    /// - HierarchyNode: Data structure for hierarchy nodes
    /// - HierarchyOptions: Configuration options
    /// </summary>
    [McpServerToolType]
    public static class GetHierarchyTools
    {
        /// <summary>
        /// Get Unity Hierarchy structure in AI-friendly format
        /// </summary>
        [McpServerTool(Name = "get-hierarchy")]
        [Description("Get Unity Hierarchy structure in AI-friendly format")]
        public static Task<GetHierarchyToolResult> GetHierarchy(
            [Description("Include inactive GameObjects in the result")] 
            bool IncludeInactive = true,
            [Description("Maximum depth to traverse (-1 for unlimited)")]
            int MaxDepth = -1,
            [Description("Root path to start traversal from (null for all root objects)")]
            string RootPath = null,
            [Description("Include component information in the result")]
            bool IncludeComponents = true,
            CancellationToken cancellationToken = default)
        {
            // Create service instances
            HierarchyService service = new HierarchyService();
            HierarchySerializer serializer = new HierarchySerializer();
            
            // Convert parameters to options
            HierarchyOptions options = new HierarchyOptions
            {
                IncludeInactive = IncludeInactive,
                MaxDepth = MaxDepth,
                RootPath = RootPath,
                IncludeComponents = IncludeComponents
            };
            
            // Get hierarchy data
            var nodes = service.GetHierarchyNodes(options);
            var context = service.GetCurrentContext();
            
            // Serialize to response format
            GetHierarchyResponse response = serializer.SerializeHierarchy(nodes, context);
            
            // Use extended response that has hierarchy and context properties
            GetHierarchyResponseExtended extendedResponse = new GetHierarchyResponseExtended(response.RootNodes, context);
            GetHierarchyToolResult result = new GetHierarchyToolResult(extendedResponse.hierarchy, context);
            
            return Task.FromResult(result);
        }
        
        /// <summary>
        /// Result for get-hierarchy tool
        /// Compatible with legacy GetHierarchyResponse structure
        /// </summary>
        public class GetHierarchyToolResult : BaseCommandResponse
        {
            [Description("List of hierarchy nodes")]
            public List<HierarchyNode> Hierarchy { get; set; }
            
            [Description("Context information about the hierarchy")]
            public HierarchyContext Context { get; set; }

            public GetHierarchyToolResult(List<HierarchyNode> hierarchy, HierarchyContext context)
            {
                Hierarchy = hierarchy ?? new List<HierarchyNode>();
                Context = context;
            }
        }
    }
}