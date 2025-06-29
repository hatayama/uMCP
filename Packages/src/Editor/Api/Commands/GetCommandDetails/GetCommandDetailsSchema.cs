using System.ComponentModel;
using Newtonsoft.Json;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Schema for GetCommandDetails command parameters
    /// Provides type-safe parameter access with immutable design
    /// Related classes:
    /// - BaseCommandSchema: Provides base timeout functionality
    /// - GetCommandDetailsCommand: Uses this schema for command details parameters
    /// </summary>
    public class GetCommandDetailsSchema : BaseCommandSchema
    {
        /// <summary>
        /// Whether to include development-only commands in the results
        /// </summary>
        [Description("Whether to include development-only commands in the results")]
        public bool IncludeDevelopmentOnly { get; }

        /// <summary>
        /// Create GetCommandDetailsSchema with all parameters
        /// </summary>
        [JsonConstructor]
        public GetCommandDetailsSchema(bool includeDevelopmentOnly = false, int timeoutSeconds = 10)
            : base(timeoutSeconds)
        {
            IncludeDevelopmentOnly = includeDevelopmentOnly;
        }

        /// <summary>
        /// Parameterless constructor for new() constraint compatibility
        /// </summary>
        public GetCommandDetailsSchema() : this(false, 10)
        {
        }
    }
} 