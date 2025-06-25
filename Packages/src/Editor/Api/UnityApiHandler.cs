using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Response for getAvailableCommands meta command
    /// </summary>
    public class GetAvailableCommandsResponse : BaseCommandResponse
    {
        public string[] Commands { get; set; }
    }

    /// <summary>
    /// Response for getCommandDetails meta command
    /// </summary>
    public class GetCommandDetailsResponse : BaseCommandResponse
    {
        public CommandInfo[] Commands { get; set; }
    }

    /// <summary>
    /// Class specialized in handling Unity API calls
    /// Supports new command-based structure
    /// </summary>
    public static class UnityApiHandler
    {
        /// <summary>
        /// Get command registry
        /// Use this registry when adding new commands
        /// </summary>
        public static UnityCommandRegistry CommandRegistry => CustomCommandManager.GetRegistry();

        /// <summary>
        /// Generic command execution method
        /// Uses new command-based structure
        /// </summary>
        /// <param name="commandName">Command name</param>
        /// <param name="paramsToken">Parameters</param>
        /// <returns>Execution result</returns>
        public static async Task<BaseCommandResponse> ExecuteCommandAsync(string commandName, JToken paramsToken)
        {
            // Check for special meta commands
            if (commandName == "getAvailableCommands")
            {
                return await HandleGetAvailableCommands(paramsToken);
            }
            if (commandName == "getCommandDetails")
            {
                return await HandleGetCommandDetails(paramsToken);
            }

            return await CustomCommandManager.GetRegistry().ExecuteCommandAsync(commandName, paramsToken);
        }

        /// <summary>
        /// Get list of available commands
        /// </summary>
        private static Task<GetAvailableCommandsResponse> HandleGetAvailableCommands(JToken request)
        {
            UnityCommandRegistry registry = CustomCommandManager.GetRegistry();
            string[] commandNames = registry.GetRegisteredCommandNames();
            
            GetAvailableCommandsResponse response = new GetAvailableCommandsResponse
            {
                Commands = commandNames
            };
            return Task.FromResult(response);
        }

        /// <summary>
        /// Get detailed command information
        /// </summary>
        private static Task<GetCommandDetailsResponse> HandleGetCommandDetails(JToken request)
        {
            UnityCommandRegistry registry = CustomCommandManager.GetRegistry();
            CommandInfo[] commands = registry.GetRegisteredCommands();
            
            GetCommandDetailsResponse response = new GetCommandDetailsResponse
            {
                Commands = commands
            };
            return Task.FromResult(response);
        }
    }
} 