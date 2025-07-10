using System.Collections.Generic;
using System.Linq;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Serializer for converting hierarchy data to response format
    /// Related classes:
    /// - HierarchyNode: Flat hierarchy structure
    /// - HierarchyNodeNested: Nested hierarchy structure
    /// - GetHierarchyResponse: Response structure
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
            
            // Convert to nested structure
            List<HierarchyNodeNested> nestedNodes = ConvertToNestedStructure(nodes);
            
            return new GetHierarchyResponse(nestedNodes, updatedContext);
        }
        
        /// <summary>
        /// Convert flat hierarchy nodes to nested structure
        /// </summary>
        public List<HierarchyNodeNested> ConvertToNestedStructure(List<HierarchyNode> flatNodes)
        {
            if (flatNodes == null || flatNodes.Count == 0)
            {
                return new List<HierarchyNodeNested>();
            }
            
            // Create dictionary for fast lookup
            Dictionary<int, HierarchyNodeNested> nodeDict = new Dictionary<int, HierarchyNodeNested>();
            
            // First pass: create all nodes
            foreach (HierarchyNode flatNode in flatNodes)
            {
                HierarchyNodeNested nestedNode = new HierarchyNodeNested(
                    flatNode.id,
                    flatNode.name,
                    flatNode.depth,
                    flatNode.isActive,
                    flatNode.components
                );
                nodeDict[flatNode.id] = nestedNode;
            }
            
            // Second pass: build parent-child relationships
            List<HierarchyNodeNested> rootNodes = new List<HierarchyNodeNested>();
            
            foreach (HierarchyNode flatNode in flatNodes)
            {
                HierarchyNodeNested nestedNode = nodeDict[flatNode.id];
                
                if (flatNode.parent == null)
                {
                    // Root node
                    rootNodes.Add(nestedNode);
                }
                else
                {
                    // Child node - add to parent's children list
                    if (nodeDict.ContainsKey(flatNode.parent.Value))
                    {
                        nodeDict[flatNode.parent.Value].children.Add(nestedNode);
                    }
                }
            }
            
            return rootNodes;
        }
    }
}