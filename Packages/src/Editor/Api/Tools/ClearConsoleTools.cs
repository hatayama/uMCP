using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Unity console clearing tools for MCP C# SDK format
    /// Related classes:
    /// - ClearConsoleCommand: Legacy command version (will be deprecated)
    /// - ClearConsoleSchema: Legacy schema (will be deprecated)
    /// - ClearConsoleResponse: Legacy response (will be deprecated)
    /// - ConsoleUtility: Core console operation logic
    /// - ClearedLogCounts: Log count breakdown structure
    /// </summary>
    [McpServerToolType]
    public static class ClearConsoleTools
    {
        /// <summary>
        /// Clear Unity console logs
        /// </summary>
        [McpServerTool(Name = "clear-console")]
        [Description("Clear Unity console logs")]
        public static Task<ClearConsoleToolResult> ClearConsole(
            [Description("Whether to add a confirmation log message after clearing")] 
            bool AddConfirmationMessage = true,
            CancellationToken cancellationToken = default)
        {
            // Get current log counts before clearing
            ConsoleUtility.GetConsoleLogCounts(out int errorCount, out int warningCount, out int logCount);
            int totalLogCount = errorCount + warningCount + logCount;
            
            ClearedLogCounts clearedCounts = new ClearedLogCounts(errorCount, warningCount, logCount);

            // Perform console clear operation
            ConsoleUtility.ClearConsole();

            // Add confirmation message if requested
            if (AddConfirmationMessage)
            {
                Debug.Log("=== Console cleared via MCP command ===");
            }

            // Create success response
            string message = totalLogCount > 0 
                ? $"Successfully cleared {totalLogCount} console logs (Errors: {errorCount}, Warnings: {warningCount}, Logs: {logCount})"
                : "Console was already empty";

            ClearConsoleToolResult result = new ClearConsoleToolResult(
                success: true,
                clearedLogCount: totalLogCount,
                clearedCounts: clearedCounts,
                message: message
            );

            return Task.FromResult(result);
        }
        
        /// <summary>
        /// Result for clear-console tool
        /// Compatible with legacy ClearConsoleResponse structure
        /// </summary>
        public class ClearConsoleToolResult : BaseCommandResponse
        {
            [Description("Whether the console clear operation was successful")]
            public bool Success { get; set; }
            
            [Description("Number of logs that were cleared from the console")]
            public int ClearedLogCount { get; set; }
            
            [Description("Breakdown of cleared logs by type")]
            public ClearedLogCounts ClearedCounts { get; set; }
            
            [Description("Message describing the clear operation result")]
            public string Message { get; set; }
            
            [Description("Error message if the operation failed")]
            public string ErrorMessage { get; set; }

            public ClearConsoleToolResult(bool success, int clearedLogCount, ClearedLogCounts clearedCounts, string message)
            {
                Success = success;
                ClearedLogCount = clearedLogCount;
                ClearedCounts = clearedCounts ?? new ClearedLogCounts(0, 0, 0);
                Message = message ?? string.Empty;
                ErrorMessage = string.Empty;
            }
            
            public ClearConsoleToolResult(string errorMessage)
            {
                Success = false;
                ClearedLogCount = 0;
                ClearedCounts = new ClearedLogCounts(0, 0, 0);
                Message = string.Empty;
                ErrorMessage = errorMessage ?? string.Empty;
            }
        }
    }
}