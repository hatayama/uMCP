using System.Collections.Generic;
using System.Linq;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Serializer for converting hierarchy data to response format
    /// </summary>
    public class HierarchySerializer
    {
        /// <summary>
        /// Serialize hierarchy nodes and context into GetHierarchyResponse
        /// </summary>
        public GetHierarchyResponse SerializeHierarchy(List<HierarchyNode> nodes, HierarchyContext context)
        {
            // Calculate actual statistics
            int nodeCount = nodes.Count;
            int maxDepth = nodes.Any() ? nodes.Max(n => n.depth) : 0;
            
            // Update context with actual values
            HierarchyContext updatedContext = new HierarchyContext(
                context.sceneType,
                context.sceneName,
                nodeCount,
                maxDepth
            );
            
            return new GetHierarchyResponse(nodes, updatedContext);
        }
    }
}