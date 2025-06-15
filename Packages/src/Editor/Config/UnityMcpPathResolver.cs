using System.IO;
using UnityEngine;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Unity MCP関連のパス解決を行うクラス
    /// 単一責任原則：パス解決のみを担当
    /// </summary>
    public static class UnityMcpPathResolver
    {
        private const string CURSOR_CONFIG_DIR = ".cursor";
        private const string MCP_CONFIG_FILE = "mcp.json";

        /// <summary>
        /// プロジェクトルートディレクトリのパスを取得
        /// </summary>
        public static string GetProjectRoot()
        {
            return Path.GetDirectoryName(Application.dataPath);
        }

        /// <summary>
        /// プロジェクトルートの.cursor/mcp.jsonパスを取得
        /// </summary>
        public static string GetMcpConfigPath()
        {
            string projectRoot = GetProjectRoot();
            return Path.Combine(projectRoot, CURSOR_CONFIG_DIR, MCP_CONFIG_FILE);
        }

        /// <summary>
        /// .cursorディレクトリのパスを取得
        /// </summary>
        public static string GetCursorConfigDirectory()
        {
            string projectRoot = GetProjectRoot();
            return Path.Combine(projectRoot, CURSOR_CONFIG_DIR);
        }

        /// <summary>
        /// TypeScriptサーバーのパスを取得
        /// Package Manager経由でインストールされた場合とローカル開発の場合の両方に対応
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
        /// パッケージのベースパスを取得
        /// Package Manager経由でインストールされた場合とローカル開発の場合の両方に対応
        /// </summary>
        public static string GetPackageBasePath()
        {
            string projectRoot = GetProjectRoot();
            
            // 1. まずローカル開発用のパスをチェック（Packages/src）
            string localPath = Path.Combine(projectRoot, "Packages", "src");
            string localServerPath = Path.Combine(localPath, "TypeScriptServer", "dist", "server.bundle.js");
            if (File.Exists(localServerPath))
            {
                McpLogger.LogInfo($"Using local package path: {localPath}");
                return localPath;
            }
            
            // 2. Package Manager経由でインストールされた場合のパスを検索
            string packageCacheDir = Path.Combine(projectRoot, "Library", "PackageCache");
            if (Directory.Exists(packageCacheDir))
            {
                // io.github.hatayama.unitymcp@で始まるディレクトリを検索
                string[] packageDirs = Directory.GetDirectories(packageCacheDir, "io.github.hatayama.unitymcp@*");
                
                foreach (string packageDir in packageDirs)
                {
                    string serverPath = Path.Combine(packageDir, "TypeScriptServer", "dist", "server.bundle.js");
                    if (File.Exists(serverPath))
                    {
                        McpLogger.LogInfo($"Using Package Manager package path: {packageDir}");
                        return packageDir;
                    }
                }
            }
            
            // 3. どちらも見つからない場合はローカルパスを返す（エラーログ付き）
            McpLogger.LogError($"Package base path not found. Checked paths:\n  Local: {localPath}\n  Package Cache: {packageCacheDir}/io.github.hatayama.unitymcp@*");
            
            return localPath;
        }
    }
} 