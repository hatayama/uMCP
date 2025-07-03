using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Filters for GameObject search operations
    /// </summary>
    public static class GameObjectSearchFilters
    {
        public static bool MatchesNamePattern(GameObject gameObject, string pattern, bool useRegex)
        {
            if (string.IsNullOrEmpty(pattern))
                return true;
                
            if (useRegex)
            {
                return Regex.IsMatch(gameObject.name, pattern);
            }
            else
            {
                return gameObject.name.Contains(pattern);
            }
        }
        
        public static bool HasRequiredComponents(GameObject gameObject, string[] componentTypes)
        {
            if (componentTypes == null || componentTypes.Length == 0)
                return true;
                
            foreach (string componentType in componentTypes)
            {
                if (string.IsNullOrEmpty(componentType))
                    continue;
                    
                Component component = gameObject.GetComponent(componentType);
                if (component == null)
                    return false;
            }
            
            return true;
        }
        
        public static bool MatchesTag(GameObject gameObject, string tag)
        {
            if (string.IsNullOrEmpty(tag))
                return true;
            
            // Handle untagged objects
            if (gameObject.tag == "Untagged" && tag != "Untagged")
                return false;
                
            return gameObject.CompareTag(tag);
        }
        
        public static bool MatchesLayer(GameObject gameObject, int? layer)
        {
            if (!layer.HasValue)
                return true;
                
            return gameObject.layer == layer.Value;
        }
        
        public static bool MatchesActiveState(GameObject gameObject, bool includeInactive)
        {
            if (includeInactive)
                return true;
                
            return gameObject.activeInHierarchy;
        }
    }
}