using System;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Base response class for all Unity MCP tool responses
    /// Provides common properties like execution timing information
    /// </summary>
    public abstract class BaseToolResponse
    {
        /// <summary>
        /// Tool execution start time (UTC)
        /// </summary>
        public string StartedAt { get; set; }

        /// <summary>
        /// Tool execution end time (UTC)
        /// </summary>
        public string EndedAt { get; set; }

        /// <summary>
        /// Tool execution duration in milliseconds
        /// </summary>
        public long ExecutionTimeMs { get; set; }

        /// <summary>
        /// Set timing information automatically
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