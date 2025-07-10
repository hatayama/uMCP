using System.Threading.Tasks;
using UnityEngine;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Tool to retrieve Unity Hierarchy information in AI-friendly format
    /// Related classes:
    /// - HierarchyService: Core logic for hierarchy traversal
    /// - HierarchySerializer: JSON formatting logic
    /// - HierarchyNode: Data structure for hierarchy nodes
    /// - HierarchyNodeNested: Nested hierarchy structure
    /// - HierarchyResultExporter: File export functionality
    /// </summary>
    [McpTool(Description = "Get Unity Hierarchy structure in AI-friendly format")]
    public class GetHierarchyTool : AbstractUnityTool<GetHierarchySchema, GetHierarchyResponse>
    {
        public override string ToolName => "get-hierarchy";
        
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
            
            // Convert to nested structure
            var nestedNodes = serializer.ConvertToNestedStructure(nodes);
            
            // Create nested response for size calculation
            GetHierarchyResponse nestedResponse = new GetHierarchyResponse(nestedNodes, context);
            
            // Calculate response size
            string jsonString = JsonUtility.ToJson(nestedResponse, true);
            int estimatedSizeBytes = jsonString.Length * 2; // UTF-8 estimation
            int estimatedSizeKB = estimatedSizeBytes / 1024;
            
            // Check if response should be saved to file
            if (estimatedSizeKB > parameters.MaxResponseSizeKB)
            {
                // Save to file and return file path response
                string filePath = HierarchyResultExporter.ExportHierarchyResults(nestedNodes, context);
                return Task.FromResult(new GetHierarchyResponse(filePath, "auto_threshold", context));
            }
            else
            {
                // Return nested response directly
                return Task.FromResult(nestedResponse);
            }
        }
    }
}