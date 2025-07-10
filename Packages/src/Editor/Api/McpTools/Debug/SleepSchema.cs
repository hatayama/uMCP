using System.ComponentModel;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Schema for debug sleep tool parameters
    /// Related classes:
    /// - SleepTool: Implementation of the debug sleep tool
    /// - SleepResponse: Response structure for the sleep tool
    /// - BaseToolSchema: Base class providing TimeoutSeconds property
    /// </summary>
    public class SleepSchema : BaseToolSchema
    {
        /// <summary>
        /// Number of seconds to sleep for testing purposes (default: 15)
        /// </summary>
        [Description("Number of seconds to sleep for testing purposes (default: 15)")]
        public int SleepSeconds { get; set; } = 15;
        
        /// <summary>
        /// Override timeout for debug purposes (default: 10 seconds)
        /// This allows testing timeout scenarios with short timeout values
        /// </summary>
        [Description("Timeout for tool execution in seconds (default: 10 seconds for debug)")]
        public override int TimeoutSeconds { get; set; } = 10;
    }
}