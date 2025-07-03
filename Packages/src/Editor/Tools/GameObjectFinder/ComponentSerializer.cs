using System.Collections.Generic;
using UnityEngine;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Serializes Unity component information to ComponentInfo format
    /// Related classes:
    /// - GameObjectFinderService: Uses this to serialize components
    /// - ComponentPropertySerializer: Serializes individual properties (TODO)
    /// </summary>
    public class ComponentSerializer
    {
        private readonly ComponentPropertySerializer propertySerializer;
        
        // Components that can cause crashes or infinite loops
        private readonly HashSet<string> problematicComponents = new HashSet<string>
        {
            "Camera",
            "AudioListener",
            "FlareLayer"
        };
        
        public ComponentSerializer()
        {
            this.propertySerializer = new ComponentPropertySerializer();
        }
        
        public ComponentInfo[] SerializeComponents(GameObject gameObject, bool includeInheritedProperties = false)
        {
            if (gameObject == null)
                return new ComponentInfo[0];
                
            Component[] components = gameObject.GetComponents<Component>();
            List<ComponentInfo> componentInfos = new List<ComponentInfo>();
            
            foreach (Component component in components)
            {
                if (component == null)
                    continue;
                
                try
                {
                    string componentTypeName = component.GetType().Name;
                    McpLogger.LogDebug($"[ComponentSerializer] Serializing component: {componentTypeName} on {gameObject.name}");
                    
                    // Skip problematic components that can cause crashes
                    if (problematicComponents.Contains(componentTypeName))
                    {
                        McpLogger.LogWarning($"[ComponentSerializer] Skipping problematic component: {componentTypeName}");
                        ComponentInfo info = new ComponentInfo
                        {
                            type = componentTypeName,
                            fullTypeName = component.GetType().FullName,
                            properties = new ComponentPropertyInfo[0] // Empty properties
                        };
                        componentInfos.Add(info);
                        continue;
                    }
                    
                    ComponentInfo fullInfo = new ComponentInfo
                    {
                        type = componentTypeName,
                        fullTypeName = component.GetType().FullName,
                        properties = propertySerializer.SerializeProperties(component, includeInheritedProperties)
                    };
                    
                    componentInfos.Add(fullInfo);
                }
                catch (System.Exception ex)
                {
                    McpLogger.LogError($"[ComponentSerializer] Failed to serialize component {component.GetType().Name}: {ex.Message}");
                    UnityEngine.Debug.LogError($"[ComponentSerializer] Stack trace: {ex.StackTrace}");
                    // Skip this component but continue with others
                }
            }
            
            return componentInfos.ToArray();
        }
    }
}