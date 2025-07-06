using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Serializes component properties to ComponentPropertyInfo format
    /// Related classes:
    /// - ComponentSerializer: Uses this to serialize properties
    /// </summary>
    public class ComponentPropertySerializer
    {
        
        
        public ComponentPropertyInfo[] SerializeProperties(Component component)
        {
            if (component == null)
                return new ComponentPropertyInfo[0];
                
            List<ComponentPropertyInfo> propertyInfos = new List<ComponentPropertyInfo>();
            
            // Use SerializedObject to get only Inspector-visible properties
            SerializedObject serializedObject = new SerializedObject(component);
            SerializedProperty iterator = serializedObject.GetIterator();
            
            // Skip the first property (m_Script)
            if (iterator.NextVisible(true))
            {
                while (iterator.NextVisible(false))
                {
                    object value = GetSerializedPropertyValue(iterator);
                    if (value != null)
                    {
                        ComponentPropertyInfoExtended info = new ComponentPropertyInfoExtended
                        {
                            Name = iterator.displayName,
                            Type = iterator.propertyType.ToString(),
                            Value = SerializeValue(value),
                            IsReadOnly = false
                        };
                        
                        propertyInfos.Add(info);
                    }
                }
            }
            
            return propertyInfos.ToArray();
        }
        
        /// <summary>
        /// Extract value from SerializedProperty based on its type
        /// </summary>
        private object GetSerializedPropertyValue(SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    return property.intValue;
                case SerializedPropertyType.Boolean:
                    return property.boolValue;
                case SerializedPropertyType.Float:
                    return property.floatValue;
                case SerializedPropertyType.String:
                    return property.stringValue;
                case SerializedPropertyType.Color:
                    return property.colorValue;
                case SerializedPropertyType.Vector2:
                    return property.vector2Value;
                case SerializedPropertyType.Vector3:
                    return property.vector3Value;
                case SerializedPropertyType.Vector4:
                    return property.vector4Value;
                case SerializedPropertyType.Rect:
                    return property.rectValue;
                case SerializedPropertyType.Bounds:
                    return property.boundsValue;
                case SerializedPropertyType.Quaternion:
                    return property.quaternionValue;
                case SerializedPropertyType.Enum:
                    return property.enumNames[property.enumValueIndex];
                default:
                    return null; // Unsupported property types
            }
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
                return new 
                { 
                    x = q.x, 
                    y = q.y, 
                    z = q.z, 
                    w = q.w,
                    eulerAngles = new { x = q.eulerAngles.x, y = q.eulerAngles.y, z = q.eulerAngles.z }
                };
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