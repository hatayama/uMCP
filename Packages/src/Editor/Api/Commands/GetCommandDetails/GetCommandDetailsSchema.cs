using System.ComponentModel;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Schema for GetCommandDetails command parameters
    /// Provides type-safe parameter access for retrieving command information
    /// </summary>
    public class GetCommandDetailsSchema : BaseCommandSchema
    {
        /// <summary>
        /// Whether to include development-only commands in the results
        /// </summary>
        [Description("Whether to include development-only commands in the results")]
        public bool IncludeDevelopmentOnly { get; set; } = false;
    }
} 