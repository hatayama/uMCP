using System.ComponentModel;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Schema for Compile command parameters
    /// Provides type-safe parameter access with default values
    /// </summary>
    public class CompileSchema : BaseCommandSchema
    {
        /// <summary>
        /// Whether to perform forced recompilation
        /// </summary>
        [Description("Whether to perform forced recompilation")]
        public bool ForceRecompile { get; set; } = false;
    }
} 