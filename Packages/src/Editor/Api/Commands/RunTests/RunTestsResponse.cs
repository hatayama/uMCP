using System;
using Newtonsoft.Json;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Response schema for RunTests command
    /// Provides type-safe response structure for Unity test execution results
    /// Related classes:
    /// - BaseCommandResponse: Provides timing information
    /// - RunTestsCommand: Creates instances of this response
    /// </summary>
    public class RunTestsResponse : BaseCommandResponse
    {
        /// <summary>
        /// Whether test execution was successful
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// Test execution result message
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Test completion timestamp
        /// </summary>
        public string CompletedAt { get; }

        /// <summary>
        /// Total number of tests executed
        /// </summary>
        public int TestCount { get; }

        /// <summary>
        /// Number of tests that passed
        /// </summary>
        public int PassedCount { get; }

        /// <summary>
        /// Number of tests that failed
        /// </summary>
        public int FailedCount { get; }

        /// <summary>
        /// Number of tests that were skipped
        /// </summary>
        public int SkippedCount { get; }

        /// <summary>
        /// Path to XML test results file (if SaveXml was enabled)
        /// </summary>
        public string XmlPath { get; }

        /// <summary>
        /// Create a new RunTestsResponse
        /// </summary>
        [JsonConstructor]
        public RunTestsResponse(bool success, string message, string completedAt, int testCount, 
                               int passedCount, int failedCount, int skippedCount, string xmlPath)
        {
            Success = success;
            Message = message ?? string.Empty;
            CompletedAt = completedAt ?? string.Empty;
            TestCount = testCount;
            PassedCount = passedCount;
            FailedCount = failedCount;
            SkippedCount = skippedCount;
            XmlPath = xmlPath ?? string.Empty;
        }
    }
} 