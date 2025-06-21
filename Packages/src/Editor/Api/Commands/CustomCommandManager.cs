using System;
using System.Collections.Generic;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Static class for managing custom commands
    /// Allows users to register and manage their own commands
    /// </summary>
    public static class CustomCommandManager
    {
        private static UnityCommandRegistry _sharedRegistry;

        /// <summary>
        /// Get shared registry (lazy initialization)
        /// </summary>
        private static UnityCommandRegistry SharedRegistry
        {
            get
            {
                if (_sharedRegistry == null)
                {
                    _sharedRegistry = new UnityCommandRegistry();
                    // Standard commands are automatically registered in UnityCommandRegistry constructor
                }
                return _sharedRegistry;
            }
        }

        /// <summary>
        /// Register custom command
        /// </summary>
        /// <param name="command">Command to register</param>
        public static void RegisterCustomCommand(IUnityCommand command)
        {
            SharedRegistry.RegisterCommand(command);
            McpLogger.LogInfo($"Custom command registered: {command.CommandName}");
            NotifyToolsChanged();
        }

        /// <summary>
        /// Unregister custom command
        /// </summary>
        /// <param name="commandName">Name of command to unregister</param>
        public static void UnregisterCustomCommand(string commandName)
        {
            SharedRegistry.UnregisterCommand(commandName);
            McpLogger.LogInfo($"Custom command unregistered: {commandName}");
            NotifyToolsChanged();
        }

        /// <summary>
        /// Get list of all registered commands (standard + custom)
        /// </summary>
        /// <returns>Array of command information</returns>
        public static CommandInfo[] GetRegisteredCustomCommands()
        {
            return SharedRegistry.GetRegisteredCommands();
        }

        /// <summary>
        /// Check if specified command is registered
        /// </summary>
        /// <param name="commandName">Command name</param>
        /// <returns>True if registered</returns>
        public static bool IsCustomCommandRegistered(string commandName)
        {
            return SharedRegistry.IsCommandRegistered(commandName);
        }

        /// <summary>
        /// Get internal registry (for MCP server)
        /// </summary>
        /// <returns>UnityCommandRegistry instance</returns>
        internal static UnityCommandRegistry GetRegistry()
        {
            return SharedRegistry;
        }

        /// <summary>
        /// Debug: Get array of registered command names
        /// </summary>
        /// <returns>Array of command names</returns>
        public static string[] GetRegisteredCommandNames()
        {
            return SharedRegistry.GetRegisteredCommandNames();
        }

        /// <summary>
        /// Debug: Get detailed registry information
        /// </summary>
        /// <returns>Debug information</returns>
        public static string GetDebugInfo()
        {
            return $"Registry instance: {SharedRegistry.GetHashCode()}, Commands: [{string.Join(", ", SharedRegistry.GetRegisteredCommandNames())}]";
        }

        /// <summary>
        /// Notify MCP clients that tools have changed
        /// </summary>
        private static void NotifyToolsChanged()
        {
            // This will be picked up by the TypeScript server when it polls for command updates
            McpLogger.LogInfo("Tools list changed - MCP clients will be notified on next command list request");
        }
    }
} 