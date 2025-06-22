using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

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

        public CommandParameterSchema ParameterSchema => new CommandParameterSchema(
            new Dictionary<string, ParameterInfo>
            {
                ["message"] = new ParameterInfo("string", "Message to send to Unity", "Hello from TypeScript MCP Server")
            }
        );

        public Task<object> ExecuteAsync(JToken paramsToken)
        {
            string message = paramsToken?["message"]?.ToString() ?? "No message";
            string response = $"Unity MCP Bridge received: {message}";
            
            McpLogger.LogDebug($"Ping request processed: {message} -> {response}");
            
            return Task.FromResult<object>(response);
        }
    }
} 