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
        // ディレクトリ・ファイル名の定数
        private const string TypeScriptServerDirName = "TypeScriptServer";
        private const string NvmVersionsNodePath = ".nvm/versions/node";
        private const string BinDirName = "bin";
        private const string NpmExecutableName = "npm";
        
        // コマンドの定数
        private const string WhichCommand = "which";
        private const string NpmCiCommand = "ci";
        private const string NpmRunBuildBundleCommand = "run build:bundle";
        private const string NpmInstallCommand = "install";
        
        // パスの定数
        private const string HomebrewBinPath = "/usr/local/bin";
        private const string AppleSiliconBrewBinPath = "/opt/homebrew/bin";
        private const string SystemBinPath = "/usr/bin";
        private const string PathEnvironmentVariable = "PATH";
        private const string PathSeparator = ":";
        private const string WildcardChar = "*";
        
        // メッセージの定数
        private const string PackagePathNotFoundError = "Package base path not found. Cannot build TypeScript server.";
        private const string TypeScriptDirNotFoundError = "TypeScript directory not found: {0}";
        private const string NpmNotFoundError = "npm command not found. Please make sure Node.js and npm are installed.";
        private const string NpmNotFoundShortError = "npm command not found";
        private const string BuildingMessage = "Building TypeScript server in: {0}";
        private const string UsingNpmMessage = "Using npm at: {0}";
        private const string BuildCompletedMessage = "TypeScript server build completed.";
        private const string SearchingNpmMessage = "Searching for npm command...";
        private const string RunningNpmInstallMessage = "Running npm install...";
        private const string NpmInstallSuccessMessage = "npm install completed successfully!";
        private const string InstallOutputMessage = "Install output:\n{0}";
        private const string NpmInstallFailedMessage = "npm install failed with exit code {0}";
        private const string InstallErrorMessage = "Install error:\n{0}";
        private const string WhichNpmOutputMessage = "which npm output: '{0}', error: '{1}', exit code: {2}";
        private const string FoundNpmViaWhichMessage = "Found npm via which command: {0}";
        private const string CheckingNpmPathMessage = "Checking npm path: {0}";
        private const string FoundNpmAtMessage = "Found npm at: {0}";
        private const string NpmNotFoundInExpectedLocationsError = "npm command not found in any of the expected locations";
        private const string RunningCommandMessage = "Running command: {0} {1} in directory: {2}";
        private const string CommandCompletedSuccessMessage = "{0} {1} completed successfully!";
        private const string OutputMessage = "Output:\n{0}";
        private const string CommandFailedMessage = "{0} {1} failed with exit code {2}";
        private const string ErrorMessage = "Error:\n{0}";
        private const string UpdatedPathMessage = "Updated PATH for npm process: {0}";
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
                McpLogger.LogError(PackagePathNotFoundError);
                onComplete?.Invoke(false, "", PackagePathNotFoundError);
                return;
            }
            
            string typeScriptDir = Path.Combine(packageBasePath, TypeScriptServerDirName);
            if (!Directory.Exists(typeScriptDir))
            {
                McpLogger.LogError(string.Format(TypeScriptDirNotFoundError, typeScriptDir));
                onComplete?.Invoke(false, "", string.Format(TypeScriptDirNotFoundError, typeScriptDir));
                return;
            }
            
            // npmのパスを取得
            string npmPath = GetNpmPath();
            if (string.IsNullOrEmpty(npmPath))
            {
                McpLogger.LogError(NpmNotFoundError);
                onComplete?.Invoke(false, "", NpmNotFoundShortError);
                return;
            }
            
            McpLogger.LogInfo(string.Format(BuildingMessage, typeScriptDir));
            McpLogger.LogInfo(string.Format(UsingNpmMessage, npmPath));
            
            // npm ciを実行（package-lock.jsonから厳密にインストール）
            RunCommand(npmPath, NpmCiCommand, typeScriptDir);
            
            // esbuildでバンドルビルドを実行
            RunCommand(npmPath, NpmRunBuildBundleCommand, typeScriptDir);
            
            McpLogger.LogInfo(BuildCompletedMessage);
            onComplete?.Invoke(true, "", BuildCompletedMessage);
        }

        /// <summary>
        /// npm installを実行する
        /// </summary>
        /// <param name="npmPath">npmのパス</param>
        /// <param name="workingDirectory">作業ディレクトリ</param>
        /// <returns>成功したかどうか</returns>
        private bool RunNpmInstall(string npmPath, string workingDirectory)
        {
            McpLogger.LogInfo(RunningNpmInstallMessage);
            
            SystemDiagnostics.ProcessStartInfo startInfo = new SystemDiagnostics.ProcessStartInfo
            {
                FileName = npmPath,
                Arguments = NpmInstallCommand,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            
            // 環境変数のPATHを設定
            SetupEnvironmentPath(startInfo, npmPath);
            
            using (SystemDiagnostics.Process process = SystemDiagnostics.Process.Start(startInfo))
            {
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                
                process.WaitForExit();
                
                if (process.ExitCode == 0)
                {
                    McpLogger.LogInfo(NpmInstallSuccessMessage);
                    if (!string.IsNullOrEmpty(output))
                    {
                        McpLogger.LogDebug(string.Format(InstallOutputMessage, output));
                    }
                    return true;
                }
                
                McpLogger.LogError(string.Format(NpmInstallFailedMessage, process.ExitCode));
                if (!string.IsNullOrEmpty(error))
                {
                    McpLogger.LogError(string.Format(InstallErrorMessage, error));
                }
                if (!string.IsNullOrEmpty(output))
                {
                    McpLogger.LogError(string.Format(InstallOutputMessage, output));
                }
                
                return false;
            }
        }

        /// <summary>
        /// npmのパスを取得する（macOS対応）
        /// </summary>
        private string GetNpmPath()
        {
            McpLogger.LogDebug(SearchingNpmMessage);
            
            // まずwhichコマンドでnpmを探す
            SystemDiagnostics.ProcessStartInfo whichInfo = new SystemDiagnostics.ProcessStartInfo
            {
                FileName = WhichCommand,
                Arguments = NpmExecutableName,
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
                
                McpLogger.LogDebug(string.Format(WhichNpmOutputMessage, whichOutput, whichError, whichProcess.ExitCode));
                
                if (whichProcess.ExitCode == 0 && !string.IsNullOrEmpty(whichOutput))
                {
                    McpLogger.LogDebug(string.Format(FoundNpmViaWhichMessage, whichOutput));
                    return whichOutput;
                }
            }
            
            // whichで見つからない場合は、一般的なパスを試す
            string[] possiblePaths = {
                Path.Combine(HomebrewBinPath, NpmExecutableName), // Homebrewでインストールした場合
                Path.Combine(AppleSiliconBrewBinPath, NpmExecutableName), // Apple Silicon Macでのbrew
                Path.Combine(SystemBinPath, NpmExecutableName), // システムインストール
                Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), NvmVersionsNodePath, WildcardChar, BinDirName, NpmExecutableName) // nvm
            };
            
            foreach (string path in possiblePaths)
            {
                if (path.Contains(WildcardChar))
                {
                    // nvmパスの場合は展開して検索
                    string nvmBasePath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), NvmVersionsNodePath);
                    if (Directory.Exists(nvmBasePath))
                    {
                        string[] nodeDirs = Directory.GetDirectories(nvmBasePath);
                        foreach (string nodeDir in nodeDirs)
                        {
                            string nvmNpmPath = Path.Combine(nodeDir, BinDirName, NpmExecutableName);
                            if (File.Exists(nvmNpmPath))
                            {
                                return nvmNpmPath;
                            }
                        }
                    }
                }
                else
                {
                    McpLogger.LogDebug(string.Format(CheckingNpmPathMessage, path));
                    // 直接パスをチェック
                    if (File.Exists(path))
                    {
                        McpLogger.LogDebug(string.Format(FoundNpmAtMessage, path));
                        return path;
                    }
                }
            }
            
            McpLogger.LogError(NpmNotFoundInExpectedLocationsError);
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
            string currentPath = System.Environment.GetEnvironmentVariable(PathEnvironmentVariable) ?? "";
            
            // 追加すべきパスのリスト
            List<string> additionalPaths = new List<string>();
            
            // npmと同じディレクトリを追加
            if (!string.IsNullOrEmpty(npmDir))
            {
                additionalPaths.Add(npmDir);
            }
            
            // 一般的なNode.jsのパスを追加
            string[] commonNodePaths = {
                HomebrewBinPath,
                AppleSiliconBrewBinPath,
                SystemBinPath
            };
            
            foreach (string path in commonNodePaths)
            {
                if (Directory.Exists(path) && !additionalPaths.Contains(path))
                {
                    additionalPaths.Add(path);
                }
            }
            
            // nvmのパスも追加
            string nvmBasePath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), NvmVersionsNodePath);
            if (Directory.Exists(nvmBasePath))
            {
                string[] nodeDirs = Directory.GetDirectories(nvmBasePath);
                foreach (string nodeDir in nodeDirs)
                {
                    string binPath = Path.Combine(nodeDir, BinDirName);
                    if (Directory.Exists(binPath) && !additionalPaths.Contains(binPath))
                    {
                        additionalPaths.Add(binPath);
                    }
                }
            }
            
            // 新しいPATHを構築
            string newPath = string.Join(PathSeparator, additionalPaths) + PathSeparator + currentPath;
            
            // 環境変数を設定
            startInfo.EnvironmentVariables[PathEnvironmentVariable] = newPath;
            
            McpLogger.LogDebug(string.Format(UpdatedPathMessage, newPath));
        }

        private void RunCommand(string command, string arguments, string workingDirectory)
        {
            McpLogger.LogInfo(string.Format(RunningCommandMessage, command, arguments, workingDirectory));
            
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
            
            using (SystemDiagnostics.Process process = SystemDiagnostics.Process.Start(startInfo))
            {
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                
                process.WaitForExit();
                
                bool success = process.ExitCode == 0;
                
                if (success)
                {
                    McpLogger.LogInfo(string.Format(CommandCompletedSuccessMessage, command, arguments));
                    if (!string.IsNullOrEmpty(output))
                    {
                        McpLogger.LogDebug(string.Format(OutputMessage, output));
                    }
                }
                else
                {
                    McpLogger.LogError(string.Format(CommandFailedMessage, command, arguments, process.ExitCode));
                    if (!string.IsNullOrEmpty(error))
                    {
                        McpLogger.LogError(string.Format(ErrorMessage, error));
                    }
                    if (!string.IsNullOrEmpty(output))
                    {
                        McpLogger.LogError(string.Format(OutputMessage, output));
                    }
                }
            }
        }
    }
} 