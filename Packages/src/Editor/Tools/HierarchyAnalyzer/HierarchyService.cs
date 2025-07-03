using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Service for retrieving Unity Hierarchy information
    /// Reusable logic separated from command implementation
    /// </summary>
    public class HierarchyService
    {
        /// <summary>
        /// Get all hierarchy nodes based on options
        /// </summary>
        public List<HierarchyNode> GetHierarchyNodes(HierarchyOptions options)
        {
            List<HierarchyNode> nodes = new List<HierarchyNode>();
            GameObject[] rootObjects = GetRootGameObjects(options.RootPath);
            
            foreach (GameObject root in rootObjects)
            {
                if (!options.IncludeInactive && !root.activeInHierarchy)
                    continue;
                    
                TraverseHierarchy(root, null, 0, options, nodes);
            }
            
            return nodes;
        }
        
        /// <summary>
        /// Get current scene context information
        /// </summary>
        public HierarchyContext GetCurrentContext()
        {
            string sceneType = "editor";
            string sceneName = "Unknown";
            
            // Check if in Prefab Edit Mode
            PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                sceneType = "prefab";
                sceneName = prefabStage.assetPath;
            }
            else if (Application.isPlaying)
            {
                sceneType = "runtime";
                Scene activeScene = SceneManager.GetActiveScene();
                sceneName = activeScene.name;
            }
            else
            {
                sceneType = "editor";
                Scene activeScene = SceneManager.GetActiveScene();
                sceneName = activeScene.name;
            }
            
            return new HierarchyContext(sceneType, sceneName, 0, 0);
        }
        
        private GameObject[] GetRootGameObjects(string rootPath)
        {
            // Check if in Prefab Edit Mode
            PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                GameObject prefabRoot = prefabStage.prefabContentsRoot;
                if (!string.IsNullOrEmpty(rootPath))
                {
                    Transform found = prefabRoot.transform.Find(rootPath);
                    if (found != null)
                        return new[] { found.gameObject };
                    return new GameObject[0];
                }
                return new[] { prefabRoot };
            }
            
            // Normal scene mode
            Scene activeScene = SceneManager.GetActiveScene();
            
            if (!string.IsNullOrEmpty(rootPath))
            {
                GameObject found = GameObject.Find(rootPath);
                if (found != null)
                    return new[] { found };
                return new GameObject[0];
            }
            
            return activeScene.GetRootGameObjects();
        }
        
        private void TraverseHierarchy(GameObject obj, int? parentId, int depth, HierarchyOptions options, List<HierarchyNode> nodes)
        {
            // Check depth limit
            if (options.MaxDepth >= 0 && depth > options.MaxDepth)
                return;
                
            // Get components
            string[] componentNames = new string[0];
            if (options.IncludeComponents)
            {
                Component[] components = obj.GetComponents<Component>();
                componentNames = components
                    .Where(c => c != null)
                    .Select(c => c.GetType().Name)
                    .ToArray();
            }
            
            // Create node
            HierarchyNode node = new HierarchyNode(
                id: obj.GetInstanceID(),
                name: obj.name,
                parent: parentId,
                depth: depth,
                isActive: obj.activeSelf,
                components: componentNames
            );
            
            nodes.Add(node);
            
            // Traverse children
            int currentId = obj.GetInstanceID();
            foreach (Transform child in obj.transform)
            {
                if (!options.IncludeInactive && !child.gameObject.activeInHierarchy)
                    continue;
                    
                TraverseHierarchy(child.gameObject, currentId, depth + 1, options, nodes);
            }
        }
    }
}