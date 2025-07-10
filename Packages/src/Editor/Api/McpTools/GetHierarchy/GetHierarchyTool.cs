using System.Threading.Tasks;
using UnityEngine;
using System.Threading;

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
        
        protected override Task<GetHierarchyResponse> ExecuteAsync(GetHierarchySchema parameters, CancellationToken cancellationToken)
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
            
            // Check for cancellation before starting hierarchy traversal
            cancellationToken.ThrowIfCancellationRequested();
            
            // Get hierarchy data
            var nodes = service.GetHierarchyNodes(options);
            var context = service.GetCurrentContext();
            
            // Check for cancellation after hierarchy traversal
            cancellationToken.ThrowIfCancellationRequested();
            
            // Convert to nested structure
            var nestedNodes = serializer.ConvertToNestedStructure(nodes);
            
            // Check for cancellation after conversion
            cancellationToken.ThrowIfCancellationRequested();
            
            // Create nested response for size calculation
            GetHierarchyResponse nestedResponse = new GetHierarchyResponse(nestedNodes, context);
            
            // Calculate response size using Newtonsoft.Json for accurate size estimation
            var settings = new Newtonsoft.Json.JsonSerializerSettings
            {
                ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore,
                MaxDepth = McpServerConfig.DEFAULT_JSON_MAX_DEPTH
            };
            string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(nestedResponse, Newtonsoft.Json.Formatting.None, settings);
            int estimatedSizeBytes = System.Text.Encoding.UTF8.GetByteCount(jsonString);
            int estimatedSizeKB = estimatedSizeBytes / 1024;
            
            // Check if response should be saved to file
            if (estimatedSizeKB >= parameters.MaxResponseSizeKB)
            {
                // Check for cancellation before file export
                cancellationToken.ThrowIfCancellationRequested();
                
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