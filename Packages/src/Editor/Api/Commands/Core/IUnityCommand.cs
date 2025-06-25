using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Constants for parameter schema property names
    /// Ensures consistency between Unity and TypeScript sides
    /// </summary>
    public static class ParameterSchemaConstants
    {
        public const string TYPE_PROPERTY = "Type";
        public const string DESCRIPTION_PROPERTY = "Description";
        public const string DEFAULT_VALUE_PROPERTY = "DefaultValue";
        public const string ENUM_PROPERTY = "Enum";
        public const string PROPERTIES_PROPERTY = "Properties";
        public const string REQUIRED_PROPERTY = "Required";
    }

    /// <summary>
    /// Base interface for Unity MCP command handlers
    /// Following the Open-Closed principle, when adding new commands,
    /// create a new class that implements this interface
    /// </summary>
    public interface IUnityCommand
    {
        /// <summary>
        /// Get command name
        /// </summary>
        string CommandName { get; }
        
        /// <summary>
        /// Get command description
        /// </summary>
        string Description { get; }
        
        /// <summary>
        /// Get parameter schema information for TypeScript side
        /// </summary>
        CommandParameterSchema ParameterSchema { get; }
        
        /// <summary>
        /// Execute command
        /// </summary>
        /// <param name="paramsToken">JSON token for parameters</param>
        /// <returns>Execution result</returns>
        Task<BaseCommandResponse> ExecuteAsync(JToken paramsToken);
    }
    
    /// <summary>
    /// Parameter schema definition for commands
    /// </summary>
    public class CommandParameterSchema
    {
        public Dictionary<string, ParameterInfo> Properties { get; }
        public string[] Required { get; }
        
        public CommandParameterSchema(Dictionary<string, ParameterInfo> properties = null, string[] required = null)
        {
            Properties = properties ?? new Dictionary<string, ParameterInfo>();
            Required = required ?? new string[0];
        }
    }
    
    /// <summary>
    /// Individual parameter information
    /// </summary>
    public class ParameterInfo
    {
        public string Type { get; }
        public string Description { get; }
        public object DefaultValue { get; }
        public string[] Enum { get; }
        
        public ParameterInfo(string type, string description, object defaultValue = null, string[] enumValues = null)
        {
            Type = type;
            Description = description;
            DefaultValue = defaultValue;
            Enum = enumValues;
        }
    }
} 