using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Scene information retrieval custom command
    /// Example of retrieving current scene information
    /// </summary>
    public class GetSceneInfoCommand : IUnityCommand
    {
        public string CommandName => "getsceneinfo";
        public string Description => "Get current scene information";

        public Task<object> ExecuteAsync(JToken paramsToken)
        {
            Debug.Log("GetSceneInfo command executed");
            
            UnityEngine.SceneManagement.Scene currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            
            return Task.FromResult<object>(new
            {
                sceneName = currentScene.name,
                scenePath = currentScene.path,
                sceneIndex = currentScene.buildIndex,
                isLoaded = currentScene.isLoaded,
                isDirty = currentScene.isDirty,
                rootCount = currentScene.rootCount,
                timestamp = System.DateTime.Now,
                commandName = CommandName
            });
        }
    }
} 