using UnityEditor;
using UnityEngine;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Unity 6 ConsoleWindowUtility API recreation for older Unity versions
    /// Provides event-driven console log monitoring and simple count retrieval
    /// </summary>
    public static class GenelicConsoleWindowUtility
    {
        private static ConsoleLogRetriever logRetriever;
        private static int lastLogCount = 0;
        private static int lastErrorCount = 0;
        private static int lastWarningCount = 0;
        private static double lastCheckTime = 0;
        private static readonly double CheckInterval = 1.0; // Check every 1000ms (1 second)

        /// <summary>
        /// Event that fires when console logs change (Unity 6 compatible)
        /// </summary>
        public static event System.Action consoleLogsChanged;

        static GenelicConsoleWindowUtility()
        {
            Initialize();
        }

        /// <summary>
        /// Initialize the utility and start monitoring
        /// </summary>
        private static void Initialize()
        {
            try
            {
                logRetriever = new ConsoleLogRetriever();
                EditorApplication.update += CheckForLogChanges;
                Debug.Log("ConsoleWindowUtility initialized successfully");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to initialize ConsoleWindowUtility: {ex.Message}");
            }
        }

        /// <summary>
        /// Monitor for console log changes and fire events
        /// </summary>
        private static void CheckForLogChanges()
        {
            if (logRetriever == null) return;
            
            double currentTime = EditorApplication.timeSinceStartup;
            if (currentTime - lastCheckTime < CheckInterval) return;
            
            lastCheckTime = currentTime;

            try
            {
                // Get current counts
                GetConsoleLogCounts(out int errorCount, out int warningCount, out int logCount);
                
                // Check if anything changed
                if (errorCount != lastErrorCount || warningCount != lastWarningCount || logCount != lastLogCount)
                {
                    lastErrorCount = errorCount;
                    lastWarningCount = warningCount;
                    lastLogCount = logCount;
                    
                    // Fire the event
                    consoleLogsChanged?.Invoke();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error checking for log changes: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets console log counts by type (Unity 6 compatible)
        /// </summary>
        /// <param name="errorCount">Number of error logs</param>
        /// <param name="warningCount">Number of warning logs</param>
        /// <param name="logCount">Number of info logs</param>
        public static void GetConsoleLogCounts(out int errorCount, out int warningCount, out int logCount)
        {
            errorCount = 0;
            warningCount = 0;
            logCount = 0;

            if (logRetriever == null) return;

            try
            {
                // Save current mask state
                int originalMask = logRetriever.GetCurrentMask();
                
                try
                {
                    // Temporarily set mask to show all log types to get accurate counts
                    logRetriever.SetMask(7); // Show all: Error(1) + Warning(2) + Log(4) = 7
                    
                    // Use Unity's internal GetCount method which respects Console clear state
                    int totalCount = logRetriever.GetLogCount();
                    
                    if (totalCount == 0)
                    {
                        // Console is empty, all counts are 0
                        return;
                    }
                    
                    // Get all logs and count by type
                    var allLogs = logRetriever.GetAllLogs();
                    
                    foreach (LogEntryDto log in allLogs)
                    {
                        switch (log.LogType)
                        {
                            case McpLogType.Error:
                                errorCount++;
                                break;
                            case McpLogType.Warning:
                                warningCount++;
                                break;
                            case McpLogType.Log:
                                logCount++;
                                break;
                        }
                    }
                }
                finally
                {
                    // Always restore original mask
                    logRetriever.SetMask(GetSimpleMaskFromUnityMask(originalMask));
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error getting console log counts: {ex.Message}");
            }
        }

        /// <summary>
        /// Clears the Unity Console (Unity 6 compatible)
        /// </summary>
        public static void ClearConsole()
        {
            try
            {
                var logEntriesType = System.Reflection.Assembly.GetAssembly(typeof(EditorWindow))
                    .GetType("UnityEditor.LogEntries");
                
                var clearMethod = logEntriesType.GetMethod("Clear", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                
                if (clearMethod != null)
                {
                    clearMethod.Invoke(null, null);
                    
                    // Reset our tracking counts
                    lastLogCount = 0;
                    lastErrorCount = 0;
                    lastWarningCount = 0;
                    
                    // Add a log message after clearing
                    Debug.Log("=== Console cleared ===");
                    
                    // Fire the change event
                    consoleLogsChanged?.Invoke();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error clearing console: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets all console logs (convenience method)
        /// </summary>
        /// <returns>List of all log entries</returns>
        public static System.Collections.Generic.List<LogEntryDto> GetAllLogs()
        {
            if (logRetriever == null) 
                return new System.Collections.Generic.List<LogEntryDto>();
            
            try
            {
                return logRetriever.GetAllLogs();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error getting all logs: {ex.Message}");
                return new System.Collections.Generic.List<LogEntryDto>();
            }
        }

        /// <summary>
        /// Gets current console mask state
        /// </summary>
        /// <returns>Current console mask</returns>
        public static int GetCurrentMask()
        {
            if (logRetriever == null) return 0;
            
            try
            {
                return logRetriever.GetCurrentMask();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error getting current mask: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Sets console mask state
        /// </summary>
        /// <param name="mask">Mask value to set</param>
        public static void SetMask(int mask)
        {
            if (logRetriever == null) return;
            
            try
            {
                logRetriever.SetMask(mask);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error setting mask: {ex.Message}");
            }
        }

        /// <summary>
        /// Converts Unity internal mask back to simple mask format
        /// </summary>
        /// <param name="unityMask">Unity's internal mask value</param>
        /// <returns>Simple mask (1=Error, 2=Warning, 4=Log)</returns>
        private static int GetSimpleMaskFromUnityMask(int unityMask)
        {
            int simpleMask = 0;
            
            if ((unityMask & 0x200) != 0) // Error bit
                simpleMask |= 1;
            
            if ((unityMask & 0x100) != 0) // Warning bit
                simpleMask |= 2;
                
            if ((unityMask & 0x80) != 0) // Log bit
                simpleMask |= 4;
            
            return simpleMask;
        }

        /// <summary>
        /// Cleanup when domain reloads
        /// </summary>
        [UnityEditor.InitializeOnLoadMethod]
        private static void OnDomainReload()
        {
            EditorApplication.update -= CheckForLogChanges;
            Initialize();
        }
    }
}