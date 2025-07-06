using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;

namespace io.github.hatayama.uMCP
{
    // Related classes:
    // - IUnityCommand: The interface that this class implements.
    // - UnityCommandRegistry: Registers and manages instances of command implementations.
    // - CommandParameterSchemaGenerator: Generates the JSON schema for command parameters.
    /// <summary>
    /// Abstract base class for type-safe Unity commands using Schema and Response types
    /// </summary>
    /// <typeparam name="TSchema">Schema type for command parameters</typeparam>
    /// <typeparam name="TResponse">Response type for command results</typeparam>
    public abstract class AbstractUnityCommand<TSchema, TResponse> : IUnityCommand
        where TSchema : BaseCommandSchema, new()
        where TResponse : BaseCommandResponse
    {
        public abstract string CommandName { get; }
        public abstract string Description { get; }

        /// <summary>
        /// Automatically generates parameter schema from TSchema type
        /// </summary>
        public virtual CommandParameterSchema ParameterSchema =>
            CommandParameterSchemaGenerator.FromDto<TSchema>();

        /// <summary>
        /// Execute command with type-safe Schema parameters
        /// Note: This method is called from the main thread context. 
        /// MainThreadSwitcher.SwitchToMainThread() is already handled by the upper layer (JsonRpcProcessor),
        /// so individual commands do not need to call it again.
        /// </summary>
        /// <param name="parameters">Strongly typed parameters</param>
        /// <returns>Strongly typed command execution result</returns>
        protected abstract Task<TResponse> ExecuteAsync(TSchema parameters);

        /// <summary>
        /// IUnityCommand implementation - converts JToken to Schema and returns BaseCommandResponse
        /// </summary>
        public async Task<BaseCommandResponse> ExecuteAsync(JToken paramsToken)
        {
            DateTime startTime = DateTime.UtcNow;

            try
            {
                // Convert JToken to strongly typed Schema
                TSchema parameters = ConvertToSchema(paramsToken);

                // Execute with type-safe parameters and get type-safe response
                TResponse response = await ExecuteAsync(parameters);

                DateTime endTime = DateTime.UtcNow;

                // Set timing information if response inherits from BaseCommandResponse
                if (response is BaseCommandResponse baseResponse)
                {
                    baseResponse.SetTimingInfo(startTime, endTime);
                }

                // Return as BaseCommandResponse for IUnityCommand interface compatibility
                return response;
            }
            catch (Exception ex)
            {
                McpLogger.LogError($"Error executing command {CommandName}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Convert JToken to strongly typed Schema with default value fallback
        /// </summary>
        private TSchema ConvertToSchema(JToken paramsToken)
        {
            if (paramsToken == null || paramsToken.Type == JTokenType.Null)
            {
                // Return default instance if no parameters provided
                return new TSchema();
            }
            
            // Create JsonSerializerSettings with CamelCasePropertyNamesContractResolver
            // This allows TypeScript side to use camelCase while C# uses PascalCase
            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
            };
            
            // Create JsonSerializer with custom settings
            JsonSerializer serializer = JsonSerializer.Create(settings);
            
            // Try to deserialize from JToken with custom serializer
            TSchema schema = paramsToken.ToObject<TSchema>(serializer);

            // If deserialization returns null, create default instance
            if (schema == null)
            {
                schema = new TSchema();
            }

            // Apply default values for null properties
            return ApplyDefaultValues(schema);
        }

        /// <summary>
        /// Apply default values to Schema properties if they are null
        /// Override this method to provide custom default value logic
        /// </summary>
        protected virtual TSchema ApplyDefaultValues(TSchema schema)
        {
            // Default implementation - return as is
            // Subclasses can override to apply specific default values
            return schema;
        }
    }
}