using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;

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

        public Task<BaseCommandResponse> ExecuteAsync(JToken paramsToken)
        {
            Debug.Log("GetProjectInfo command executed");
            
            GetProjectInfoResponse response = new GetProjectInfoResponse
            {
                ProjectName = Application.productName,
                CompanyName = Application.companyName,
                Version = Application.version,
                UnityVersion = Application.unityVersion,
                Platform = Application.platform.ToString(),
                DataPath = Application.dataPath,
                PersistentDataPath = Application.persistentDataPath,
                TemporaryCachePath = Application.temporaryCachePath,
                IsEditor = Application.isEditor,
                IsPlaying = Application.isPlaying,
                TargetFrameRate = Application.targetFrameRate,
                RunInBackground = Application.runInBackground,
                SystemLanguage = Application.systemLanguage.ToString(),
                InternetReachability = Application.internetReachability.ToString(),
                DeviceType = SystemInfo.deviceType.ToString(),
                DeviceModel = SystemInfo.deviceModel,
                OperatingSystem = SystemInfo.operatingSystem,
                ProcessorType = SystemInfo.processorType,
                ProcessorCount = SystemInfo.processorCount,
                SystemMemorySize = SystemInfo.systemMemorySize,
                GraphicsDeviceName = SystemInfo.graphicsDeviceName,
                Timestamp = System.DateTime.Now,
                CommandName = CommandName
            };
            
            return Task.FromResult<BaseCommandResponse>(response);
        }
    }
} 