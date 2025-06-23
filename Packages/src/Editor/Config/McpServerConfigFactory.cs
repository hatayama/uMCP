using System.Collections.Generic;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Factory class for creating MCP server configuration objects.
    /// Single Responsibility Principle: Only responsible for object creation.
    /// </summary>
    public static class McpServerConfigFactory
    {
        /// <summary>
        /// Creates Unity MCP server configuration.
        /// </summary>
        /// <param name="port">The port number to use.</param>
        /// <param name="serverPath">The path to the TypeScript server.</param>
        /// <returns>Settings data for Unity MCP.</returns>
        public static McpServerConfigData CreateUnityMcpConfig(int port, string serverPath)
        {
            Dictionary<string, string> env = new Dictionary<string, string>
            {
                { McpConstants.UNITY_TCP_PORT_ENV_KEY, port.ToString() }
            };

            return new McpServerConfigData(
                command: McpConstants.NODE_COMMAND,
                args: new[] { serverPath },
                env: env
            );
        }

        /// <summary>
        /// Creates Unity MCP server configuration with development mode support.
        /// </summary>
        /// <param name="port">The port number.</param>
        /// <param name="serverPath">The path to the TypeScript server.</param>
        /// <param name="developmentMode">Whether to enable development mode.</param>
        /// <returns>Settings data for Unity MCP.</returns>
        public static McpServerConfigData CreateUnityMcpConfigWithDevelopmentMode(int port, string serverPath, bool developmentMode)
        {
            Dictionary<string, string> env = new Dictionary<string, string>
            {
                { McpConstants.UNITY_TCP_PORT_ENV_KEY, port.ToString() }
            };
            
            // Add environment variables based on development mode
            if (developmentMode)
            {
                env[McpConstants.ENV_KEY_UMCP_DEBUG] = McpConstants.ENV_VALUE_TRUE;
                env[McpConstants.ENV_KEY_NODE_ENV] = McpConstants.ENV_VALUE_DEVELOPMENT;
            }
            else
            {
                env[McpConstants.ENV_KEY_UMCP_PRODUCTION] = McpConstants.ENV_VALUE_TRUE;
            }

            return new McpServerConfigData(
                command: McpConstants.NODE_COMMAND,
                args: new[] { serverPath },
                env: env
            );
        }

        /// <summary>
        /// Creates Unity MCP server key.
        /// </summary>
        /// <param name="port">The port number.</param>
        /// <returns>The server key.</returns>
        public static string CreateUnityMcpServerKey(int port)
        {
            return $"{McpConstants.PROJECT_NAME.ToLower()}-{port}";
        }
    }
} 