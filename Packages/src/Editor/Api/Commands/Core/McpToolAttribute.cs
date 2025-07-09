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
        /// Gets whether this tool should only be displayed in development mode
        /// </summary>
        public bool DisplayDevelopmentOnly { get; set; } = false;

        /// <summary>
        /// Gets the specific security setting required to execute this command
        /// </summary>
        public SecuritySettings RequiredSecuritySetting { get; set; } = SecuritySettings.None;

        /// <summary>
        /// Initialize McpTool attribute
        /// </summary>
        public McpToolAttribute()
        {
        }
    }
} 