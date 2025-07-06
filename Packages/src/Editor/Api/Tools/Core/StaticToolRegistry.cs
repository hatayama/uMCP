using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Static tool registry for MCP C# SDK format tools
    /// Related classes:
    /// - McpServerToolTypeAttribute: Marks classes as tool containers
    /// - McpServerToolAttribute: Marks methods as tools
    /// - UnityCommandRegistry: Legacy command registry (will be replaced)
    /// - StaticToolExecutor: Executes tools from this registry
    /// </summary>
    public class StaticToolRegistry
    {
        private readonly Dictionary<string, StaticToolInfo> tools = new Dictionary<string, StaticToolInfo>();

        /// <summary>
        /// Default constructor
        /// Auto-registers all static tools
        /// </summary>
        public StaticToolRegistry()
        {
            RegisterStaticTools();
        }

        /// <summary>
        /// Register all static tools using reflection
        /// </summary>
        private void RegisterStaticTools()
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (Assembly assembly in assemblies)
            {
                Type[] toolTypes = assembly.GetTypes()
                    .Where(type => type.IsClass && type.IsSealed && type.IsAbstract) // static class
                    .Where(type => type.GetCustomAttribute<McpServerToolTypeAttribute>() != null)
                    .ToArray();

                foreach (Type toolType in toolTypes)
                {
                    RegisterToolsFromType(toolType);
                }
            }
        }

        /// <summary>
        /// Register tools from a specific type
        /// </summary>
        private void RegisterToolsFromType(Type toolType)
        {
            MethodInfo[] methods = toolType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(method => method.GetCustomAttribute<McpServerToolAttribute>() != null)
                .ToArray();

            foreach (MethodInfo method in methods)
            {
                RegisterTool(method);
            }
        }

        /// <summary>
        /// Register a single tool method
        /// </summary>
        private void RegisterTool(MethodInfo method)
        {
            McpServerToolAttribute toolAttribute = method.GetCustomAttribute<McpServerToolAttribute>();
            string toolName = toolAttribute?.Name ?? ConvertToKebabCase(method.Name);
            int timeoutMs = toolAttribute?.TimeoutMs ?? 120000; // Default 2 minutes
            

            StaticToolInfo toolInfo = new StaticToolInfo(
                name: toolName,
                method: method,
                description: GetMethodDescription(method),
                parameters: GetMethodParameters(method),
                timeoutMs: timeoutMs
            );

            tools[toolName] = toolInfo;
        }

        /// <summary>
        /// Get method description from Description attribute
        /// </summary>
        private string GetMethodDescription(MethodInfo method)
        {
            DescriptionAttribute descriptionAttribute = method.GetCustomAttribute<DescriptionAttribute>();
            return descriptionAttribute?.Description ?? method.Name;
        }

        /// <summary>
        /// Get method parameters information
        /// </summary>
        private StaticToolParameterInfo[] GetMethodParameters(MethodInfo method)
        {
            return method.GetParameters()
                .Where(param => param.ParameterType != typeof(System.Threading.CancellationToken))
                .Select(param => new StaticToolParameterInfo(
                    name: param.Name,
                    type: param.ParameterType,
                    description: param.GetCustomAttribute<DescriptionAttribute>()?.Description ?? param.Name,
                    isOptional: param.HasDefaultValue,
                    defaultValue: param.HasDefaultValue ? param.DefaultValue : null
                ))
                .ToArray();
        }

        /// <summary>
        /// Convert method name to kebab-case
        /// </summary>
        private string ConvertToKebabCase(string methodName)
        {
            return string.Concat(methodName.Select((c, i) => i > 0 && char.IsUpper(c) ? "-" + char.ToLower(c) : char.ToLower(c).ToString()));
        }

        /// <summary>
        /// Execute a static tool with optional timeout
        /// </summary>
        public async Task<object> ExecuteToolAsync(string toolName, JToken paramsToken, int? timeoutMs = null)
        {
            if (!tools.TryGetValue(toolName, out StaticToolInfo toolInfo))
            {
                throw new ArgumentException($"Unknown tool: {toolName}");
            }

            // Use tool-specific timeout if available, otherwise use provided timeout, otherwise use default
            int effectiveTimeout = timeoutMs ?? toolInfo.TimeoutMs;
            
            object[] parameters = PrepareParameters(toolInfo, paramsToken);
            object result = toolInfo.Method.Invoke(null, parameters);
            
            if (result is Task task)
            {
                // Apply timeout if specified
                if (effectiveTimeout > 0)
                {
                    using (var cts = new System.Threading.CancellationTokenSource(effectiveTimeout))
                    {
                        try
                        {
                            await task.ConfigureAwait(false);
                        }
                        catch (System.Threading.Tasks.TaskCanceledException)
                        {
                            throw new TimeoutException($"Tool '{toolName}' execution timed out after {effectiveTimeout}ms");
                        }
                    }
                }
                else
                {
                    await task;
                }
                
                // Get result from Task<T>
                Type taskType = task.GetType();
                if (taskType.IsGenericType)
                {
                    PropertyInfo resultProperty = taskType.GetProperty("Result");
                    object taskResult = resultProperty?.GetValue(task);
                    return taskResult;
                }
                
                return null;
            }
            
            return result;
        }

        /// <summary>
        /// Prepare parameters for method invocation
        /// </summary>
        private object[] PrepareParameters(StaticToolInfo toolInfo, JToken paramsToken)
        {
            List<object> parameters = new List<object>();
            
            JObject paramsObject = paramsToken as JObject ?? new JObject();
            
            foreach (StaticToolParameterInfo param in toolInfo.Parameters)
            {
                object value = GetParameterValue(param, paramsObject);
                parameters.Add(value);
            }
            
            // Add CancellationToken if method expects it
            if (toolInfo.Method.GetParameters().Any(p => p.ParameterType == typeof(System.Threading.CancellationToken)))
            {
                parameters.Add(System.Threading.CancellationToken.None);
            }
            
            return parameters.ToArray();
        }

        /// <summary>
        /// Get parameter value from JSON params
        /// </summary>
        private object GetParameterValue(StaticToolParameterInfo param, JObject paramsObject)
        {
            // Try exact match first
            if (paramsObject.TryGetValue(param.Name, out JToken paramToken))
            {
                return paramToken.ToObject(param.Type);
            }
            
            // Try case-insensitive search for parameter
            // This handles mismatched naming conventions between TypeScript and C#
            JProperty matchingProperty = paramsObject.Properties()
                .FirstOrDefault(p => string.Equals(p.Name, param.Name, StringComparison.OrdinalIgnoreCase));
            
            if (matchingProperty != null)
            {
                return matchingProperty.Value.ToObject(param.Type);
            }
            
            if (param.IsOptional)
            {
                return param.DefaultValue;
            }
            
            if (param.Type.IsValueType)
            {
                return Activator.CreateInstance(param.Type);
            }
            
            return null;
        }

        /// <summary>
        /// Get list of registered tool names
        /// </summary>
        public string[] GetRegisteredToolNames()
        {
            return tools.Keys.ToArray();
        }

        /// <summary>
        /// Get detailed information of registered tools
        /// </summary>
        public StaticToolInfo[] GetRegisteredTools()
        {
            return tools.Values.ToArray();
        }

        /// <summary>
        /// Check if specified tool is registered
        /// </summary>
        public bool IsToolRegistered(string toolName)
        {
            return tools.ContainsKey(toolName);
        }
    }

    /// <summary>
    /// Information about a static tool
    /// </summary>
    public class StaticToolInfo
    {
        public string Name { get; }
        public MethodInfo Method { get; }
        public string Description { get; }
        public StaticToolParameterInfo[] Parameters { get; }
        public int TimeoutMs { get; }

        public StaticToolInfo(string name, MethodInfo method, string description, StaticToolParameterInfo[] parameters, int timeoutMs = 120000)
        {
            Name = name;
            Method = method;
            Description = description;
            Parameters = parameters;
            TimeoutMs = timeoutMs;
        }
    }

    /// <summary>
    /// Information about a static tool parameter
    /// </summary>
    public class StaticToolParameterInfo
    {
        public string Name { get; }
        public Type Type { get; }
        public string Description { get; }
        public bool IsOptional { get; }
        public object DefaultValue { get; }

        public StaticToolParameterInfo(string name, Type type, string description, bool isOptional, object defaultValue)
        {
            Name = name;
            Type = type;
            Description = description;
            IsOptional = isOptional;
            DefaultValue = defaultValue;
        }
    }
}