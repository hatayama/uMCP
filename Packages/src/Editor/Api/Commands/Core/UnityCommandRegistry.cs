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
                    try
                    {
                        // Find all types with McpTool attribute that implement IUnityCommand
                        Type[] types = assembly.GetTypes()
                            .Where(type => type.GetCustomAttribute<McpToolAttribute>() != null)
                            .Where(type => typeof(IUnityCommand).IsAssignableFrom(type))
                            .Where(type => !type.IsAbstract && !type.IsInterface)
                            .ToArray();
                        
                        commandTypes.AddRange(types);
                    }
                    catch (ReflectionTypeLoadException ex)
                    {
                        // Skip assemblies that can't be loaded
                        McpLogger.LogDebug($"Skipped assembly {assembly.FullName} due to type load exception: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        // Skip assemblies with other issues
                        McpLogger.LogDebug($"Skipped assembly {assembly.FullName} due to exception: {ex.Message}");
                    }
                }
                
                // Register commands
                foreach (Type type in commandTypes)
                {
                    try
                    {
                        IUnityCommand command = (IUnityCommand)Activator.CreateInstance(type);
                        RegisterCommand(command);
                        McpLogger.LogDebug($"Auto-registered command: {command.CommandName} (Type: {type.Name})");
                    }
                    catch (Exception ex)
                    {
                        McpLogger.LogError($"Failed to auto-register command {type.Name}: {ex.Message}");
                    }
                }
                
                McpLogger.LogInfo($"Auto-registered {commandTypes.Count} commands using McpTool attribute");
            }
            catch (Exception ex)
            {
                McpLogger.LogError($"Failed to register commands with attributes: {ex.Message}");
                // Fall back to manual registration
                RegisterManualCommands();
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
        /// Manually trigger commands changed notification
        /// Used for manual notifications and post-compilation notifications
        /// </summary>
        public static void TriggerCommandsChangedNotification()
        {
            McpLogger.LogDebug("Manually triggering commands changed notification");
            
            // Call the public method in McpServerController
            McpServerController.TriggerCommandChangeNotification();
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