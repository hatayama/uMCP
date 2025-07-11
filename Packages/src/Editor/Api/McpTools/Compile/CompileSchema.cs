using System.ComponentModel;

namespace io.github.hatayama.uLoopMCP
{
    /// <summary>
    /// Schema for Compile command parameters
    /// Provides type-safe parameter access with default values
    /// </summary>
    public class CompileSchema : BaseToolSchema
    {
        /// <summary>
        /// Whether to perform forced recompilation
        /// </summary>
        [Description("Whether to perform forced recompilation")]
        public bool ForceRecompile { get; set; } = false;
    }
} 