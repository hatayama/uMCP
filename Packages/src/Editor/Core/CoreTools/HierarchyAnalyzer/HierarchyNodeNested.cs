using System;
using System.Collections.Generic;

namespace io.github.hatayama.uLoopMCP
{
    /// <summary>
    /// Represents a single node in the Unity Hierarchy with nested structure
    /// Immutable data structure for AI-friendly JSON serialization with direct parent-child relationships
    /// </summary>
    [Serializable]
    public class HierarchyNodeNested
    {
        /// <summary>
        /// Unity's GetInstanceID() value - unique within session
        /// </summary>
        public readonly int id;
        
        /// <summary>
        /// GameObject name
        /// </summary>
        public readonly string name;
        
        /// <summary>
        /// Depth level in hierarchy (0 for root)
        /// </summary>
        public readonly int depth;
        
        /// <summary>
        /// Whether the GameObject is active
        /// </summary>
        public readonly bool isActive;
        
        /// <summary>
        /// List of component type names attached to this GameObject
        /// </summary>
        public readonly string[] components;
        
        /// <summary>
        /// Child nodes directly nested under this node
        /// </summary>
        public readonly List<HierarchyNodeNested> children;
        
        /// <summary>
        /// Constructor for HierarchyNodeNested
        /// </summary>
        public HierarchyNodeNested(int id, string name, int depth, bool isActive, string[] components, List<HierarchyNodeNested> children = null)
        {
            this.id = id;
            this.name = name ?? string.Empty;
            this.depth = depth;
            this.isActive = isActive;
            this.components = components ?? Array.Empty<string>();
            this.children = children ?? new List<HierarchyNodeNested>();
        }
    }
}