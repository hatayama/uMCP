/*
 * NetworkUtility.cs
 * 
 * 設計ドキュメント: ARCHITECTURE.md - Network Communication
 * 関連クラス: McpBridgeServer, McpServerController, ConnectedClient
 * 
 * ネットワーク関連のユーティリティ機能を提供する統合クラス
 * ポート利用可能性チェック、プロセスID取得、ネットワーク接続管理を担当
 */

using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using io.github.hatayama.uMCP;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// ネットワーク関連のユーティリティ機能を提供する統合クラス
    /// ポート利用可能性チェック、プロセスID取得、ネットワーク接続管理を担当
    /// </summary>
    public static class NetworkUtility
    {
        /// <summary>
        /// 一般的なシステムポート（競合を避けるため除外対象）
        /// </summary>
        private static readonly int[] CommonSystemPorts = { 80, 443, 21, 22, 23, 25, 53, 110, 143, 993, 995, 3389 };

        #region Port Availability Methods

        /// <summary>
        /// 指定されたポートが使用中かどうかをチェックする
        /// TCP接続テストによりポートの利用可能性を確認
        /// </summary>
        /// <param name="port">チェック対象のポート番号</param>
        /// <returns>ポートが使用中の場合true、利用可能な場合false</returns>
        public static bool IsPortInUse(int port)
        {
            TcpListener tcpListener = null;
            
            try
            {
                tcpListener = new TcpListener(IPAddress.Loopback, port);
                tcpListener.Start();
                return false; // The port is available.
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
            {
                // Port is already in use - this is expected behavior
                return true;
            }
            catch (SocketException ex)
            {
                // Other socket errors should be logged with specific information
                McpLogger.LogError($"[IsPortInUse] Socket error checking port {port}: {ex.SocketErrorCode} - {ex.Message}");
                return true; // Treat as "in use" to be safe
            }
            finally
            {
                tcpListener?.Stop();
            }
        }

        /// <summary>
        /// 指定されたポートから開始して利用可能なポートを探索する
        /// システムポートを避けて安全なポートを選択
        /// </summary>
        /// <param name="startPort">探索開始ポート番号</param>
        /// <param name="maxAttempts">最大試行回数（デフォルト: 10）</param>
        /// <returns>利用可能なポート番号</returns>
        /// <exception cref="InvalidOperationException">利用可能なポートが見つからない場合</exception>
        public static int FindAvailablePort(int startPort, int maxAttempts = 10)
        {
            for (int i = 0; i < maxAttempts; i++)
            {
                int candidatePort = startPort + i;
                
                // Skip if port is out of valid range
                if (candidatePort > 65535)
                {
                    break;
                }
                
                // Skip commonly used system ports
                if (Array.IndexOf(CommonSystemPorts, candidatePort) != -1)
                {
                    continue;
                }
                
                // Check if port is available
                if (!IsPortInUse(candidatePort))
                {
                    return candidatePort;
                }
            }
            
            // If no available port found, throw exception
            throw new InvalidOperationException(
                $"Could not find an available port starting from {startPort}. Tried {maxAttempts} ports.");
        }

        /// <summary>
        /// ポート番号の有効性を検証する
        /// </summary>
        /// <param name="port">検証対象のポート番号</param>
        /// <param name="parameterName">パラメータ名（エラーメッセージ用）</param>
        /// <returns>有効な場合true</returns>
        public static bool IsValidPort(int port, string parameterName = "port")
        {
            if (port < 1 || port > 65535)
            {
                McpLogger.LogError($"[IsValidPort] Invalid port number {port} for {parameterName}. Must be between 1 and 65535.");
                return false;
            }
            return true;
        }

        #endregion

        #region Process ID Resolution Methods

        /// <summary>
        /// クライアントプロセスIDを取得する（プラットフォーム自動判定）
        /// </summary>
        /// <param name="serverPort">サーバーポート番号</param>
        /// <param name="remotePort">リモートポート番号</param>
        /// <param name="currentUnityPid">現在のUnityプロセスID（除外対象）</param>
        /// <returns>クライアントプロセスID、見つからない場合は UNKNOWN_PROCESS_ID</returns>
        public static int GetClientProcessId(int serverPort, int remotePort, int currentUnityPid)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return GetClientProcessIdWindows(serverPort, remotePort, currentUnityPid);
            }
            else
            {
                return GetClientProcessIdUnix(serverPort, remotePort, currentUnityPid);
            }
        }

        /// <summary>
        /// Windows環境でnetstatコマンドを使用してクライアントプロセスIDを取得する
        /// </summary>
        /// <param name="serverPort">サーバーポート番号</param>
        /// <param name="remotePort">リモートポート番号</param>
        /// <param name="currentUnityPid">現在のUnityプロセスID（除外対象）</param>
        /// <returns>クライアントプロセスID、見つからない場合は UNKNOWN_PROCESS_ID</returns>
        private static int GetClientProcessIdWindows(int serverPort, int remotePort, int currentUnityPid)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "netstat",
                Arguments = "-ano",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            
            Process process = Process.Start(startInfo);
            if (process == null)
            {
                McpLogger.LogError($"[GetClientProcessId] Failed to start netstat command on Windows. Command may not be available.");
                return McpConstants.UNKNOWN_PROCESS_ID;
            }
            
            using (process)
            {
                string output = process.StandardOutput.ReadToEnd();
                string errorOutput = process.StandardError.ReadToEnd();
                process.WaitForExit();
                
                if (process.ExitCode != 0)
                {
                    McpLogger.LogError($"[GetClientProcessId] netstat command failed with exit code {process.ExitCode}. Error: {errorOutput}");
                    return McpConstants.UNKNOWN_PROCESS_ID;
                }
                
                if (string.IsNullOrEmpty(output))
                {
                    McpLogger.LogWarning($"[GetClientProcessId] netstat command returned empty output");
                    return McpConstants.UNKNOWN_PROCESS_ID;
                }
                
                string[] lines = output.Split('\n');
                foreach (string line in lines)
                {
                    // Look for TCP connections involving our server port
                    if (line.Contains("TCP") && line.Contains($":{serverPort}") && line.Contains("ESTABLISHED"))
                    {
                        string[] parts = line.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
                        
                        // netstat output format: Protocol Local_Address Foreign_Address State PID
                        if (parts.Length >= 5 && int.TryParse(parts[4], out int pid))
                        {
                            // Skip Unity's own PID
                            if (pid == currentUnityPid)
                            {
                                continue;
                            }
                            
                            // Check if this is a client connection (foreign address contains our server port)
                            string foreignAddress = parts[2];
                            if (foreignAddress.Contains($":{remotePort}"))
                            {
                                McpLogger.LogInfo($"[GetClientProcessId] Found client PID: {pid} for connection {foreignAddress}");
                                return pid;
                            }
                        }
                    }
                }
            }
            
            return McpConstants.UNKNOWN_PROCESS_ID;
        }

        /// <summary>
        /// Unix環境（macOS/Linux）でlsofコマンドを使用してクライアントプロセスIDを取得する
        /// </summary>
        /// <param name="serverPort">サーバーポート番号</param>
        /// <param name="remotePort">リモートポート番号</param>
        /// <param name="currentUnityPid">現在のUnityプロセスID（除外対象）</param>
        /// <returns>クライアントプロセスID、見つからない場合は UNKNOWN_PROCESS_ID</returns>
        private static int GetClientProcessIdUnix(int serverPort, int remotePort, int currentUnityPid)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = McpConstants.LSOF_COMMAND,
                Arguments = string.Format(McpConstants.LSOF_ARGS_TEMPLATE, serverPort),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            
            Process process = Process.Start(startInfo);
            if (process == null)
            {
                McpLogger.LogError($"[GetClientProcessId] Failed to start lsof command on Unix. Command may not be available. Please ensure lsof is installed.");
                return McpConstants.UNKNOWN_PROCESS_ID;
            }
            
            using (process)
            {
                string output = process.StandardOutput.ReadToEnd();
                string errorOutput = process.StandardError.ReadToEnd();
                process.WaitForExit();
                
                if (process.ExitCode != 0)
                {
                    McpLogger.LogError($"[GetClientProcessId] lsof command failed with exit code {process.ExitCode}. Error: {errorOutput}. Please ensure lsof is installed and accessible.");
                    return McpConstants.UNKNOWN_PROCESS_ID;
                }
                
                if (string.IsNullOrEmpty(output))
                {
                    McpLogger.LogWarning($"[GetClientProcessId] lsof command returned empty output");
                    return McpConstants.UNKNOWN_PROCESS_ID;
                }
                
                string[] lines = output.Split('\n');
                foreach (string line in lines)
                {
                    if ((line.Contains($":{serverPort}") || line.Contains($":{remotePort}")) && !line.StartsWith(McpConstants.LSOF_HEADER_COMMAND))
                    {
                        string[] parts = line.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
                        
                        if (parts.Length >= McpConstants.LSOF_PID_ARRAY_MIN_LENGTH && int.TryParse(parts[McpConstants.LSOF_PID_COLUMN_INDEX], out int pid))
                        {
                            // Skip Unity's own PID and look for the client PID
                            if (pid == currentUnityPid)
                            {
                                continue;
                            }
                            
                            // Check if this line represents an ESTABLISHED connection from client to server
                            if (line.Contains("ESTABLISHED") && line.Contains($":{remotePort}->"))
                            {
                                McpLogger.LogInfo($"[GetClientProcessId] Found client PID: {pid} for connection {line.Trim()}");
                                return pid;
                            }
                        }
                    }
                }
            }
            
            return McpConstants.UNKNOWN_PROCESS_ID;
        }

        #endregion
    }
} 