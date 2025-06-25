using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;


namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// GetVersion command handler
    /// Get Unity version information
    /// Created as an example of adding new commands
    /// </summary>
    public class GetVersionCommand : IUnityCommand
    {
        public string CommandName => "getversion";
        public string Description => "Get Unity version and project information";

        public CommandParameterSchema ParameterSchema => new CommandParameterSchema();

        public Task<BaseCommandResponse> ExecuteAsync(JToken paramsToken)
        {
            McpLogger.LogDebug("GetVersion request received");
            
            GetVersionResponse response = new GetVersionResponse
            {
                UnityVersion = Application.unityVersion,
                Platform = Application.platform.ToString(),
                DataPath = Application.dataPath,
                PersistentDataPath = Application.persistentDataPath,
                TemporaryCachePath = Application.temporaryCachePath,
                IsEditor = Application.isEditor,
                ProductName = Application.productName,
                CompanyName = Application.companyName,
                Version = Application.version
            };
            
            McpLogger.LogDebug($"GetVersion completed: Unity {Application.unityVersion}");
            
            return Task.FromResult<BaseCommandResponse>(response);
        }
    }
} 