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
        
        // Properties that can cause infinite loops or crashes
        private readonly HashSet<string> blacklistedProperties = new HashSet<string>
        {
            // Camera component properties that can cause issues
            "targetTexture",
            "activeTexture",
            "commandBufferCount",
            "allCameras",
            "current",
            "main",
            "scene",
            
            // Transform properties that reference other transforms
            "root",
            "parent",
            
            // General Unity properties that can cause issues
            "gameObject",
            "transform",
            "rigidbody",
            "rigidbody2D",
            "camera",
            "light",
            "animation",
            "constantForce",
            "renderer",
            "audio",
            "networkView",
            "collider",
            "collider2D",
            "hingeJoint",
            "particleSystem",
            
            // Matrix properties that might have circular references
            "worldToLocalMatrix",
            "localToWorldMatrix"
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
                
                // Skip blacklisted properties
                if (blacklistedProperties.Contains(property.Name))
                {
                    McpLogger.LogDebug($"[ComponentPropertySerializer] Skipped blacklisted property '{property.Name}' on {component.GetType().Name}");
                    continue;
                }
                    
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
                catch (Exception ex)
                {
                    // Log which property failed
                    McpLogger.LogWarning($"[ComponentPropertySerializer] Skipped property '{property.Name}' on {component.GetType().Name}: {ex.Message}");
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