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
                return new CommandAttributeInfo(commandName, SecuritySettings.None);
            }

            return new CommandAttributeInfo(commandName, attribute.RequiredSecuritySetting);
        }

        /// <summary>
        /// Checks if command is allowed based on its attribute settings
        /// </summary>
        /// <param name="commandInfo">Command attribute information</param>
        /// <returns>True if command is allowed</returns>
        private static bool IsCommandAllowedByAttribute(CommandAttributeInfo commandInfo)
        {
            // Check by required security setting
            switch (commandInfo.RequiredSecuritySetting)
            {
                case SecuritySettings.None:
                    return true; // No security restriction
                    
                case SecuritySettings.EnableTestsExecution:
                    return IsTestsExecutionAllowed();
                    
                case SecuritySettings.AllowMenuItemExecution:
                    return IsMenuItemExecutionAllowed();
                    
                default:
                    return false; // Unknown setting - block by default
            }
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
                case SecuritySettings.EnableTestsExecution:
                    return "Tests execution is disabled. Enable 'Enable Tests Execution' in uMCP Security Settings.";
                    
                case SecuritySettings.AllowMenuItemExecution:
                    return "Menu item execution is disabled. Enable 'Allow Menu Item Execution' in uMCP Security Settings.";
                    
                case SecuritySettings.None:
                default:
                    return $"Command '{commandName}' is not allowed by security policy.";
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
            
            return new CommandSecurityInfo(commandName, isAllowed, reason);
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

        public CommandSecurityInfo(string commandName, bool isAllowed, string reason)
        {
            CommandName = commandName;
            IsAllowed = isAllowed;
            Reason = reason;
        }
    }

    /// <summary>
    /// Command attribute information for security checking
    /// </summary>
    internal readonly struct CommandAttributeInfo
    {
        public readonly string CommandName;
        public readonly SecuritySettings RequiredSecuritySetting;

        public CommandAttributeInfo(string commandName, SecuritySettings requiredSecuritySetting)
        {
            CommandName = commandName;
            RequiredSecuritySetting = requiredSecuritySetting;
        }
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