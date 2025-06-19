using UnityEditor;
using UnityEngine;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// MCPサーバーの状態を永続化するScriptableSingleton
    /// Domain Reloadに関係なく状態を保持する
    /// </summary>
    [FilePath("Library/UnityMcpServer.asset", FilePathAttribute.Location.ProjectFolder)]
    public class McpServerData : ScriptableSingleton<McpServerData>
    {
        [SerializeField] private bool isServerRunning = false;
        [SerializeField] private int serverPort = McpServerConfig.DEFAULT_PORT;
        [SerializeField] private bool autoStartEnabled = false;
        [SerializeField] private string lastStartTime = "";
        [SerializeField] private string lastStopTime = "";

        /// <summary>
        /// サーバーが実行中かどうか
        /// </summary>
        public bool IsServerRunning
        {
            get => isServerRunning;
            set
            {
                if (isServerRunning != value)
                {
                    isServerRunning = value;
                    if (value)
                    {
                        lastStartTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    }
                    else
                    {
                        lastStopTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    }
                    SaveSafe();
                }
            }
        }

        /// <summary>
        /// サーバーのポート番号
        /// </summary>
        public int ServerPort
        {
            get => serverPort;
            set
            {
                if (serverPort != value)
                {
                    serverPort = value;
                    SaveSafe();
                }
            }
        }

        /// <summary>
        /// 自動起動が有効かどうか
        /// </summary>
        public bool AutoStartEnabled
        {
            get => autoStartEnabled;
            set
            {
                if (autoStartEnabled != value)
                {
                    autoStartEnabled = value;
                    SaveSafe();
                }
            }
        }

        /// <summary>
        /// 最後にサーバーを開始した時刻
        /// </summary>
        public string LastStartTime => lastStartTime;

        /// <summary>
        /// 最後にサーバーを停止した時刻
        /// </summary>
        public string LastStopTime => lastStopTime;

        /// <summary>
        /// データをリセットする
        /// </summary>
        public void Reset()
        {
            isServerRunning = false;
            serverPort = McpServerConfig.DEFAULT_PORT;
            autoStartEnabled = false;
            lastStartTime = "";
            lastStopTime = "";
            SaveSafe();
        }

        /// <summary>
        /// デバッグ用の情報を取得
        /// </summary>
        public string GetDebugInfo()
        {
            return $"McpServerData: Running={isServerRunning}, Port={serverPort}, AutoStart={autoStartEnabled}, LastStart={lastStartTime}, LastStop={lastStopTime}";
        }

        /// <summary>
        /// メインスレッドでセーフにSave()を実行する
        /// </summary>
        private void SaveSafe()
        {
            if (MainThreadSwitcher.IsMainThread)
            {
                Save(true);
            }
            else
            {
                // 別スレッドからの場合はUnitySynchronizationContextを使って同期実行
                bool completed = false;
                System.Exception thrownException = null;
                
                MainThreadSwitcher.UnitySynchronizationContext.Post(_ =>
                {
                    try
                    {
                        Save(true);
                    }
                    catch (System.Exception ex)
                    {
                        thrownException = ex;
                    }
                    finally
                    {
                        completed = true;
                    }
                }, null);
                
                // 完了まで待機
                while (!completed)
                {
                    System.Threading.Thread.Sleep(1);
                }
                
                if (thrownException != null)
                {
                    throw thrownException;
                }
            }
        }
    }
}