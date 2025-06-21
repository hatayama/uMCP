using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace io.github.hatayama.uMCP
{
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
        public static async Task<object> ExecuteCommandAsync(string commandName, JToken paramsToken)
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
        private static Task<object> HandleGetAvailableCommands(JToken request)
        {
            UnityCommandRegistry registry = CustomCommandManager.GetRegistry();
            string[] commandNames = registry.GetRegisteredCommandNames();
            
            McpLogger.LogDebug($"GetAvailableCommands: Returning {commandNames.Length} commands");
            return Task.FromResult<object>(commandNames);
        }

        /// <summary>
        /// Get detailed command information
        /// </summary>
        private static Task<object> HandleGetCommandDetails(JToken request)
        {
            UnityCommandRegistry registry = CustomCommandManager.GetRegistry();
            CommandInfo[] commands = registry.GetRegisteredCommands();
            
            McpLogger.LogDebug($"GetCommandDetails: Returning {commands.Length} command details");
            return Task.FromResult<object>(commands);
        }
    }
} 