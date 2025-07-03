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
                    
                ComponentInfo info = new ComponentInfo
                {
                    type = component.GetType().Name,
                    fullTypeName = component.GetType().FullName,
                    properties = propertySerializer.SerializeProperties(component, includeInheritedProperties)
                };
                
                componentInfos.Add(info);
            }
            
            return componentInfos.ToArray();
        }
    }
}