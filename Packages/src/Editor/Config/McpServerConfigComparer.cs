using System.Collections.Generic;

namespace io.github.hatayama.uLoopMCP
{
    /// <summary>
    /// Class responsible for comparing MCP server settings.
    /// Single Responsibility Principle: Only responsible for comparing settings.
    /// </summary>
    public static class McpServerConfigComparer
    {
        /// <summary>
        /// Checks if two settings are equal.
        /// </summary>
        public static bool AreEqual(McpServerConfigData config1, McpServerConfigData config2)
        {
            // Compare command.
            if (config1.command != config2.command)
                return false;

            // Compare args.
            if (config1.args.Length != config2.args.Length)
                return false;
            
            for (int i = 0; i < config1.args.Length; i++)
            {
                if (config1.args[i] != config2.args[i])
                    return false;
            }

            // Compare env.
            if (config1.env.Count != config2.env.Count)
                return false;

            foreach (KeyValuePair<string, string> kvp in config1.env)
            {
                if (!config2.env.ContainsKey(kvp.Key) || config2.env[kvp.Key] != kvp.Value)
                    return false;
            }

            return true;
        }
    }
} 