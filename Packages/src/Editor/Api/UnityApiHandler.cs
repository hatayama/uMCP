using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace io.github.hatayama.uLoopMCP
{

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
        public static UnityToolRegistry CommandRegistry => CustomToolManager.GetRegistry();

        /// <summary>
        /// Generic command execution method
        /// Uses new command-based structure
        /// </summary>
        /// <param name="commandName">Command name</param>
        /// <param name="paramsToken">Parameters</param>
        /// <returns>Execution result</returns>
        public static async Task<BaseToolResponse> ExecuteCommandAsync(string commandName, JToken paramsToken)
        {
            McpLogger.LogDebug($"UnityApiHandler.ExecuteCommandAsync called with command: {commandName}");
            
            // Handle MCP resources requests
            if (commandName == "resources/list")
            {
                McpLogger.LogDebug("Matched resources/list command, calling HandleResourcesListAsync");
                return await HandleResourcesListAsync(paramsToken);
            }
            else if (commandName == "resources/read")
            {
                McpLogger.LogDebug("Matched resources/read command, calling HandleResourcesReadAsync");
                return await HandleResourcesReadAsync(paramsToken);
            }
            
            McpLogger.LogDebug($"Command {commandName} not matched, delegating to CustomToolManager");
            return await CustomToolManager.GetRegistry().ExecuteToolAsync(commandName, paramsToken);
        }

        /// <summary>
        /// Handle resources/list request
        /// </summary>
        private static async Task<BaseToolResponse> HandleResourcesListAsync(JToken paramsToken)
        {
            McpLogger.LogDebug("Handling resources/list request");
            BaseToolResponse response = await McpResourceManager.Instance.HandleResourcesListAsync(paramsToken);
            McpLogger.LogDebug($"resources/list response: {response?.GetType().Name}");
            return response;
        }

        /// <summary>
        /// Handle resources/read request
        /// </summary>
        private static async Task<BaseToolResponse> HandleResourcesReadAsync(JToken paramsToken)
        {
            return await McpResourceManager.Instance.HandleResourcesReadAsync(paramsToken);
        }

    }
} 