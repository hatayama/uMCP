using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Command details retrieval tools for MCP C# SDK format
    /// Related classes:
    /// - GetCommandDetailsCommand: Legacy command version (will be deprecated)
    /// - GetCommandDetailsSchema: Legacy schema (will be deprecated)  
    /// - GetCommandDetailsResponse: Legacy response (will be deprecated)
    /// - UnityCommandRegistry: Source of command information
    /// - CommandInfo: Data structure for command details
    /// </summary>
    [McpServerToolType]
    public static class GetCommandDetailsTools
    {
        /// <summary>
        /// Retrieve detailed information about all registered Unity MCP commands
        /// </summary>
        [McpServerTool(Name = "get-command-details")]
        [Description("Retrieve detailed information about all registered Unity MCP commands")]
        public static Task<GetCommandDetailsToolResult> GetCommandDetails(
            [Description("Include development-only commands in the results")] 
            bool includeDevelopmentOnly = false,
            CancellationToken cancellationToken = default)
        {
            // Get command registry and retrieve all registered commands
            UnityCommandRegistry registry = CustomCommandManager.GetRegistry();
            CommandInfo[] allCommands = registry.GetRegisteredCommands();
            
            // Filter commands based on development-only setting
            CommandInfo[] filteredCommands = allCommands;
            if (!includeDevelopmentOnly)
            {
                filteredCommands = allCommands
                    .Where(cmd => !cmd.DisplayDevelopmentOnly)
                    .ToArray();
            }
            
            return Task.FromResult(new GetCommandDetailsToolResult(
                commands: filteredCommands,
                totalCount: filteredCommands.Length,
                includedDevelopmentOnly: includeDevelopmentOnly,
                timestamp: System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            ));
        }
        
        /// <summary>
        /// Result for get-command-details tool
        /// Compatible with legacy GetCommandDetailsResponse structure
        /// </summary>
        public class GetCommandDetailsToolResult : BaseCommandResponse
        {
            [Description("Array of registered command information")]
            public CommandInfo[] Commands { get; set; }
            
            [Description("Total number of commands returned")]
            public int TotalCount { get; set; }
            
            [Description("Whether development-only commands were included")]
            public bool IncludedDevelopmentOnly { get; set; }
            
            [Description("Timestamp when the command details were retrieved")]
            public string Timestamp { get; set; }

            public GetCommandDetailsToolResult(CommandInfo[] commands, int totalCount, bool includedDevelopmentOnly, string timestamp)
            {
                Commands = commands;
                TotalCount = totalCount;
                IncludedDevelopmentOnly = includedDevelopmentOnly;
                Timestamp = timestamp;
            }
        }
    }
}