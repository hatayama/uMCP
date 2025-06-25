using UnityEditor;
using System;
using Newtonsoft.Json;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// SessionState management class for forced recompilation
    /// Preserves request information even when assembly reload occurs
    /// </summary>
    public static class CompileSessionState
    {
        private const string SESSION_KEY_PREFIX = "uMCP.CompileRequest.";
        private const string PENDING_REQUESTS_KEY = "uMCP.PendingCompileRequests";

        /// <summary>
        /// Forced recompilation request information
        /// </summary>
        [Serializable]
        public class CompileRequestInfo
        {
            public string requestId;
            public bool forceRecompile;
            public string clientEndpoint;
            public DateTime requestTime;
            public bool isCompleted;

            public CompileRequestInfo(string requestId, bool forceRecompile, string clientEndpoint)
            {
                this.requestId = requestId;
                this.forceRecompile = forceRecompile;
                this.clientEndpoint = clientEndpoint;
                this.requestTime = DateTime.Now;
                this.isCompleted = false;
            }
        }

        /// <summary>
        /// Save forced recompilation request
        /// </summary>
        public static void SaveCompileRequest(string requestId, bool forceRecompile, string clientEndpoint = "unknown")
        {
            if (!forceRecompile) return; // Don't save normal compilation

            CompileRequestInfo requestInfo = new CompileRequestInfo(requestId, forceRecompile, clientEndpoint);
            string json = JsonConvert.SerializeObject(requestInfo);
            
            SessionState.SetString(SESSION_KEY_PREFIX + requestId, json);
            
            // Add to pending request list
            string[] pendingRequests = GetPendingRequestIds();
            string[] newPendingRequests = new string[pendingRequests.Length + 1];
            Array.Copy(pendingRequests, newPendingRequests, pendingRequests.Length);
            newPendingRequests[pendingRequests.Length] = requestId;
            
            SessionState.SetString(PENDING_REQUESTS_KEY, JsonConvert.SerializeObject(newPendingRequests));
            
        }

        /// <summary>
        /// Get pending request IDs
        /// </summary>
        public static string[] GetPendingRequestIds()
        {
            string json = SessionState.GetString(PENDING_REQUESTS_KEY, "[]");
            try
            {
                return JsonConvert.DeserializeObject<string[]>(json) ?? new string[0];
            }
            catch
            {
                return new string[0];
            }
        }

        /// <summary>
        /// Get saved request information
        /// </summary>
        public static CompileRequestInfo GetCompileRequest(string requestId)
        {
            string json = SessionState.GetString(SESSION_KEY_PREFIX + requestId, null);
            if (string.IsNullOrEmpty(json)) return null;

            try
            {
                return JsonConvert.DeserializeObject<CompileRequestInfo>(json);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Mark request as completed
        /// </summary>
        public static void MarkRequestCompleted(string requestId)
        {
            CompileRequestInfo requestInfo = GetCompileRequest(requestId);
            if (requestInfo == null) return;

            requestInfo.isCompleted = true;
            string json = JsonConvert.SerializeObject(requestInfo);
            SessionState.SetString(SESSION_KEY_PREFIX + requestId, json);

            // Remove from pending request list
            RemoveFromPendingRequests(requestId);
            
        }

        /// <summary>
        /// Remove from pending request list
        /// </summary>
        private static void RemoveFromPendingRequests(string requestId)
        {
            string[] pendingRequests = GetPendingRequestIds();
            string[] newPendingRequests = new string[pendingRequests.Length];
            int newIndex = 0;
            
            for (int i = 0; i < pendingRequests.Length; i++)
            {
                if (pendingRequests[i] != requestId)
                {
                    newPendingRequests[newIndex++] = pendingRequests[i];
                }
            }
            
            // Adjust array size
            if (newIndex < newPendingRequests.Length)
            {
                string[] trimmedArray = new string[newIndex];
                Array.Copy(newPendingRequests, trimmedArray, newIndex);
                newPendingRequests = trimmedArray;
            }
            
            SessionState.SetString(PENDING_REQUESTS_KEY, JsonConvert.SerializeObject(newPendingRequests));
        }

        /// <summary>
        /// Start forced recompilation
        /// </summary>
        public static void StartForceRecompile()
        {
            UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation(
                UnityEditor.Compilation.RequestScriptCompilationOptions.CleanBuildCache
            );
        }

        /// <summary>
        /// Clear SessionState
        /// </summary>
        public static void ClearAll()
        {
            string[] pendingRequests = GetPendingRequestIds();
            foreach (string requestId in pendingRequests)
            {
                SessionState.EraseString(SESSION_KEY_PREFIX + requestId);
            }
            SessionState.EraseString(PENDING_REQUESTS_KEY);
            
        }
    }
} 