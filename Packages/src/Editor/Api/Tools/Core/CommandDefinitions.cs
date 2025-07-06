using System.Collections.Generic;
using Newtonsoft.Json;

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

    /// <summary>
    /// Parameter schema definition for commands (immutable)
    /// </summary>
    public class CommandParameterSchema
    {
        public readonly Dictionary<string, ParameterInfo> Properties;
        public readonly string[] Required;
        
        public CommandParameterSchema(Dictionary<string, ParameterInfo> properties = null, string[] required = null)
        {
            Properties = properties ?? new Dictionary<string, ParameterInfo>();
            Required = required ?? new string[0];
        }
    }
    
    /// <summary>
    /// Individual parameter information (immutable)
    /// </summary>
    public class ParameterInfo
    {
        public readonly string Type;
        public readonly string Description;
        public readonly object DefaultValue;
        public readonly string[] Enum;
        
        public ParameterInfo(string type, string description, object defaultValue = null, string[] enumValues = null)
        {
            Type = type;
            Description = description;
            DefaultValue = defaultValue;
            Enum = enumValues;
        }
    }
}