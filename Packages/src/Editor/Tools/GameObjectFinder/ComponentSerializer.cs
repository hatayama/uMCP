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
        
        public ComponentInfo[] SerializeComponents(GameObject gameObject)
        {
            if (gameObject == null)
                return new ComponentInfo[0];
                
            Component[] components = gameObject.GetComponents<Component>();
            List<ComponentInfo> componentInfos = new List<ComponentInfo>();
            
            foreach (Component component in components)
            {
                if (component == null)
                    continue;
                
                string componentTypeName = component.GetType().Name;
                ComponentPropertyInfo[] properties = propertySerializer.SerializeProperties(component);
                
                ComponentInfoExtended info = new ComponentInfoExtended
                {
                    TypeName = componentTypeName,
                    AssemblyName = component.GetType().Assembly.GetName().Name,
                    IsEnabled = true,
                    Properties = properties != null ? new List<ComponentPropertyInfo>(properties) : new List<ComponentPropertyInfo>()
                };
                
                componentInfos.Add(info);
            }
            
            return componentInfos.ToArray();
        }
    }
}