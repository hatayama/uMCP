using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// GetVersionコマンドハンドラー
    /// Unityのバージョン情報を取得する
    /// 新しいコマンドの追加例として作成
    /// </summary>
    public class GetVersionCommand : IUnityCommand
    {
        public CommandType CommandType => CommandType.GetVersion;

        public Task<object> ExecuteAsync(JToken paramsToken)
        {
            McpLogger.LogDebug("GetVersion request received");
            
            object response = new
            {
                unityVersion = Application.unityVersion,
                platform = Application.platform.ToString(),
                dataPath = Application.dataPath,
                persistentDataPath = Application.persistentDataPath,
                temporaryCachePath = Application.temporaryCachePath,
                isEditor = Application.isEditor,
                productName = Application.productName,
                companyName = Application.companyName,
                version = Application.version
            };
            
            McpLogger.LogDebug($"GetVersion completed: Unity {Application.unityVersion}");
            
            return Task.FromResult(response);
        }
    }
} 