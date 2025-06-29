using System.ComponentModel;
using Newtonsoft.Json;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Schema for Compile command parameters
    /// Provides type-safe parameter access with immutable design
    /// Related classes:
    /// - BaseCommandSchema: Provides base timeout functionality
    /// - CompileCommand: Uses this schema for compilation parameters
    /// </summary>
    public class CompileSchema : BaseCommandSchema
    {
        /// <summary>
        /// Whether to perform forced recompilation
        /// </summary>
        [Description("Whether to perform forced recompilation")]
        public bool ForceRecompile { get; }

        /// <summary>
        /// Create CompileSchema with all parameters
        /// </summary>
        [JsonConstructor]
        public CompileSchema(bool forceRecompile = false, int timeoutSeconds = 10)
            : base(timeoutSeconds)
        {
            ForceRecompile = forceRecompile;
        }

        /// <summary>
        /// Parameterless constructor for new() constraint compatibility
        /// </summary>
        public CompileSchema() : this(false, 10)
        {
        }
    }
} 