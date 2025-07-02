using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace io.github.hatayama.uMCP
{
    public sealed class McpSessionManager : ScriptableSingleton<McpSessionManager>
    {
        [SerializeField] private bool isServerRunning;
        [SerializeField] private int serverPort = McpServerConfig.DEFAULT_PORT;
        [SerializeField] private bool isAfterCompile;
        [SerializeField] private bool isDomainReloadInProgress;
        [SerializeField] private bool isReconnecting;
        [SerializeField] private bool showReconnectingUI;
        [SerializeField] private bool showPostCompileReconnectingUI;
        [SerializeField] private int selectedEditorType = (int)McpEditorType.Cursor;
        [SerializeField] private float communicationLogHeight = McpUIConstants.DEFAULT_COMMUNICATION_LOG_HEIGHT;
        [SerializeField] private string communicationLogsJson = "[]";
        [SerializeField] private string pendingRequestsJson = "{}";
        [SerializeField] private string compileWindowLogText = "";
        [SerializeField] private bool compileWindowHasData;
        [SerializeField] private List<string> pendingCompileRequestIds = new List<string>();
        [SerializeField] private List<CompileRequestData> compileRequests = new List<CompileRequestData>();

        [System.Serializable]
        public class CompileRequestData
        {
            public string requestId;
            public string json;
        }

        // Server関連
        public bool IsServerRunning
        {
            get => isServerRunning;
            set => isServerRunning = value;
        }

        public int ServerPort
        {
            get => serverPort;
            set => serverPort = value;
        }

        public bool IsAfterCompile
        {
            get => isAfterCompile;
            set => isAfterCompile = value;
        }

        public bool IsDomainReloadInProgress
        {
            get => isDomainReloadInProgress;
            set => isDomainReloadInProgress = value;
        }

        public bool IsReconnecting
        {
            get => isReconnecting;
            set => isReconnecting = value;
        }

        public bool ShowReconnectingUI
        {
            get => showReconnectingUI;
            set { showReconnectingUI = value; }
        }

        public bool ShowPostCompileReconnectingUI
        {
            get => showPostCompileReconnectingUI;
            set { showPostCompileReconnectingUI = value; }
        }

        // UI関連
        public McpEditorType SelectedEditorType
        {
            get => (McpEditorType)selectedEditorType;
            set => selectedEditorType = (int)value;
        }

        public float CommunicationLogHeight
        {
            get => communicationLogHeight;
            set => communicationLogHeight = value;
        }

        // Communication Log関連
        public string CommunicationLogsJson
        {
            get => communicationLogsJson;
            set => communicationLogsJson = value;
        }

        public string PendingRequestsJson
        {
            get => pendingRequestsJson;
            set => pendingRequestsJson = value;
        }

        // CompileWindow関連
        public string CompileWindowLogText
        {
            get => compileWindowLogText;
            set => compileWindowLogText = value;
        }

        public bool CompileWindowHasData
        {
            get => compileWindowHasData;
            set => compileWindowHasData = value;
        }

        // CompileSessionState関連のプロパティ
        public string[] PendingCompileRequestIds
        {
            get => pendingCompileRequestIds.ToArray();
            set => pendingCompileRequestIds = new List<string>(value);
        }

        // メソッド群

        public void ClearServerSession()
        {
            isServerRunning = false;
            serverPort = McpServerConfig.DEFAULT_PORT;
        }

        public void ClearAfterCompileFlag()
        {
            isAfterCompile = false;
        }

        public void ClearReconnectingFlags()
        {
            isReconnecting = false;
            showReconnectingUI = false;
        }

        public void ClearPostCompileReconnectingUI()
        {
            showPostCompileReconnectingUI = false;
        }

        public void ClearDomainReloadFlag()
        {
            isDomainReloadInProgress = false;
        }

        public void ClearCommunicationLogs()
        {
            communicationLogsJson = "[]";
            pendingRequestsJson = "{}";
        }

        public void ClearCompileWindowData()
        {
            compileWindowLogText = "";
            compileWindowHasData = false;
        }

        public string GetCompileRequestJson(string requestId)
        {
            CompileRequestData request = compileRequests.Find(r => r.requestId == requestId);
            return request?.json;
        }

        public void SetCompileRequestJson(string requestId, string json)
        {
            CompileRequestData existingRequest = compileRequests.Find(r => r.requestId == requestId);
            if (existingRequest != null)
            {
                existingRequest.json = json;
            }
            else
            {
                compileRequests.Add(new CompileRequestData { requestId = requestId, json = json });
            }
        }

        public void ClearCompileRequest(string requestId)
        {
            compileRequests.RemoveAll(r => r.requestId == requestId);
        }

        public void ClearAllCompileRequests()
        {
            compileRequests.Clear();
            pendingCompileRequestIds.Clear();
        }

        public void AddPendingCompileRequest(string requestId)
        {
            if (!pendingCompileRequestIds.Contains(requestId))
            {
                pendingCompileRequestIds.Add(requestId);
            }
        }

        public void RemovePendingCompileRequest(string requestId)
        {
            pendingCompileRequestIds.Remove(requestId);
        }

        // SessionState全体をクリアする危険な操作（開発時のみ使用）
        public void ClearAllSessionData()
        {
            ClearServerSession();
            ClearAfterCompileFlag();
            ClearReconnectingFlags();
            ClearDomainReloadFlag();
            ClearCommunicationLogs();
            ClearCompileWindowData();
            ClearAllCompileRequests();
        }
    }
}