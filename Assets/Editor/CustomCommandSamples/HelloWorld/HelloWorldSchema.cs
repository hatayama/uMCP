using System.ComponentModel;
using Newtonsoft.Json;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Supported languages for greeting
    /// </summary>
    public enum GreetingLanguage
    {
        english,
        japanese,
        spanish,
        french
    }

    /// <summary>
    /// Schema for HelloWorld command parameters
    /// Provides type-safe parameter access with immutable design
    /// Related classes:
    /// - BaseCommandSchema: Provides base timeout functionality
    /// - HelloWorldCommand: Uses this schema for greeting parameters
    /// </summary>
    public class HelloWorldSchema : BaseCommandSchema
    {
        /// <summary>
        /// Name to greet
        /// </summary>
        [Description("Name to greet")]
        public string Name { get; }

        /// <summary>
        /// Language for greeting
        /// </summary>
        [Description("Language for greeting")]
        public GreetingLanguage Language { get; }

        /// <summary>
        /// Whether to include timestamp in response
        /// </summary>
        [Description("Whether to include timestamp in response")]
        public bool IncludeTimestamp { get; }

        /// <summary>
        /// Create HelloWorldSchema with all parameters
        /// </summary>
        [JsonConstructor]
        public HelloWorldSchema(string name = "World", GreetingLanguage language = GreetingLanguage.english, bool includeTimestamp = true, int timeoutSeconds = 10)
            : base(timeoutSeconds)
        {
            Name = name ?? "World";
            Language = language;
            IncludeTimestamp = includeTimestamp;
        }

        /// <summary>
        /// Parameterless constructor for new() constraint compatibility
        /// </summary>
        public HelloWorldSchema() : this("World", GreetingLanguage.english, true, 10)
        {
        }

        /// <summary>
        /// Get language as string value for greeting generation
        /// </summary>
        public string GetLanguageString()
        {
            return Language.ToString();
        }
    }
} 