using System;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Attribute to mark classes for automatic MCP tool registration
    /// Classes marked with this attribute will be automatically registered as MCP commands
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class McpToolAttribute : Attribute
    {
        /// <summary>
        /// Initialize McpTool attribute
        /// </summary>
        public McpToolAttribute()
        {
        }
    }
} 