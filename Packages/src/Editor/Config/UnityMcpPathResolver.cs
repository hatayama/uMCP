using System.IO;
using UnityEngine;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Class for resolving paths related to Unity MCP.
    /// Single Responsibility Principle: Only responsible for path resolution.
    /// 
    /// ## Adding New LLM Tools Support
    /// 
    /// When adding support for a new LLM tool, follow these steps to avoid common failures:
    /// 
    /// ### 1. Update UnityMcpPathResolver.cs (this file)
    /// - Add new McpEditorType enum value
    /// - Add configuration file path constants
    /// - Add GetXXXConfigPath() method
    /// - Add GetXXXConfigDirectory() method (if needed)
    /// - Update GetConfigPath() switch statement
    /// - Update GetConfigDirectory() switch statement
    /// 
    /// ### 2. Update McpConstants.cs
    /// - Add CLIENT_NAME_XXX constant
    /// - Update GetClientNameForEditor() switch statement
    /// 
    /// ### 3. Update McpEditorWindow.cs
    /// - Add private McpConfigService field (_xxxConfigService)
    /// - Initialize the service in OnEnable()
    /// - Update GetConfigService() switch statement
    /// - Update GetEditorDisplayName() switch statement
    /// 
    /// ### 4. Related Classes to Check
    /// - McpConfigService: Handles configuration file read/write operations
    /// - McpConfigRepository: Manages configuration data persistence
    /// - McpServerConfigFactory: Creates server configuration objects
    /// - McpEditorSettings: Manages editor-specific settings
    /// 
    /// ### 5. Configuration File Format
    /// All tools use the same JSON structure:
    /// ```json
    /// {
    ///   "mcpServers": {
    ///     "unity-mcp@{port}": {
    ///       "command": "node",
    ///       "args": ["{path_to_server.bundle.js}"],
    ///       "env": {
    ///         "UNITY_TCP_PORT": "{port}",
    ///         "MCP_CLIENT_NAME": "{client_name}"
    ///       }
    ///     }
    ///   }
    /// }
    /// ```
    /// 
    /// ### 6. Testing Checklist
    /// - Verify compilation succeeds without errors
    /// - Test configuration file creation/update
    /// - Verify client name appears correctly in Unity MCP window
    /// - Test server start/stop functionality
    /// - Confirm MCP_CLIENT_NAME environment variable is set correctly
    /// </summary>
    public static class UnityMcpPathResolver
    {
        private const string CURSOR_CONFIG_DIR = ".cursor";
        private const string VSCODE_CONFIG_DIR = ".vscode";
        private const string GEMINI_CONFIG_DIR = ".gemini";
        private const string MCP_CONFIG_FILE = "mcp.json";
        private const string CLAUDE_CODE_CONFIG_FILE = ".mcp.json";
        private const string GEMINI_CONFIG_FILE = "settings.json";
        private const string MCP_INSPECTOR_CONFIG_FILE = ".inspector.mcp.json";

        /// <summary>
        /// Gets the path to the project root directory.
        /// </summary>
        public static string GetProjectRoot()
        {
            return Path.GetDirectoryName(Application.dataPath);
        }

        /// <summary>
        /// Gets the path to .cursor/mcp.json in the project root.
        /// </summary>
        public static string GetMcpConfigPath()
        {
            string projectRoot = GetProjectRoot();
            return Path.Combine(projectRoot, CURSOR_CONFIG_DIR, MCP_CONFIG_FILE);
        }

        /// <summary>
        /// Gets the path to the Claude Code configuration file (.mcp.json).
        /// </summary>
        public static string GetClaudeCodeConfigPath()
        {
            string projectRoot = GetProjectRoot();
            return Path.Combine(projectRoot, CLAUDE_CODE_CONFIG_FILE);
        }

        /// <summary>
        /// Gets the path to the VSCode configuration file (.vscode/mcp.json).
        /// </summary>
        public static string GetVSCodeConfigPath()
        {
            string projectRoot = GetProjectRoot();
            return Path.Combine(projectRoot, VSCODE_CONFIG_DIR, MCP_CONFIG_FILE);
        }

        /// <summary>
        /// Gets the path to the Gemini CLI configuration file (.gemini/settings.json).
        /// </summary>
        public static string GetGeminiCLIConfigPath()
        {
            string projectRoot = GetProjectRoot();
            return Path.Combine(projectRoot, GEMINI_CONFIG_DIR, GEMINI_CONFIG_FILE);
        }

        /// <summary>
        /// Gets the path to the MCP Inspector configuration file (.inspector.mcp.json).
        /// </summary>
        public static string GetMcpInspectorConfigPath()
        {
            string projectRoot = GetProjectRoot();
            return Path.Combine(projectRoot, MCP_INSPECTOR_CONFIG_FILE);
        }

        /// <summary>
        /// Gets the configuration file path for the specified editor.
        /// </summary>
        /// <param name="editorType">The type of editor.</param>
        /// <returns>The path to the configuration file.</returns>
        public static string GetConfigPath(McpEditorType editorType)
        {
            return editorType switch
            {
                McpEditorType.Cursor => GetMcpConfigPath(),
                McpEditorType.ClaudeCode => GetClaudeCodeConfigPath(),
                McpEditorType.VSCode => GetVSCodeConfigPath(),
                McpEditorType.GeminiCLI => GetGeminiCLIConfigPath(),
                McpEditorType.McpInspector => GetMcpInspectorConfigPath(),
                _ => throw new System.ArgumentException($"Unsupported editor type: {editorType}")
            };
        }

        /// <summary>
        /// Gets the path to the .cursor directory.
        /// </summary>
        public static string GetCursorConfigDirectory()
        {
            string projectRoot = GetProjectRoot();
            return Path.Combine(projectRoot, CURSOR_CONFIG_DIR);
        }

        /// <summary>
        /// Gets the path to the .vscode directory.
        /// </summary>
        public static string GetVSCodeConfigDirectory()
        {
            string projectRoot = GetProjectRoot();
            return Path.Combine(projectRoot, VSCODE_CONFIG_DIR);
        }

        /// <summary>
        /// Gets the path to the .gemini directory.
        /// </summary>
        public static string GetGeminiConfigDirectory()
        {
            string projectRoot = GetProjectRoot();
            return Path.Combine(projectRoot, GEMINI_CONFIG_DIR);
        }

        /// <summary>
        /// Gets the configuration directory for the specified editor (only if it exists).
        /// </summary>
        /// <param name="editorType">The type of editor.</param>
        /// <returns>The path to the configuration directory (null for Claude Code and MCP Inspector).</returns>
        public static string GetConfigDirectory(McpEditorType editorType)
        {
            return editorType switch
            {
                McpEditorType.Cursor => GetCursorConfigDirectory(),
                McpEditorType.ClaudeCode => null, // Claude Code is placed directly in the project root.
                McpEditorType.VSCode => GetVSCodeConfigDirectory(),
                McpEditorType.GeminiCLI => GetGeminiConfigDirectory(),
                McpEditorType.McpInspector => null, // MCP Inspector is placed directly in the project root.
                _ => throw new System.ArgumentException($"Unsupported editor type: {editorType}")
            };
        }

        /// <summary>
        /// Gets the path to the TypeScript server.
        /// Supports both installation via Package Manager and local development.
        /// </summary>
        public static string GetTypeScriptServerPath()
        {
            string packageBasePath = GetPackageBasePath();
            if (string.IsNullOrEmpty(packageBasePath))
            {
                return string.Empty;
            }
            
            return Path.Combine(packageBasePath, "TypeScriptServer", "dist", "server.bundle.js");
        }

        /// <summary>
        /// Gets the base path of the package.
        /// Supports both installation via Package Manager and local development.
        /// </summary>
        public static string GetPackageBasePath()
        {
            string projectRoot = GetProjectRoot();
            
            // 1. First, check the path for local development (Packages/src).
            string localPath = Path.Combine(projectRoot, "Packages", "src");
            string localServerPath = Path.Combine(localPath, "TypeScriptServer", "dist", "server.bundle.js");
            if (File.Exists(localServerPath))
            {
                // Using local package path
                return localPath;
            }
            
            // 2. Search for the path when installed via Package Manager.
            string packageCacheDir = Path.Combine(projectRoot, "Library", "PackageCache");
            if (Directory.Exists(packageCacheDir))
            {
                // Search for directories starting with io.github.hatayama.umcp@.
                string[] packageDirs = Directory.GetDirectories(packageCacheDir, "io.github.hatayama.umcp@*");
                
                foreach (string packageDir in packageDirs)
                {
                    string serverPath = Path.Combine(packageDir, "TypeScriptServer", "dist", "server.bundle.js");
                    if (File.Exists(serverPath))
                    {
                        // Using Package Manager package path
                        return packageDir;
                    }
                }
            }
            
            // 3. If neither is found, return the local path (with an error log).
            McpLogger.LogError($"Package base path not found. Checked paths:\n  Local: {localPath}\n  Package Cache: {packageCacheDir}/io.github.hatayama.unitymcp@*");
            
            return localPath;
        }
    }

    /// <summary>
    /// Supported editor types.
    /// </summary>
    public enum McpEditorType
    {
        Cursor,
        ClaudeCode,
        VSCode,
        GeminiCLI,
        McpInspector
    }
} 