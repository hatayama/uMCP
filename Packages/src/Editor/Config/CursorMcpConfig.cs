using System.Collections.Generic;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// MCP設定のデータ構造
    /// mcp.jsonファイルの構造を表現するimmutableなクラス
    /// </summary>
    public class McpConfig
    {
        public readonly Dictionary<string, McpServerConfigData> mcpServers;

        public McpConfig(Dictionary<string, McpServerConfigData> mcpServers)
        {
            this.mcpServers = mcpServers;
        }
    }

    /// <summary>
    /// MCP サーバー設定のデータ構造
    /// 各サーバーの設定情報を表現するimmutableなクラス
    /// </summary>
    public class McpServerConfigData
    {
        public readonly string command;
        public readonly string[] args;
        public readonly Dictionary<string, string> env;

        public McpServerConfigData(string command, string[] args, Dictionary<string, string> env)
        {
            this.command = command;
            this.args = args;
            this.env = env;
        }
    }


} 