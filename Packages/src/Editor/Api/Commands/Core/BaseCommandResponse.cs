using System;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Base response class for all Unity MCP command responses
    /// Provides common properties like execution timing information
    /// Related classes:
    /// - AbstractUnityCommand: Sets timing information via SetTimingInfo
    /// - All concrete response classes: Inherit from this base class
    /// </summary>
    public abstract class BaseCommandResponse
    {
        /// <summary>
        /// Command execution start time (UTC)
        /// </summary>
        public string StartedAt { get; private set; }

        /// <summary>
        /// Command execution end time (UTC)
        /// </summary>
        public string EndedAt { get; private set; }

        /// <summary>
        /// Command execution duration in milliseconds
        /// </summary>
        public long ExecutionTimeMs { get; private set; }

        /// <summary>
        /// Set timing information automatically
        /// This method is called by AbstractUnityCommand after command execution
        /// </summary>
        public void SetTimingInfo(DateTime startTime, DateTime endTime)
        {
            // Convert UTC to local time for display
            DateTime localStartTime = startTime.ToLocalTime();
            DateTime localEndTime = endTime.ToLocalTime();
            
            StartedAt = localStartTime.ToString("yyyy-MM-ddTHH:mm:ss.fff");
            EndedAt = localEndTime.ToString("yyyy-MM-ddTHH:mm:ss.fff");
            ExecutionTimeMs = (long)(endTime - startTime).TotalMilliseconds;
        }
    }
} 