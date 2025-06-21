using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Unity MCP command registry class
    /// Supports dynamic command registration, allowing users to add their own commands
    /// </summary>
    public class UnityCommandRegistry
    {
        private readonly Dictionary<string, IUnityCommand> commands = new Dictionary<string, IUnityCommand>();

        /// <summary>
        /// Default constructor
        /// Auto-registers standard commands
        /// </summary>
        public UnityCommandRegistry()
        {
            RegisterDefaultCommands();
        }

        /// <summary>
        /// Register standard commands
        /// </summary>
        private void RegisterDefaultCommands()
        {
            RegisterCommand(new PingCommand());
            RegisterCommand(new CompileCommand());
            RegisterCommand(new GetLogsCommand());
            RegisterCommand(new RunTestsCommand());
            RegisterCommand(new GetVersionCommand());
        }

        /// <summary>
        /// Register command
        /// </summary>
        /// <param name="command">Command to register</param>
        public void RegisterCommand(IUnityCommand command)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            if (string.IsNullOrWhiteSpace(command.CommandName))
            {
                throw new ArgumentException("Command name cannot be null or empty", nameof(command));
            }

            commands[command.CommandName] = command;
            McpLogger.LogDebug($"Command registered: {command.CommandName}");
        }

        /// <summary>
        /// Unregister command
        /// </summary>
        /// <param name="commandName">Name of command to unregister</param>
        public void UnregisterCommand(string commandName)
        {
            if (commands.Remove(commandName))
            {
                McpLogger.LogDebug($"Command unregistered: {commandName}");
            }
        }

        /// <summary>
        /// Execute command
        /// </summary>
        /// <param name="commandName">Command name</param>
        /// <param name="paramsToken">Parameters</param>
        /// <returns>Execution result</returns>
        /// <exception cref="ArgumentException">When command is unknown</exception>
        public async Task<object> ExecuteCommandAsync(string commandName, JToken paramsToken)
        {
            if (!commands.TryGetValue(commandName, out IUnityCommand command))
            {
                throw new ArgumentException($"Unknown command: {commandName}");
            }

            McpLogger.LogDebug($"Executing command: {commandName}");
            await MainThreadSwitcher.SwitchToMainThread();
            return await command.ExecuteAsync(paramsToken);
        }

        /// <summary>
        /// Get list of registered command names
        /// </summary>
        /// <returns>Array of command names</returns>
        public string[] GetRegisteredCommandNames()
        {
            return commands.Keys.ToArray();
        }

        /// <summary>
        /// Get detailed information of registered commands
        /// </summary>
        /// <returns>Array of command information</returns>
        public CommandInfo[] GetRegisteredCommands()
        {
            return commands.Values.Select(cmd => new CommandInfo(cmd.CommandName, cmd.Description)).ToArray();
        }

        /// <summary>
        /// Check if specified command is registered
        /// </summary>
        /// <param name="commandName">Command name</param>
        /// <returns>True if registered</returns>
        public bool IsCommandRegistered(string commandName)
        {
            return commands.ContainsKey(commandName);
        }
    }

    /// <summary>
    /// Class representing command information
    /// </summary>
    public class CommandInfo
    {
        [JsonProperty("name")]
        public string Name { get; }
        
        [JsonProperty("description")]
        public string Description { get; }

        public CommandInfo(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }
} 