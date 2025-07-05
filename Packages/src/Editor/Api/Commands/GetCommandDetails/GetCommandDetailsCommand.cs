using System.Linq;
using System.Threading.Tasks;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// GetCommandDetails command handler - Type-safe implementation using Schema and Response
    /// Retrieves detailed information about all registered Unity MCP commands
    /// Related classes:
    /// - UnityCommandRegistry: Source of command information
    /// - CommandInfo: Data structure for command details
    /// - GetCommandDetailsResponse: Type-safe response structure
    /// </summary>
    [McpTool(DisplayDevelopmentOnly = true)]
    public class GetCommandDetailsCommand : AbstractUnityCommand<GetCommandDetailsSchema, GetCommandDetailsResponse>
    {
        public override string CommandName => "get-command-details";
        public override string Description => "Retrieve detailed information about all registered Unity MCP commands";

        protected override Task<GetCommandDetailsResponse> ExecuteAsync(GetCommandDetailsSchema parameters)
        {
            // Type-safe parameter access
            bool includeDevelopmentOnly = parameters.IncludeDevelopmentOnly;
            
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
            
            // Create type-safe response
            GetCommandDetailsResponse response = new GetCommandDetailsResponse
            {
                Commands = filteredCommands
            };
            
            return Task.FromResult(response);
        }
    }
} 