using UnityEngine;
using UnityEditor;
using System.IO;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Utility for testing the path resolution feature.
    /// </summary>
    public static class PathResolverTest
    {
        [MenuItem("uMCP/Debug/Test TypeScript Server Path")]
        public static void TestTypeScriptServerPath()
        {
            McpLogger.LogInfo("=== TypeScript Server Path Test ===");
            
            string serverPath = UnityMcpPathResolver.GetTypeScriptServerPath();
            McpLogger.LogInfo($"Resolved server path: {serverPath}");
            
            if (File.Exists(serverPath))
            {
                McpLogger.LogInfo("✓ Server file exists");
                
                // Also display the file size
                FileInfo fileInfo = new FileInfo(serverPath);
                McpLogger.LogInfo($"  File size: {fileInfo.Length} bytes");
                McpLogger.LogInfo($"  Last modified: {fileInfo.LastWriteTime}");
            }
            else
            {
                McpLogger.LogError("✗ Server file not found");
            }
            
            // Also display detailed information of the search target path
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            
            McpLogger.LogInfo("=== Search Details ===");
            McpLogger.LogInfo($"Project root: {projectRoot}");
            
            // Check local path
            string localPath = Path.Combine(projectRoot, "Packages", "src", ".TypeScriptServer", "dist", "server.bundle.js");
            McpLogger.LogInfo($"Local path: {localPath} - {(File.Exists(localPath) ? "EXISTS" : "NOT FOUND")}");
            
            // Check Package Cache
            string packageCacheDir = Path.Combine(projectRoot, "Library", "PackageCache");
            McpLogger.LogInfo($"Package cache dir: {packageCacheDir} - {(Directory.Exists(packageCacheDir) ? "EXISTS" : "NOT FOUND")}");
            
            if (Directory.Exists(packageCacheDir))
            {
                string[] packageDirs = Directory.GetDirectories(packageCacheDir, "io.github.hatayama.unitymcp@*");
                McpLogger.LogInfo($"Found {packageDirs.Length} matching package directories:");
                
                foreach (string packageDir in packageDirs)
                {
                    string serverPathInCache = Path.Combine(packageDir, ".TypeScriptServer", "dist", "server.bundle.js");
                    McpLogger.LogInfo($"  {packageDir} -> {(File.Exists(serverPathInCache) ? "HAS SERVER" : "NO SERVER")}");
                }
            }
            
            McpLogger.LogInfo("=== Test Complete ===");
        }
        
        [MenuItem("uMCP/Debug/Force Update MCP Config")]
        public static void ForceUpdateMcpConfig()
        {
            McpLogger.LogInfo("=== Force Update MCP Config ===");
            
            int port = 7400;
            McpConfigRepository repository = new(McpEditorType.Cursor);
            McpConfigService configService = new(repository, McpEditorType.Cursor);
            configService.AutoConfigure(port);
            
            McpLogger.LogInfo("MCP config update completed");
        }

        [MenuItem("uMCP/Test Path Resolver")]
        public static void TestPathResolver()
        {
            Debug.Log("=== Path Resolver Test ===");
            
            string serverPath = UnityMcpPathResolver.GetTypeScriptServerPath();
            Debug.Log($"TypeScript Server Path: {serverPath}");
            
            if (string.IsNullOrEmpty(serverPath))
            {
                Debug.LogError("TypeScript server path is empty!");
                return;
            }
            
            if (System.IO.File.Exists(serverPath))
            {
                Debug.Log("✓ TypeScript server file exists");
            }
            else
            {
                Debug.LogError("✗ TypeScript server file not found");
            }
            
            string packageBasePath = UnityMcpPathResolver.GetPackageBasePath();
            Debug.Log($"Package Base Path: {packageBasePath}");
            
            string configPath = UnityMcpPathResolver.GetMcpConfigPath();
            Debug.Log($"MCP Config Path: {configPath}");
        }
    }
} 