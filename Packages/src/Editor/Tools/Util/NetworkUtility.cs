/*
 * NetworkUtility.cs
 * 
 * Design Document: ARCHITECTURE.md - Network Communication
 * Related Classes: McpBridgeServer, McpServerController, ConnectedClient
 * 
 * Provides network-related utility functions
 * Handles port availability checking, process ID resolution, and network connection management
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
    /// Provides network-related utility functions
    /// Handles port availability checking, process ID resolution, and network connection management
    /// </summary>
    public static class NetworkUtility
    {
        /// <summary>
        /// Common system ports to avoid conflicts
        /// </summary>
        private static readonly int[] CommonSystemPorts = { 80, 443, 21, 22, 23, 25, 53, 110, 143, 993, 995, 3389 };

        #region Port Availability Methods

        /// <summary>
        /// Checks if the specified port is currently in use
        /// Uses TCP connection test to determine port availability
        /// </summary>
        /// <param name="port">Port number to check</param>
        /// <returns>True if the port is in use, false if available</returns>
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
        /// Finds an available port starting from the specified port number
        /// Avoids system ports and selects a safe port for use
        /// </summary>
        /// <param name="startPort">Starting port number for search</param>
        /// <param name="maxAttempts">Maximum number of attempts (default: 10)</param>
        /// <returns>Available port number</returns>
        /// <exception cref="InvalidOperationException">Thrown when no available port is found</exception>
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
        /// Validates if the port number is within valid range
        /// </summary>
        /// <param name="port">Port number to validate</param>
        /// <param name="parameterName">Parameter name for error messages</param>
        /// <returns>True if valid, false otherwise</returns>
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
        /// Gets the client process ID with automatic platform detection
        /// </summary>
        /// <param name="serverPort">Server port number</param>
        /// <param name="remotePort">Remote port number</param>
        /// <param name="currentUnityPid">Current Unity process ID to exclude</param>
        /// <returns>Client process ID, or UNKNOWN_PROCESS_ID if not found</returns>
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
        /// Gets client process ID on Windows using netstat command
        /// </summary>
        /// <param name="serverPort">Server port number</param>
        /// <param name="remotePort">Remote port number</param>
        /// <param name="currentUnityPid">Current Unity process ID to exclude</param>
        /// <returns>Client process ID, or UNKNOWN_PROCESS_ID if not found</returns>
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
        /// Gets client process ID on Unix systems (macOS/Linux) using lsof command
        /// </summary>
        /// <param name="serverPort">Server port number</param>
        /// <param name="remotePort">Remote port number</param>
        /// <param name="currentUnityPid">Current Unity process ID to exclude</param>
        /// <returns>Client process ID, or UNKNOWN_PROCESS_ID if not found</returns>
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