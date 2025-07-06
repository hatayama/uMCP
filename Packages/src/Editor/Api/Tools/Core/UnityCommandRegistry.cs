using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Unity MCP command registry class
    /// Simplified version that only supports static tool registry system
    /// </summary>
    public class UnityCommandRegistry
    {
        private readonly StaticToolRegistry staticToolRegistry = new StaticToolRegistry();

        /// <summary>
        /// Execute command
        /// </summary>
        /// <param name="commandName">Command name</param>
        /// <param name="paramsToken">Parameters</param>
        /// <returns>Execution result</returns>
        /// <exception cref="ArgumentException">When command is unknown</exception>
        public async Task<BaseCommandResponse> ExecuteCommandAsync(string commandName, JToken paramsToken)
        {
            if (!staticToolRegistry.IsToolRegistered(commandName))
            {
                throw new ArgumentException($"Unknown command: {commandName}");
            }

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

        /// <summary>
        /// Get list of registered command names
        /// </summary>
        /// <returns>Array of command names</returns>
        public string[] GetRegisteredCommandNames()
        {
            return staticToolRegistry.GetRegisteredToolNames();
        }

        /// <summary>
        /// Get detailed information of registered commands
        /// </summary>
        /// <returns>Array of command information</returns>
        public CommandInfo[] GetRegisteredCommands()
        {
            return staticToolRegistry.GetRegisteredTools().Select(tool =>
            {
                CommandParameterSchema parameterSchema = CreateParameterSchemaFromStaticTool(tool);
                return new CommandInfo(tool.Name, tool.Description, parameterSchema, false);
            }).ToArray();
        }

        /// <summary>
        /// Check if specified command is registered
        /// </summary>
        /// <param name="commandName">Command name</param>
        /// <returns>True if registered</returns>
        public bool IsCommandRegistered(string commandName)
        {
            return staticToolRegistry.IsToolRegistered(commandName);
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
        /// Register command (deprecated - use static tools instead)
        /// </summary>
        /// <param name="command">Command to register</param>
        public void RegisterCommand(IUnityCommand command)
        {
            throw new NotSupportedException("Legacy command registration is no longer supported. Please use static tools with [McpServerToolType] attribute instead.");
        }

        /// <summary>
        /// Unregister command (deprecated - use static tools instead)
        /// </summary>
        /// <param name="commandName">Name of command to unregister</param>
        public void UnregisterCommand(string commandName)
        {
            throw new NotSupportedException("Legacy command unregistration is no longer supported. Static tools are automatically discovered and cannot be unregistered.");
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
}