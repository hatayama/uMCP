using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
            // Register commands with attribute-based discovery
            RegisterCommandsWithAttributes();

            // Manual registration for commands without attributes (for backward compatibility)
            RegisterManualCommands();
        }

        /// <summary>
        /// Register commands using attribute-based discovery
        /// </summary>
        private void RegisterCommandsWithAttributes()
        {
            try
            {
                // Get all assemblies in the current domain
                Assembly[] assemblies = System.AppDomain.CurrentDomain.GetAssemblies();

                List<Type> commandTypes = new List<Type>();

                foreach (Assembly assembly in assemblies)
                {
                    // Find all types with McpTool attribute that implement IUnityCommand
                    Type[] types = assembly.GetTypes()
                        .Where(type => type.GetCustomAttribute<McpToolAttribute>() != null)
                        .Where(type => typeof(IUnityCommand).IsAssignableFrom(type))
                        .Where(type => !type.IsAbstract && !type.IsInterface)
                        .ToArray();

                    commandTypes.AddRange(types);
                }

                // Register all commands - filtering will be handled by TypeScript side
                foreach (Type type in commandTypes)
                {
                    IUnityCommand command = (IUnityCommand)Activator.CreateInstance(type);
                    RegisterCommand(command);
                }
            }
            catch (Exception ex)
            {
                McpLogger.LogError($"Failed to register commands with attributes: {ex.Message}");
                // Fall back to manual registration
                RegisterManualCommands();
                throw;
            }
        }


        /// <summary>
        /// Register commands manually (for backward compatibility)
        /// </summary>
        private void RegisterManualCommands()
        {
            // Only register commands that don't have the McpTool attribute
            // This prevents double registration

            if (!IsCommandTypeRegistered<PingCommand>())
            {
                RegisterCommand(new PingCommand());
            }

            if (!IsCommandTypeRegistered<CompileCommand>())
            {
                RegisterCommand(new CompileCommand());
            }

            if (!IsCommandTypeRegistered<GetLogsCommand>())
            {
                RegisterCommand(new GetLogsCommand());
            }

            if (!IsCommandTypeRegistered<RunTestsCommand>())
            {
                RegisterCommand(new RunTestsCommand());
            }
        }

        /// <summary>
        /// Check if a command type is already registered
        /// </summary>
        private bool IsCommandTypeRegistered<T>() where T : IUnityCommand
        {
            return commands.Values.Any(cmd => cmd.GetType() == typeof(T));
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
        }

        /// <summary>
        /// Unregister command
        /// </summary>
        /// <param name="commandName">Name of command to unregister</param>
        public void UnregisterCommand(string commandName)
        {
            commands.Remove(commandName);
        }

        /// <summary>
        /// Execute command
        /// </summary>
        /// <param name="commandName">Command name</param>
        /// <param name="paramsToken">Parameters</param>
        /// <returns>Execution result</returns>
        /// <exception cref="ArgumentException">When command is unknown</exception>
        public async Task<BaseCommandResponse> ExecuteCommandAsync(string commandName, JToken paramsToken)
        {
            if (!commands.TryGetValue(commandName, out IUnityCommand command))
            {
                throw new ArgumentException($"Unknown command: {commandName}");
            }

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
            var result = commands.Values.Select(cmd => 
            {
                // Check if command has McpTool attribute with DisplayDevelopmentOnly
                bool displayDevelopmentOnly = false;
                McpToolAttribute attribute = cmd.GetType().GetCustomAttribute<McpToolAttribute>();
                if (attribute != null)
                {
                    displayDevelopmentOnly = attribute.DisplayDevelopmentOnly;
                }
                
                // Debug logging
                UnityEngine.Debug.Log($"[DEBUG] Command: {cmd.CommandName}, displayDevelopmentOnly: {displayDevelopmentOnly}");
                
                return new CommandInfo(cmd.CommandName, cmd.Description, cmd.ParameterSchema, displayDevelopmentOnly);
            }).ToArray();
            
            UnityEngine.Debug.Log($"[DEBUG] Total registered commands: {result.Length}");
            return result;
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
        /// Manually trigger commands changed notification
        /// Used for manual notifications and post-compilation notifications
        /// </summary>
        public static void TriggerCommandsChangedNotification()
        {
            // Call the public method in McpServerController
            McpServerController.TriggerCommandChangeNotification();
        }
    }

    /// <summary>
    /// Class representing command information
    /// </summary>
    public class CommandInfo
    {
        [JsonProperty("name")] public string Name { get; }

        [JsonProperty("description")] public string Description { get; }

        [JsonProperty("parameterSchema")] public CommandParameterSchema ParameterSchema { get; }

        [JsonProperty("displayDevelopmentOnly")] public bool DisplayDevelopmentOnly { get; }

        public CommandInfo(string name, string description, CommandParameterSchema parameterSchema, bool displayDevelopmentOnly = false)
        {
            Name = name;
            Description = description;
            ParameterSchema = parameterSchema;
            DisplayDevelopmentOnly = displayDevelopmentOnly;
        }
    }
}