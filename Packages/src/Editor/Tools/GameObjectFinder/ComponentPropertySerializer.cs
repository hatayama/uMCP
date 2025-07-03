using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Serializes component properties to ComponentPropertyInfo format
    /// Related classes:
    /// - ComponentSerializer: Uses this to serialize properties
    /// </summary>
    public class ComponentPropertySerializer
    {
        private readonly HashSet<Type> supportedTypes = new HashSet<Type>
        {
            typeof(string),
            typeof(int),
            typeof(float),
            typeof(double),
            typeof(bool),
            typeof(Vector2),
            typeof(Vector3),
            typeof(Vector4),
            typeof(Quaternion),
            typeof(Color),
            typeof(Color32),
            typeof(Rect),
            typeof(Bounds),
            typeof(Matrix4x4)
        };
        
        public ComponentPropertyInfo[] SerializeProperties(Component component, bool includeInheritedProperties)
        {
            if (component == null)
                return new ComponentPropertyInfo[0];
                
            Type componentType = component.GetType();
            BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance;
            
            if (!includeInheritedProperties)
                bindingFlags |= BindingFlags.DeclaredOnly;
                
            PropertyInfo[] properties = componentType.GetProperties(bindingFlags);
            List<ComponentPropertyInfo> propertyInfos = new List<ComponentPropertyInfo>();
            
            foreach (PropertyInfo property in properties)
            {
                if (!property.CanRead)
                    continue;
                    
                if (!IsTypeSupported(property.PropertyType))
                    continue;
                    
                try
                {
                    object value = property.GetValue(component);
                    
                    ComponentPropertyInfo info = new ComponentPropertyInfo
                    {
                        name = property.Name,
                        type = property.PropertyType.Name,
                        value = SerializeValue(value)
                    };
                    
                    propertyInfos.Add(info);
                }
                catch
                {
                    // Skip properties that throw exceptions when accessed
                }
            }
            
            return propertyInfos.ToArray();
        }
        
        private bool IsTypeSupported(Type type)
        {
            return supportedTypes.Contains(type) || type.IsEnum;
        }
        
        private object SerializeValue(object value)
        {
            if (value == null)
                return null;
                
            Type valueType = value.GetType();
            
            // Unity types need special serialization
            if (valueType == typeof(Vector2))
            {
                Vector2 v = (Vector2)value;
                return new { x = v.x, y = v.y };
            }
            else if (valueType == typeof(Vector3))
            {
                Vector3 v = (Vector3)value;
                return new { x = v.x, y = v.y, z = v.z };
            }
            else if (valueType == typeof(Vector4))
            {
                Vector4 v = (Vector4)value;
                return new { x = v.x, y = v.y, z = v.z, w = v.w };
            }
            else if (valueType == typeof(Quaternion))
            {
                Quaternion q = (Quaternion)value;
                return new { x = q.x, y = q.y, z = q.z, w = q.w };
            }
            else if (valueType == typeof(Color))
            {
                Color c = (Color)value;
                return new { r = c.r, g = c.g, b = c.b, a = c.a };
            }
            else if (valueType == typeof(Rect))
            {
                Rect r = (Rect)value;
                return new { x = r.x, y = r.y, width = r.width, height = r.height };
            }
            else if (valueType == typeof(Bounds))
            {
                Bounds b = (Bounds)value;
                return new 
                { 
                    center = new { x = b.center.x, y = b.center.y, z = b.center.z },
                    size = new { x = b.size.x, y = b.size.y, z = b.size.z }
                };
            }
            else if (valueType.IsEnum)
            {
                return value.ToString();
            }
            
            return value;
        }
    }
}