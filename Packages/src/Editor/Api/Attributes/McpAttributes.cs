using System;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// MCP SDK compatible tool type attribute
    /// Will be replaced with ModelContextProtocol.Server.McpServerToolType when MCP SDK is introduced
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class McpServerToolTypeAttribute : Attribute
    {
    }

    /// <summary>
    /// MCP SDK compatible tool attribute
    /// Will be replaced with ModelContextProtocol.Server.McpServerTool when MCP SDK is introduced
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class McpServerToolAttribute : Attribute
    {
        /// <summary>
        /// The name of the tool as it will be exposed to clients
        /// If not specified, the method name will be used (converted to kebab-case)
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Timeout in milliseconds for this specific tool
        /// Can be overridden by dynamic timeout from client
        /// </summary>
        public int TimeoutMs { get; set; } = 120000; // Default 2 minutes

        public McpServerToolAttribute()
        {
        }

        public McpServerToolAttribute(string name)
        {
            Name = name;
        }
    }
}