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
    /// Class specialized in handling Unity API calls
    /// Supports new command-based structure
    /// 
    /// Design document reference: Packages/src/Editor/ARCHITECTURE.md
    /// 
    /// Related classes:
    /// - UnityCommandRegistry: Registry that manages all available Unity commands
    /// - CustomCommandManager: Provides access to the command registry singleton
    /// - JsonRpcProcessor: Receives JSON-RPC requests and delegates to this handler
    /// - IUnityCommand: Interface implemented by all command classes
    /// - AbstractUnityCommand: Base class for all Unity commands
    /// - BaseCommandResponse: Base response type for all commands
    /// - MainThreadSwitcher: Ensures command execution on Unity's main thread
    /// 
    /// Command execution flow:
    /// 1. JsonRpcProcessor receives request from TypeScript server
    /// 2. Delegates to ExecuteCommand method with command name and parameters
    /// 3. Looks up command in registry and executes asynchronously
    /// 4. Returns command response or error information
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


    }
} 