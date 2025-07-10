using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace io.github.hatayama.uMCP
{
    // Related classes:
    // - UnityCommandExecutor: Uses this registry to execute commands.
    // - IUnityCommand: The interface for all commands stored in this registry.
    // - AbstractUnityCommand: The base class for most command implementations.
    // - McpToolAttribute: Attribute used to discover and register commands automatically.
    /// <summary>
    /// Unity MCP command registry class
    /// Supports dynamic command registration, allowing users to add their own commands
    /// </summary>
    public class UnityCommandRegistry
    {
        private readonly Dictionary<string, IUnityCommand> commands = new Dictionary<string, IUnityCommand>();

        /// <summary>
        /// Singleton instance for global access
        /// </summary>
        public static UnityCommandRegistry Instance { get; private set; }

        /// <summary>
        /// Default constructor
        /// Auto-registers standard commands
        /// </summary>
        public UnityCommandRegistry()
        {
            Instance = this;
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
                    // Security: Validate type before creating instance
                    if (!IsValidCommandType(type))
                    {
                        UnityEngine.Debug.LogWarning($"{McpConstants.SECURITY_LOG_PREFIX} Skipping invalid command type: {type.FullName}");
                        continue;
                    }
                    
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
        /// Security: Validate if the type is safe to instantiate
        /// </summary>
        private bool IsValidCommandType(Type type)
        {
            try
            {
                // Must implement IUnityCommand
                if (!typeof(IUnityCommand).IsAssignableFrom(type))
                {
                    return false;
                }
                
                // Must not be abstract or interface
                if (type.IsAbstract || type.IsInterface)
                {
                    return false;
                }
                
                // Must have McpTool attribute
                if (type.GetCustomAttribute<McpToolAttribute>() == null)
                {
                    return false;
                }
                
                // Must be in uMCP namespace (security restriction)
                if (!type.Namespace?.StartsWith(McpConstants.UMCP_NAMESPACE_PREFIX) == true)
                {
                    return false;
                }
                
                // Must have parameterless constructor
                if (type.GetConstructor(Type.EmptyTypes) == null)
                {
                    return false;
                }
                
                return true;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"{McpConstants.SECURITY_LOG_PREFIX} Error validating command type {type?.FullName}: {ex.Message}");
                return false;
            }
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
        /// <exception cref="McpSecurityException">When command is blocked by security settings</exception>
        public async Task<BaseCommandResponse> ExecuteCommandAsync(string commandName, JToken paramsToken)
        {
            if (!commands.TryGetValue(commandName, out IUnityCommand command))
            {
                throw new ArgumentException($"Unknown command: {commandName}");
            }

            // Security check - validate command before execution
            McpSecurityChecker.ValidateCommand(commandName, throwOnBlock: true);

            await MainThreadSwitcher.SwitchToMainThread();
            return await command.ExecuteAsync(paramsToken);
        }


        /// <summary>
        /// Get detailed information of registered commands
        /// </summary>
        /// <returns>Array of command information</returns>
        public CommandInfo[] GetRegisteredCommands()
        {
            return commands.Values.Select(cmd => 
            {
                // Check if command has McpTool attribute with DisplayDevelopmentOnly
                bool displayDevelopmentOnly = false;
                McpToolAttribute attribute = cmd.GetType().GetCustomAttribute<McpToolAttribute>();
                if (attribute != null)
                {
                    displayDevelopmentOnly = attribute.DisplayDevelopmentOnly;
                }
                
                // Check security settings
                CommandSecurityInfo securityInfo = McpSecurityChecker.GetCommandSecurityInfo(cmd.CommandName);
                string description = cmd.Description;
                
                // Modify description for blocked commands
                if (!securityInfo.IsAllowed)
                {
                    description = $"[BLOCKED] {description} - {securityInfo.Reason}";
                }
                
                return new CommandInfo(cmd.CommandName, description, cmd.ParameterSchema, displayDevelopmentOnly);
            }).ToArray();
        }

        /// <summary>
        /// Get command type by name for security checking
        /// </summary>
        /// <param name="commandName">Command name</param>
        /// <returns>Command type or null if not found</returns>
        public Type GetCommandType(string commandName)
        {
            if (commands.TryGetValue(commandName, out IUnityCommand command))
            {
                return command.GetType();
            }
            return null;
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