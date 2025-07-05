using System.Threading.Tasks;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Command to retrieve Unity Hierarchy information in AI-friendly format
    /// Related classes:
    /// - HierarchyService: Core logic for hierarchy traversal
    /// - HierarchySerializer: JSON formatting logic
    /// - HierarchyNode: Data structure for hierarchy nodes
    /// </summary>
    [McpTool]
    public class GetHierarchyCommand : AbstractUnityCommand<GetHierarchySchema, GetHierarchyResponse>
    {
        public override string CommandName => "get-hierarchy";
        public override string Description => "Get Unity Hierarchy structure in AI-friendly format";
        
        protected override Task<GetHierarchyResponse> ExecuteAsync(GetHierarchySchema parameters)
        {
            // Create service instances
            HierarchyService service = new HierarchyService();
            HierarchySerializer serializer = new HierarchySerializer();
            
            // Convert schema to options
            HierarchyOptions options = new HierarchyOptions
            {
                IncludeInactive = parameters.IncludeInactive,
                MaxDepth = parameters.MaxDepth,
                RootPath = parameters.RootPath,
                IncludeComponents = parameters.IncludeComponents
            };
            
            // Get hierarchy data
            var nodes = service.GetHierarchyNodes(options);
            var context = service.GetCurrentContext();
            
            // Serialize to response format
            GetHierarchyResponse response = serializer.SerializeHierarchy(nodes, context);
            
            return Task.FromResult(response);
        }
    }
}