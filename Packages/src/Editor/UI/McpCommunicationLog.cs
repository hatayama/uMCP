using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;
using Newtonsoft.Json;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// An entry for the MCP communication log.
    /// Log information that includes a request/response pair.
    /// </summary>
    public record McpCommunicationLogEntry
    {
        public readonly string CommandName;
        public readonly DateTime Timestamp;
        public readonly string Request;
        public readonly string Response;
        public readonly bool IsError;
        public bool IsExpanded { get; set; }

        public string HeaderText => $"[{CommandName}: {Timestamp:HH:mm:ss}]";

        public McpCommunicationLogEntry(string commandName, DateTime timestamp, string request, string response, bool isError, bool isExpanded = false)
        {
            CommandName = commandName;
            Timestamp = timestamp;
            Request = request;
            Response = response;
            IsError = isError;
            IsExpanded = isExpanded;
        }
    }

    /// <summary>
    /// A class for managing MCP communication logs.
    /// </summary>
    public static class McpCommunicationLogger
    {
        private const string LOGS_SESSION_KEY = McpConstants.SESSION_KEY_COMMUNICATION_LOGS;
        private const string PENDING_REQUESTS_SESSION_KEY = McpConstants.SESSION_KEY_PENDING_REQUESTS;

        private static List<McpCommunicationLogEntry> _logs;
        private static Dictionary<string, PendingRequest> _pendingRequests;

        /// <summary>
        /// Event for when the log is updated (for UI updates).
        /// </summary>
        public static event System.Action OnLogUpdated;

        /// <summary>
        /// Static constructor (automatically executed after a domain reload).
        /// </summary>
        static McpCommunicationLogger()
        {
            LoadFromSessionState();
        }

        /// <summary>
        /// Information about pending requests.
        /// </summary>
        private record PendingRequest
        {
            public readonly string CommandName;
            public readonly DateTime Timestamp;
            public readonly string RequestJson;

            public PendingRequest(string commandName, DateTime timestamp, string requestJson)
            {
                CommandName = commandName;
                Timestamp = timestamp;
                RequestJson = requestJson;
            }
        }

        /// <summary>
        /// Gets all logs.
        /// </summary>
        public static IReadOnlyList<McpCommunicationLogEntry> GetAllLogs()
        {
            lock (_logs)
            {
                return new List<McpCommunicationLogEntry>(_logs).AsReadOnly();
            }
        }

        /// <summary>
        /// Records a request (awaiting response).
        /// </summary>
        public static async Task LogRequest(string jsonRequest)
        {
            McpLogger.LogDebug($"LogRequest called: {jsonRequest}");

            JObject request = JObject.Parse(jsonRequest);
            string method = request["method"]?.ToString() ?? "unknown";
            string id = NormalizeId(request["id"]);

            McpLogger.LogDebug($"Storing request with ID: '{id}', Method: {method}");

            PendingRequest pendingRequest = new(method, DateTime.Now, jsonRequest);

            // Store in memory immediately (on any thread)
            lock (_pendingRequests)
            {
                _pendingRequests[id] = pendingRequest;
            }

            // Switch to main thread for SessionState operations
            await MainThreadSwitcher.SwitchToMainThread();
            
            SaveToSessionState();
            OnLogUpdated?.Invoke();

            McpLogger.LogDebug($"Request logged - Method: {method}, ID: {id}");
        }

        /// <summary>
        /// Records a response (adds it to the log paired with its request).
        /// </summary>
        public static async Task RecordLogResponse(string jsonResponse)
        {
            McpLogger.LogDebug($"LogResponse called: {jsonResponse}");

            JObject response = JObject.Parse(jsonResponse);
            string id = NormalizeId(response["id"]);

            McpLogger.LogDebug($"Looking for request with ID: '{id}'");
            
            lock (_pendingRequests)
            {
                McpLogger.LogDebug($"Pending requests count: {_pendingRequests.Count}");
                foreach (var kvp in _pendingRequests)
                {
                    McpLogger.LogDebug($"- Pending ID: '{kvp.Key}', Method: {kvp.Value.CommandName}");
                }
            }

            PendingRequest pendingRequest;
            bool foundPendingRequest;
            
            // Process response in memory immediately (on any thread)
            lock (_pendingRequests)
            {
                foundPendingRequest = _pendingRequests.TryGetValue(id, out pendingRequest);
                if (foundPendingRequest)
                {
                    _pendingRequests.Remove(id);
                }
            }
            
            if (foundPendingRequest)
            {
                bool isError = response["error"] != null;

                // Create new logs in a closed state.
                McpCommunicationLogEntry logEntry = new(
                    pendingRequest.CommandName,
                    pendingRequest.Timestamp,
                    pendingRequest.RequestJson,
                    jsonResponse,
                    isError,
                    false // Start with the toggle closed.
                );

                lock (_logs)
                {
                    // If there are existing logs, close them all (before adding a new log).
                    foreach (McpCommunicationLogEntry existingLog in _logs)
                    {
                        existingLog.IsExpanded = false;
                    }
                    
                    _logs.Add(logEntry);
                }

                McpLogger.LogDebug($"Response logged - Method: {pendingRequest.CommandName}, Total logs: {_logs.Count}");

                // Switch to main thread for SessionState operations
                await MainThreadSwitcher.SwitchToMainThread();
                
                SaveToSessionState();
                OnLogUpdated?.Invoke();
            }
            else
            {
                McpLogger.LogWarning($"No pending request found for response ID: {id}");
            }
        }

        /// <summary>
        /// Clears all logs.
        /// </summary>
        public static void ClearLogs()
        {
            lock (_logs)
            {
                _logs.Clear();
            }
            
            lock (_pendingRequests)
            {
                _pendingRequests.Clear();
            }

            // Also completely delete from SessionState and execute UI update on the main thread.
            EditorApplication.delayCall += () =>
            {
                ClearLogSessionState();
                OnLogUpdated?.Invoke();
            };
        }

        /// <summary>
        /// Restores data from SessionState.
        /// </summary>
        private static void LoadFromSessionState()
        {
            // Restore logs.
            string logsJson = SessionState.GetString(LOGS_SESSION_KEY, "[]");
            try
            {
                _logs = JsonConvert.DeserializeObject<List<McpCommunicationLogEntry>>(logsJson) ?? new List<McpCommunicationLogEntry>();
            }
            catch (Exception ex)
            {
                McpLogger.LogWarning($"Failed to deserialize logs: {ex.Message}");
                _logs = new List<McpCommunicationLogEntry>();
            }

            // Restore pending requests.
            string pendingJson = SessionState.GetString(PENDING_REQUESTS_SESSION_KEY, "{}");
            try
            {
                _pendingRequests = JsonConvert.DeserializeObject<Dictionary<string, PendingRequest>>(pendingJson) ?? new Dictionary<string, PendingRequest>();
            }
            catch (Exception ex)
            {
                McpLogger.LogWarning($"Failed to deserialize pending requests: {ex.Message}");
                _pendingRequests = new Dictionary<string, PendingRequest>();
            }
        }

        /// <summary>
        /// Saves data to SessionState.
        /// </summary>
        public static void SaveToSessionState()
        {
            try
            {
                // Create snapshots to avoid collection modification during serialization
                List<McpCommunicationLogEntry> logsSnapshot;
                Dictionary<string, PendingRequest> pendingSnapshot;
                
                lock (_logs)
                {
                    logsSnapshot = new List<McpCommunicationLogEntry>(_logs);
                }
                
                lock (_pendingRequests)
                {
                    pendingSnapshot = new Dictionary<string, PendingRequest>(_pendingRequests);
                }

                string logsJson = JsonConvert.SerializeObject(logsSnapshot);
                string pendingJson = JsonConvert.SerializeObject(pendingSnapshot);

                SessionState.SetString(LOGS_SESSION_KEY, logsJson);
                SessionState.SetString(PENDING_REQUESTS_SESSION_KEY, pendingJson);
            }
            catch (Exception ex)
            {
                McpLogger.LogError($"Failed to save to SessionState: {ex.Message}");
            }
        }

        /// <summary>
        /// Clears SessionState related to communication logs (for fixing log issues).
        /// </summary>
        public static void ClearLogSessionState()
        {
            try
            {
                SessionState.EraseString(LOGS_SESSION_KEY);
                SessionState.EraseString(PENDING_REQUESTS_SESSION_KEY);
                
                lock (_logs)
                {
                    _logs?.Clear();
                }
                
                lock (_pendingRequests)
                {
                    _pendingRequests?.Clear();
                }

                // SessionState clear complete (no log output).

                // Notify UI of update.
                EditorApplication.delayCall += () => OnLogUpdated?.Invoke();
            }
            catch (Exception ex)
            {
                McpLogger.LogError($"Failed to clear communication log SessionState: {ex.Message}");
            }
        }

        /// <summary>
        /// Normalize ID to consistent string representation for proper matching
        /// </summary>
        private static string NormalizeId(JToken idToken)
        {
            if (idToken == null)
            {
                return "unknown";
            }

            // Convert both string and numeric IDs to string for consistent matching
            return idToken.ToString();
        }
    }
}