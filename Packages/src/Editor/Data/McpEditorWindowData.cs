using UnityEditor;
using UnityEngine;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// McpEditorWindowのUI設定を永続化するScriptableSingleton
    /// Domain Reloadを越えてUI状態を保持する
    /// </summary>
    [FilePath("Library/UnityMcpEditorWindow.asset", FilePathAttribute.Location.ProjectFolder)]
    public class McpEditorWindowData : ScriptableSingleton<McpEditorWindowData>
    {
        [SerializeField] private McpEditorType selectedEditorType = McpEditorType.Cursor;
        [SerializeField] private float communicationLogHeight = 300f;

        /// <summary>
        /// 選択されているエディタタイプ
        /// </summary>
        public McpEditorType SelectedEditorType
        {
            get => selectedEditorType;
            set
            {
                if (selectedEditorType != value)
                {
                    selectedEditorType = value;
                    SaveSafe();
                }
            }
        }

        /// <summary>
        /// 通信ログエリアの高さ
        /// </summary>
        public float CommunicationLogHeight
        {
            get => communicationLogHeight;
            set
            {
                if (Mathf.Abs(communicationLogHeight - value) > 0.01f)
                {
                    communicationLogHeight = Mathf.Clamp(value, 100f, 800f);
                    SaveSafe();
                }
            }
        }

        /// <summary>
        /// デバッグ情報を取得
        /// </summary>
        public string GetDebugInfo()
        {
            return $"McpEditorWindowData: EditorType={selectedEditorType}, LogHeight={communicationLogHeight}";
        }

        /// <summary>
        /// メインスレッドでセーフにSave()を実行する
        /// </summary>
        private void SaveSafe()
        {
            Save(true);
        }
    }
}