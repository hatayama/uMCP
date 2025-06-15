using UnityEngine;
using System.Collections.Generic;
using System.IO;
using SystemDiagnostics = System.Diagnostics;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// TypeScriptサーバーのビルド処理を担当するクラス
    /// </summary>
    public class TypeScriptBuilder
    {
        /// <summary>
        /// ビルド完了時のコールバック
        /// </summary>
        /// <param name="success">ビルドが成功したかどうか</param>
        /// <param name="output">ビルド出力</param>
        /// <param name="error">エラー出力</param>
        public delegate void BuildCompleteCallback(bool success, string output, string error);

        /// <summary>
        /// TypeScriptサーバーをビルドする
        /// </summary>
        /// <param name="onComplete">ビルド完了時のコールバック</param>
        public void BuildTypeScriptServer(BuildCompleteCallback onComplete = null)
        {
            string packageBasePath = UnityMcpPathResolver.GetPackageBasePath();
            if (string.IsNullOrEmpty(packageBasePath))
            {
                Debug.LogError("Package base path not found. Cannot build TypeScript server.");
                onComplete?.Invoke(false, "", "Package base path not found. Cannot build TypeScript server.");
                return;
            }
            
            string typeScriptDir = Path.Combine(packageBasePath, "TypeScriptServer");
            if (!Directory.Exists(typeScriptDir))
            {
                Debug.LogError($"TypeScript directory not found: {typeScriptDir}");
                onComplete?.Invoke(false, "", $"TypeScript directory not found: {typeScriptDir}");
                return;
            }
            
            // npmのパスを取得
            string npmPath = GetNpmPath();
            if (string.IsNullOrEmpty(npmPath))
            {
                Debug.LogError("npm command not found. Please make sure Node.js and npm are installed.");
                onComplete?.Invoke(false, "", "npm command not found");
                return;
            }
            
            Debug.Log($"Building TypeScript server in: {typeScriptDir}");
            Debug.Log($"Using npm at: {npmPath}");
            
            // npm ciを実行（package-lock.jsonから厳密にインストール）
            RunCommand(npmPath, "ci", typeScriptDir);
            
            // esbuildでバンドルビルドを実行
            RunCommand(npmPath, "run build:bundle", typeScriptDir);
            
            Debug.Log("TypeScript server build completed.");
            onComplete?.Invoke(true, "", "TypeScript server build completed.");
        }

        /// <summary>
        /// npm installを実行する
        /// </summary>
        /// <param name="npmPath">npmのパス</param>
        /// <param name="workingDirectory">作業ディレクトリ</param>
        /// <returns>成功したかどうか</returns>
        private bool RunNpmInstall(string npmPath, string workingDirectory)
        {
            Debug.Log("Running npm install...");
            
            SystemDiagnostics.ProcessStartInfo startInfo = new SystemDiagnostics.ProcessStartInfo
            {
                FileName = npmPath,
                Arguments = "install",
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            
            // 環境変数のPATHを設定
            SetupEnvironmentPath(startInfo, npmPath);
            
            try
            {
                using (SystemDiagnostics.Process process = SystemDiagnostics.Process.Start(startInfo))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    
                    process.WaitForExit();
                    
                    if (process.ExitCode == 0)
                    {
                        Debug.Log("npm install completed successfully!");
                        if (!string.IsNullOrEmpty(output))
                        {
                            Debug.Log($"Install output:\n{output}");
                        }
                        return true;
                    }
                    else
                    {
                        Debug.LogError($"npm install failed with exit code {process.ExitCode}");
                        if (!string.IsNullOrEmpty(error))
                        {
                            Debug.LogError($"Install error:\n{error}");
                        }
                        if (!string.IsNullOrEmpty(output))
                        {
                            Debug.LogError($"Install output:\n{output}");
                        }
                        return false;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to start npm install process: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// npmのパスを取得する（macOS対応）
        /// </summary>
        private string GetNpmPath()
        {
            Debug.Log("Searching for npm command...");
            
            // まずwhichコマンドでnpmを探す
            try
            {
                SystemDiagnostics.ProcessStartInfo whichInfo = new SystemDiagnostics.ProcessStartInfo
                {
                    FileName = "which",
                    Arguments = "npm",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                
                using (SystemDiagnostics.Process whichProcess = SystemDiagnostics.Process.Start(whichInfo))
                {
                    string whichOutput = whichProcess.StandardOutput.ReadToEnd().Trim();
                    string whichError = whichProcess.StandardError.ReadToEnd().Trim();
                    whichProcess.WaitForExit();
                    
                    Debug.Log($"which npm output: '{whichOutput}', error: '{whichError}', exit code: {whichProcess.ExitCode}");
                    
                    if (whichProcess.ExitCode == 0 && !string.IsNullOrEmpty(whichOutput))
                    {
                        Debug.Log($"Found npm via which command: {whichOutput}");
                        return whichOutput;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to run which command: {ex.Message}");
            }
            
            // whichで見つからない場合は、一般的なパスを試す
            string[] possiblePaths = {
                "/usr/local/bin/npm", // Homebrewでインストールした場合
                "/opt/homebrew/bin/npm", // Apple Silicon Macでのbrew
                "/usr/bin/npm", // システムインストール
                Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), ".nvm/versions/node/*/bin/npm") // nvm
            };
            
            foreach (string path in possiblePaths)
            {
                if (path.Contains("*"))
                {
                    // nvmパスの場合は展開して検索
                    string nvmBasePath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), ".nvm/versions/node");
                    if (Directory.Exists(nvmBasePath))
                    {
                        string[] nodeDirs = Directory.GetDirectories(nvmBasePath);
                        foreach (string nodeDir in nodeDirs)
                        {
                            string nvmNpmPath = Path.Combine(nodeDir, "bin", "npm");
                            if (File.Exists(nvmNpmPath))
                            {
                                return nvmNpmPath;
                            }
                        }
                    }
                }
                else
                {
                    Debug.Log($"Checking npm path: {path}");
                    // 直接パスをチェック
                    if (File.Exists(path))
                    {
                        Debug.Log($"Found npm at: {path}");
                        return path;
                    }
                }
            }
            
            Debug.LogError("npm command not found in any of the expected locations");
            return null;
        }

        /// <summary>
        /// 環境変数のPATHを設定する（nodeコマンドが見つかるように）
        /// </summary>
        private void SetupEnvironmentPath(SystemDiagnostics.ProcessStartInfo startInfo, string npmPath)
        {
            // npmのディレクトリからnodeのパスを推測
            string npmDir = Path.GetDirectoryName(npmPath);
            
            // 現在のPATH環境変数を取得
            string currentPath = System.Environment.GetEnvironmentVariable("PATH") ?? "";
            
            // 追加すべきパスのリスト
            List<string> additionalPaths = new List<string>();
            
            // npmと同じディレクトリを追加
            if (!string.IsNullOrEmpty(npmDir))
            {
                additionalPaths.Add(npmDir);
            }
            
            // 一般的なNode.jsのパスを追加
            string[] commonNodePaths = {
                "/usr/local/bin",
                "/opt/homebrew/bin",
                "/usr/bin"
            };
            
            foreach (string path in commonNodePaths)
            {
                if (Directory.Exists(path) && !additionalPaths.Contains(path))
                {
                    additionalPaths.Add(path);
                }
            }
            
            // nvmのパスも追加
            string nvmBasePath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), ".nvm/versions/node");
            if (Directory.Exists(nvmBasePath))
            {
                string[] nodeDirs = Directory.GetDirectories(nvmBasePath);
                foreach (string nodeDir in nodeDirs)
                {
                    string binPath = Path.Combine(nodeDir, "bin");
                    if (Directory.Exists(binPath) && !additionalPaths.Contains(binPath))
                    {
                        additionalPaths.Add(binPath);
                    }
                }
            }
            
            // 新しいPATHを構築
            string newPath = string.Join(":", additionalPaths) + ":" + currentPath;
            
            // 環境変数を設定
            startInfo.EnvironmentVariables["PATH"] = newPath;
            
            Debug.Log($"Updated PATH for npm process: {newPath}");
        }

        private void RunCommand(string command, string arguments, string workingDirectory)
        {
            Debug.Log($"Running command: {command} {arguments} in directory: {workingDirectory}");
            
            SystemDiagnostics.ProcessStartInfo startInfo = new SystemDiagnostics.ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            
            // 環境変数のPATHを設定
            SetupEnvironmentPath(startInfo, GetNpmPath());
            
            try
            {
                using (SystemDiagnostics.Process process = SystemDiagnostics.Process.Start(startInfo))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    
                    process.WaitForExit();
                    
                    bool success = process.ExitCode == 0;
                    
                    if (success)
                    {
                        Debug.Log($"{command} {arguments} completed successfully!");
                        if (!string.IsNullOrEmpty(output))
                        {
                            Debug.Log($"Output:\n{output}");
                        }
                    }
                    else
                    {
                        Debug.LogError($"{command} {arguments} failed with exit code {process.ExitCode}");
                        if (!string.IsNullOrEmpty(error))
                        {
                            Debug.LogError($"Error:\n{error}");
                        }
                        if (!string.IsNullOrEmpty(output))
                        {
                            Debug.LogError($"Output:\n{output}");
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                string errorMsg = $"Failed to run command: {command} {arguments}: {ex.Message}";
                Debug.LogError(errorMsg);
                Debug.LogError("Make sure npm is installed and available in PATH");
            }
        }
    }
} 