using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Core service for finding GameObjects and extracting component information
    /// Related classes:
    /// - ComponentSerializer: Serializes component properties to ComponentInfo
    /// - FindGameObjectCommand: API command that uses this service
    /// - FindGameObjectsCommand: API command for advanced search
    /// - GameObjectSearchFilters: Filtering logic for search operations
    /// </summary>
    public class GameObjectFinderService
    {
        public GameObjectDetails FindGameObject(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return new GameObjectDetails
                {
                    Found = false,
                    ErrorMessage = "Path cannot be null or empty"
                };
            }
            
            GameObject gameObject = GameObject.Find(path);
            
            if (gameObject == null)
            {
                return new GameObjectDetails
                {
                    Found = false,
                    ErrorMessage = $"GameObject not found at path: {path}"
                };
            }
            
            return new GameObjectDetails
            {
                Found = true,
                GameObject = gameObject,
                Name = gameObject.name,
                Path = GetFullPath(gameObject),
                IsActive = gameObject.activeSelf
            };
        }
        
        private string GetFullPath(GameObject gameObject)
        {
            if (gameObject.transform.parent == null)
            {
                return gameObject.name;
            }
            
            return GetFullPath(gameObject.transform.parent.gameObject) + "/" + gameObject.name;
        }
        
        public GameObjectDetails[] FindGameObjectsAdvanced(GameObjectSearchOptions options)
        {
            List<GameObjectDetails> results = new List<GameObjectDetails>();
            
            // Get all root GameObjects from all loaded scenes
            List<GameObject> allGameObjects = GetAllGameObjects(options.IncludeInactive);
            
            foreach (GameObject gameObject in allGameObjects)
            {
                if (MatchesAllCriteria(gameObject, options))
                {
                    GameObjectDetails details = new GameObjectDetails
                    {
                        Found = true,
                        GameObject = gameObject,
                        Name = gameObject.name,
                        Path = GetFullPath(gameObject),
                        IsActive = gameObject.activeSelf
                    };
                    
                    results.Add(details);
                    
                    if (results.Count >= options.MaxResults)
                        break;
                }
            }
            
            return results.ToArray();
        }
        
        private List<GameObject> GetAllGameObjects(bool includeInactive)
        {
            List<GameObject> allGameObjects = new List<GameObject>();
            
            // Get objects from all loaded scenes
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                {
                    GameObject[] rootObjects = scene.GetRootGameObjects();
                    foreach (GameObject root in rootObjects)
                    {
                        AddGameObjectAndChildren(root, allGameObjects, includeInactive);
                    }
                }
            }
            
            return allGameObjects;
        }
        
        private void AddGameObjectAndChildren(GameObject gameObject, List<GameObject> list, bool includeInactive)
        {
            if (!includeInactive && !gameObject.activeInHierarchy)
                return;
                
            list.Add(gameObject);
            
            foreach (Transform child in gameObject.transform)
            {
                AddGameObjectAndChildren(child.gameObject, list, includeInactive);
            }
        }
        
        private bool MatchesAllCriteria(GameObject gameObject, GameObjectSearchOptions options)
        {
            // Check name pattern
            if (!GameObjectSearchFilters.MatchesNamePattern(gameObject, options.NamePattern, options.UseRegex))
                return false;
                
            // Check required components
            if (!GameObjectSearchFilters.HasRequiredComponents(gameObject, options.RequiredComponents))
                return false;
                
            // Check tag
            if (!GameObjectSearchFilters.MatchesTag(gameObject, options.Tag))
                return false;
                
            // Check layer
            if (!GameObjectSearchFilters.MatchesLayer(gameObject, options.Layer))
                return false;
                
            // Check active state
            if (!GameObjectSearchFilters.MatchesActiveState(gameObject, options.IncludeInactive))
                return false;
                
            return true;
        }
    }
}