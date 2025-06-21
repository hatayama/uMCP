using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Pingコマンドハンドラー
    /// 接続確認とメッセージエコーを行う
    /// </summary>
    public class PingCommand : IUnityCommand
    {
        public string CommandName => "ping";
        public string Description => "Connection test and message echo";

        public Task<object> ExecuteAsync(JToken paramsToken)
        {
            string message = paramsToken?["message"]?.ToString() ?? "No message";
            string response = $"Unity MCP Bridge received: {message}";
            
            McpLogger.LogDebug($"Ping request processed: {message} -> {response}");
            
            return Task.FromResult<object>(response);
        }
    }
} 