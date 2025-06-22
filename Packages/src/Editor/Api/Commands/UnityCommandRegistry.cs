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
        /// Event triggered when commands are changed (added/removed)
        /// </summary>
        public static event Action OnCommandsChanged;

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

            bool isNewCommand = !commands.ContainsKey(command.CommandName);
            commands[command.CommandName] = command;
            McpLogger.LogDebug($"Command registered: {command.CommandName}");
            
            // Trigger event notification if this is a new command
            if (isNewCommand)
            {
                NotifyCommandsChanged();
            }
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
                NotifyCommandsChanged();
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
            return commands.Values.Select(cmd => new CommandInfo(cmd.CommandName, cmd.Description, cmd.ParameterSchema)).ToArray();
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
        
        /// <summary>
        /// Notify TypeScript side that commands have changed
        /// </summary>
        private void NotifyCommandsChanged()
        {
            McpLogger.LogDebug("Commands changed, notifying TypeScript side...");
            McpLogger.LogInfo($"[DEBUG] NotifyCommandsChanged called - OnCommandsChanged subscribers: {OnCommandsChanged?.GetInvocationList()?.Length ?? 0}");
            OnCommandsChanged?.Invoke();
            McpLogger.LogInfo("[DEBUG] NotifyCommandsChanged completed - event invoked");
        }
        
        /// <summary>
        /// Manually trigger commands changed notification
        /// Useful for external triggers like compilation completion
        /// </summary>
        public static void TriggerCommandsChangedNotification()
        {
            McpLogger.LogDebug("Manually triggering commands changed notification");
            McpLogger.LogInfo($"[DEBUG] TriggerCommandsChangedNotification called - OnCommandsChanged subscribers: {OnCommandsChanged?.GetInvocationList()?.Length ?? 0}");
            OnCommandsChanged?.Invoke();
            McpLogger.LogInfo("[DEBUG] TriggerCommandsChangedNotification completed - event invoked");
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
        
        [JsonProperty("parameterSchema")]
        public CommandParameterSchema ParameterSchema { get; }

        public CommandInfo(string name, string description, CommandParameterSchema parameterSchema)
        {
            Name = name;
            Description = description;
            ParameterSchema = parameterSchema;
        }
    }
} 