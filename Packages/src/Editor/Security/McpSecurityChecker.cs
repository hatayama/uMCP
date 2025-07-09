using System;
using System.Reflection;
using UnityEngine;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Security checker for Unity MCP commands - provides runtime command blocking based on user settings
    /// 
    /// Design document reference: Packages/src/Editor/Security/SECURITY.md
    /// 
    /// Related classes:
    /// - McpEditorSettings: Persistent security settings storage
    /// - McpSecurityException: Custom exception for security violations
    /// - UnityCommandRegistry: Command registration and execution
    /// - McpBridgeServer: Server that executes commands
    /// </summary>
    public static class McpSecurityChecker
    {
        // Security setting constants
        private const string EnableTestsExecutionSetting = "enableTestsExecution";
        private const string AllowMenuItemExecutionSetting = "allowMenuItemExecution";
        /// <summary>
        /// Checks if a command is allowed based on current security settings
        /// </summary>
        /// <param name="commandName">The name of the command to check</param>
        /// <returns>True if command is allowed, false otherwise</returns>
        public static bool IsCommandAllowed(string commandName)
        {
            if (string.IsNullOrEmpty(commandName))
            {
                return false;
            }

            // Get command info from registry
            var commandInfo = GetCommandSecurityInfoFromRegistry(commandName);
            
            if (!commandInfo.HasValue)
            {
                // Unknown command - default to block for security
                McpLogger.LogWarning($"Unknown command '{commandName}' - blocking by default for security. Consider adding security rules.");
                return false;
            }

            // Check if command requires specific permission
            return IsCommandAllowedByAttribute(commandInfo.Value);
        }

        /// <summary>
        /// Gets command security info from the command registry
        /// </summary>
        /// <param name="commandName">The name of the command</param>
        /// <returns>Command security info or null if not found</returns>
        private static CommandAttributeInfo? GetCommandSecurityInfoFromRegistry(string commandName)
        {
            // Get the command registry instance
            var registry = UnityCommandRegistry.Instance;
            if (registry == null)
            {
                return null;
            }

            // Find the command and get its attribute
            var commandType = registry.GetCommandType(commandName);
            if (commandType == null)
            {
                return null;
            }

            var attribute = commandType.GetCustomAttribute<McpToolAttribute>();
            if (attribute == null)
            {
                return new CommandAttributeInfo(commandName, SecurityRiskLevel.High, null);
            }

            return new CommandAttributeInfo(commandName, attribute.SecurityLevel, attribute.RequiredSecuritySetting);
        }

        /// <summary>
        /// Checks if command is allowed based on its attribute settings
        /// </summary>
        /// <param name="commandInfo">Command attribute information</param>
        /// <returns>True if command is allowed</returns>
        private static bool IsCommandAllowedByAttribute(CommandAttributeInfo? commandInfo)
        {
            // Check by security level first
            switch (commandInfo.Value.SecurityLevel)
            {
                case SecurityRiskLevel.Safe:
                case SecurityRiskLevel.Low:
                    return true;
                    
                case SecurityRiskLevel.Medium:
                    // Medium risk commands are blocked by default for safe-by-default approach
                    return false;
                    
                case SecurityRiskLevel.High:
                    // For high-risk commands, check specific settings
                    if (commandInfo.Value.RequiredSecuritySetting == EnableTestsExecutionSetting)
                    {
                        return IsTestsExecutionAllowed();
                    }
                    return false;
                    
                case SecurityRiskLevel.Critical:
                    // For critical commands, check specific settings
                    if (commandInfo.Value.RequiredSecuritySetting == AllowMenuItemExecutionSetting)
                    {
                        return IsMenuItemExecutionAllowed();
                    }
                    return false;
            }

            // Default to block for unknown security levels
            return false;
        }

        /// <summary>
        /// Validates command execution and throws exception if blocked
        /// </summary>
        /// <param name="commandName">The name of the command to validate</param>
        /// <param name="throwOnBlock">If true, throws exception when blocked. If false, returns validation result.</param>
        /// <returns>True if command is allowed</returns>
        /// <exception cref="McpSecurityException">Thrown when command is blocked and throwOnBlock is true</exception>
        public static bool ValidateCommand(string commandName, bool throwOnBlock = true)
        {
            if (IsCommandAllowed(commandName))
            {
                return true;
            }

            string reason = GetBlockReason(commandName);
            McpLogger.LogWarning($"Command '{commandName}' blocked by security settings: {reason}");

            if (throwOnBlock)
            {
                throw new McpSecurityException(commandName, reason);
            }

            return false;
        }

        /// <summary>
        /// Gets the user-friendly reason why a command is blocked
        /// </summary>
        /// <param name="commandName">The name of the command</param>
        /// <returns>Human-readable reason string</returns>
        public static string GetBlockReason(string commandName)
        {
            var commandInfo = GetCommandSecurityInfoFromRegistry(commandName);
            if (!commandInfo.HasValue)
            {
                return $"Command '{commandName}' is not allowed by security policy.";
            }

            // Generate block reason based on required security setting
            switch (commandInfo.Value.RequiredSecuritySetting)
            {
                case EnableTestsExecutionSetting:
                    return "Tests execution is disabled. Enable 'Enable Tests Execution' in uMCP Security Settings.";
                    
                case AllowMenuItemExecutionSetting:
                    return "Menu item execution is disabled. Enable 'Allow Menu Item Execution' in uMCP Security Settings.";
                    
                default:
                    return $"Command '{commandName}' is not allowed by security policy (Risk Level: {commandInfo.Value.SecurityLevel}).";
            }
        }

        /// <summary>
        /// Gets security information for a command
        /// </summary>
        /// <param name="commandName">The name of the command</param>
        /// <returns>Security information object</returns>
        public static CommandSecurityInfo GetCommandSecurityInfo(string commandName)
        {
            bool isAllowed = IsCommandAllowed(commandName);
            string reason = isAllowed ? "Command is allowed" : GetBlockReason(commandName);
            SecurityRiskLevel riskLevel = GetCommandRiskLevel(commandName);
            
            return new CommandSecurityInfo(commandName, isAllowed, reason, riskLevel);
        }

        /// <summary>
        /// Gets the security risk level of a command
        /// </summary>
        /// <param name="commandName">The name of the command</param>
        /// <returns>Risk level enum</returns>
        private static SecurityRiskLevel GetCommandRiskLevel(string commandName)
        {
            var commandInfo = GetCommandSecurityInfoFromRegistry(commandName);
            if (!commandInfo.HasValue)
            {
                return SecurityRiskLevel.High; // Default to high risk for unknown commands
            }

            return commandInfo.Value.SecurityLevel;
        }

        /// <summary>
        /// Checks if tests execution is allowed
        /// </summary>
        /// <returns>True if tests execution is allowed</returns>
        private static bool IsTestsExecutionAllowed()
        {
            return McpEditorSettings.GetEnableTestsExecution();
        }

        /// <summary>
        /// Checks if menu item execution is allowed
        /// </summary>
        /// <returns>True if menu item execution is allowed</returns>
        private static bool IsMenuItemExecutionAllowed()
        {
            return McpEditorSettings.GetAllowMenuItemExecution();
        }
    }

    /// <summary>
    /// Security information for a command
    /// </summary>
    public readonly struct CommandSecurityInfo
    {
        public readonly string CommandName;
        public readonly bool IsAllowed;
        public readonly string Reason;
        public readonly SecurityRiskLevel RiskLevel;

        public CommandSecurityInfo(string commandName, bool isAllowed, string reason, SecurityRiskLevel riskLevel)
        {
            CommandName = commandName;
            IsAllowed = isAllowed;
            Reason = reason;
            RiskLevel = riskLevel;
        }
    }

    /// <summary>
    /// Command attribute information for security checking
    /// </summary>
    internal readonly struct CommandAttributeInfo
    {
        public readonly string CommandName;
        public readonly SecurityRiskLevel SecurityLevel;
        public readonly string RequiredSecuritySetting;

        public CommandAttributeInfo(string commandName, SecurityRiskLevel securityLevel, string requiredSecuritySetting)
        {
            CommandName = commandName;
            SecurityLevel = securityLevel;
            RequiredSecuritySetting = requiredSecuritySetting;
        }
    }

    /// <summary>
    /// Security risk levels for commands
    /// </summary>
    public enum SecurityRiskLevel
    {
        Safe = 0,       // No security risk
        Low = 1,        // Minimal risk
        Medium = 2,     // Moderate risk
        High = 3,       // High risk - can affect project files
        Critical = 4    // Critical risk - can execute arbitrary code
    }

    /// <summary>
    /// Exception thrown when a command is blocked by security settings
    /// </summary>
    public class McpSecurityException : Exception
    {
        public string CommandName { get; }
        public string SecurityReason { get; }

        public McpSecurityException(string commandName, string reason)
            : base($"Command '{commandName}' is blocked by security settings: {reason}")
        {
            CommandName = commandName;
            SecurityReason = reason;
        }

        public McpSecurityException(string commandName, string reason, Exception innerException)
            : base($"Command '{commandName}' is blocked by security settings: {reason}", innerException)
        {
            CommandName = commandName;
            SecurityReason = reason;
        }
    }
}