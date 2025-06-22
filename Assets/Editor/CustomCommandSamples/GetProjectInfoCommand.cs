using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;
using System.Collections.Generic;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Project information retrieval custom command
    /// Example of retrieving detailed Unity project information
    /// </summary>
    public class GetProjectInfoCommand : IUnityCommand
    {
        public string CommandName => "getprojectinfo";
        public string Description => "Get detailed Unity project information";

        public CommandParameterSchema ParameterSchema => new CommandParameterSchema();

        public Task<object> ExecuteAsync(JToken paramsToken)
        {
            Debug.Log("GetProjectInfo command executed");
            
            return Task.FromResult<object>(new
            {
                projectName = Application.productName,
                companyName = Application.companyName,
                version = Application.version,
                unityVersion = Application.unityVersion,
                platform = Application.platform.ToString(),
                dataPath = Application.dataPath,
                persistentDataPath = Application.persistentDataPath,
                temporaryCachePath = Application.temporaryCachePath,
                isEditor = Application.isEditor,
                isPlaying = Application.isPlaying,
                targetFrameRate = Application.targetFrameRate,
                runInBackground = Application.runInBackground,
                systemLanguage = Application.systemLanguage.ToString(),
                internetReachability = Application.internetReachability.ToString(),
                deviceType = SystemInfo.deviceType.ToString(),
                deviceModel = SystemInfo.deviceModel,
                operatingSystem = SystemInfo.operatingSystem,
                processorType = SystemInfo.processorType,
                processorCount = SystemInfo.processorCount,
                systemMemorySize = SystemInfo.systemMemorySize,
                graphicsDeviceName = SystemInfo.graphicsDeviceName,
                timestamp = System.DateTime.Now,
                commandName = CommandName
            });
        }
    }
} 