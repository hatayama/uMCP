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
    /// Also supports new static tool registry system
    /// </summary>
    public class UnityCommandRegistry
    {
        private readonly Dictionary<string, IUnityCommand> commands = new Dictionary<string, IUnityCommand>();
        private readonly StaticToolRegistry staticToolRegistry = new StaticToolRegistry();

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

            // PingCommand disabled - using PingTools static class instead
            // if (!IsCommandTypeRegistered<PingCommand>())
            // {
            //     RegisterCommand(new PingCommand());
            // }

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
            // First try static tools (new system)
            if (staticToolRegistry.IsToolRegistered(commandName))
            {
                DateTime startTime = DateTime.UtcNow;
                
                await MainThreadSwitcher.SwitchToMainThread();
                object result = await staticToolRegistry.ExecuteToolAsync(commandName, paramsToken);
                
                DateTime endTime = DateTime.UtcNow;
                
                // If result is already a BaseCommandResponse, return it directly
                if (result is BaseCommandResponse baseResponse)
                {
                    baseResponse.SetTimingInfo(startTime, endTime);
                    return baseResponse;
                }
                
                // Otherwise, wrap in StaticToolCommandResponse
                StaticToolCommandResponse response = new StaticToolCommandResponse(result);
                response.SetTimingInfo(startTime, endTime);
                return response;
            }

            // Fall back to legacy command system
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
            List<string> allCommands = new List<string>();
            allCommands.AddRange(commands.Keys);
            allCommands.AddRange(staticToolRegistry.GetRegisteredToolNames());
            return allCommands.ToArray();
        }

        /// <summary>
        /// Get detailed information of registered commands
        /// </summary>
        /// <returns>Array of command information</returns>
        public CommandInfo[] GetRegisteredCommands()
        {
            List<CommandInfo> allCommands = new List<CommandInfo>();
            
            // Add legacy commands
            allCommands.AddRange(commands.Values.Select(cmd => 
            {
                // Check if command has McpTool attribute with DisplayDevelopmentOnly
                bool displayDevelopmentOnly = false;
                McpToolAttribute attribute = cmd.GetType().GetCustomAttribute<McpToolAttribute>();
                if (attribute != null)
                {
                    displayDevelopmentOnly = attribute.DisplayDevelopmentOnly;
                }
                
                return new CommandInfo(cmd.CommandName, cmd.Description, cmd.ParameterSchema, displayDevelopmentOnly);
            }));
            
            // Add static tools
            allCommands.AddRange(staticToolRegistry.GetRegisteredTools().Select(tool =>
            {
                CommandParameterSchema parameterSchema = CreateParameterSchemaFromStaticTool(tool);
                return new CommandInfo(tool.Name, tool.Description, parameterSchema, false);
            }));
            
            return allCommands.ToArray();
        }

        /// <summary>
        /// Check if specified command is registered
        /// </summary>
        /// <param name="commandName">Command name</param>
        /// <returns>True if registered</returns>
        public bool IsCommandRegistered(string commandName)
        {
            return commands.ContainsKey(commandName) || staticToolRegistry.IsToolRegistered(commandName);
        }

        /// <summary>
        /// Create parameter schema from static tool info
        /// </summary>
        private CommandParameterSchema CreateParameterSchemaFromStaticTool(StaticToolInfo tool)
        {
            Dictionary<string, ParameterInfo> properties = new Dictionary<string, ParameterInfo>();
            List<string> required = new List<string>();
            
            foreach (StaticToolParameterInfo param in tool.Parameters)
            {
                ParameterInfo paramInfo = new ParameterInfo(
                    GetJsonTypeFromType(param.Type),
                    param.Description,
                    param.DefaultValue
                );
                
                properties[param.Name] = paramInfo;
                
                if (!param.IsOptional)
                {
                    required.Add(param.Name);
                }
            }
            
            return new CommandParameterSchema(properties, required.ToArray());
        }
        
        /// <summary>
        /// Get JSON type string from .NET type
        /// </summary>
        private string GetJsonTypeFromType(Type type)
        {
            if (type == typeof(string))
                return "string";
            if (type == typeof(int) || type == typeof(long) || type == typeof(short))
                return "integer";
            if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
                return "number";
            if (type == typeof(bool))
                return "boolean";
            if (type.IsArray || type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                return "array";
            
            return "object";
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
    /// Response wrapper for static tool results
    /// </summary>
    public class StaticToolCommandResponse : BaseCommandResponse
    {
        [JsonProperty("result")] public object Result { get; }

        public StaticToolCommandResponse(object result)
        {
            Result = result;
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