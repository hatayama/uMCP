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
        /// <param name="editorType">The editor type for client name.</param>
        /// <returns>Settings data for Unity MCP.</returns>
        public static McpServerConfigData CreateUnityMcpConfig(int port, string serverPath, McpEditorType editorType)
        {
            Dictionary<string, string> env = new Dictionary<string, string>
            {
                { McpConstants.UNITY_TCP_PORT_ENV_KEY, port.ToString() },
                { McpConstants.ENV_KEY_MCP_CLIENT_NAME, GetClientNameForEditor(editorType) }
            };

            return new McpServerConfigData(
                command: McpConstants.NODE_COMMAND,
                args: new[] { serverPath },
                env: env
            );
        }

        /// <summary>
        /// Creates Unity MCP server configuration (legacy overload for backward compatibility).
        /// </summary>
        /// <param name="port">The port number to use.</param>
        /// <param name="serverPath">The path to the TypeScript server.</param>
        /// <returns>Settings data for Unity MCP.</returns>
        public static McpServerConfigData CreateUnityMcpConfig(int port, string serverPath)
        {
            return CreateUnityMcpConfig(port, serverPath, McpEditorType.ClaudeCode);
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
            
            // Add NODE_ENV for development mode (simplified approach)
            if (developmentMode)
            {
                env[McpConstants.ENV_KEY_NODE_ENV] = McpConstants.ENV_VALUE_DEVELOPMENT;
            }
            // For production mode, simply don't set NODE_ENV (default behavior)

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
            return $"{McpConstants.PROJECT_NAME}-{port}";
        }

        /// <summary>
        /// Gets the client name for the specified editor type.
        /// </summary>
        /// <param name="editorType">The editor type.</param>
        /// <returns>The client name.</returns>
        private static string GetClientNameForEditor(McpEditorType editorType)
        {
            return editorType switch
            {
                McpEditorType.Cursor => McpConstants.CLIENT_NAME_CURSOR,
                McpEditorType.ClaudeCode => McpConstants.CLIENT_NAME_CLAUDE_CODE,
                _ => "Unknown MCP Client"
            };
        }
    }
} 