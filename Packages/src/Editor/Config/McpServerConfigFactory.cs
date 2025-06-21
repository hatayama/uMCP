using System.Collections.Generic;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Class responsible for creating MCP server configuration objects.
    /// Single Responsibility Principle: Only responsible for creating configuration objects.
    /// </summary>
    public static class McpServerConfigFactory
    {
        /// <summary>
        /// Creates settings for Unity MCP.
        /// </summary>
        /// <param name="port">The port number to use.</param>
        /// <param name="serverPath">The path to the TypeScript server.</param>
        /// <returns>Settings data for Unity MCP.</returns>
        public static McpServerConfigData CreateUnityMcpConfig(int port, string serverPath)
        {
            return new McpServerConfigData(
                command: "node",
                args: new[] { serverPath },
                env: new Dictionary<string, string> { { "UNITY_TCP_PORT", port.ToString() } }
            );
        }

        /// <summary>
        /// Generates a server key for the Unity MCP settings.
        /// </summary>
        /// <param name="port">The port number.</param>
        /// <returns>The server key.</returns>
        public static string CreateUnityMcpServerKey(int port)
        {
            return $"unity-mcp-{port}";
        }
    }
} 