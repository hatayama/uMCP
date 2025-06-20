using UnityEngine;
using UnityEditor;

namespace io.github.hatayama.uMCP
{
    public class DebugFilterTest
    {
        [MenuItem("Tools/ログゲッター/デバッグ：フィルター詳細テスト")]
        public static void DetailedFilterTest()
        {
            // 1. テストログを複数出力
            Debug.Log("=== デバッグテスト開始 ===");
            Debug.Log("通常ログ1");
            Debug.Log("通常ログ2");
            Debug.LogWarning("警告ログ1");
            Debug.LogWarning("警告ログ2");
            Debug.LogError("エラーログ1");
            Debug.LogError("エラーログ2");
            
            // 2. UnityLogEntriesAccessorを直接テスト
            using (UnityLogEntriesAccessor accessor = new UnityLogEntriesAccessor())
            {
                Debug.Log($"UnityLogEntriesAccessor初期化状態: {accessor.IsInitialized}");
                Debug.Log($"フィルター制御利用可能: {accessor.IsFilterControlAvailable}");
                
                if (accessor.IsInitialized)
                {
                    // 通常のGetLogCount
                    int normalCount = accessor.GetLogCount();
                    Debug.Log($"通常のGetLogCount(): {normalCount}");
                    
                    // フィルター無視のGetLogCountWithAllFlags
                    int allFlagsCount = accessor.GetLogCountWithAllFlags();
                    Debug.Log($"GetLogCountWithAllFlags(): {allFlagsCount}");
                    
                    // LogGetterModel経由でのテスト
                    using (LogGetterModel model = new LogGetterModel())
                    {
                        LogEntryDto[] entries = model.GetConsoleLogEntries();
                        Debug.Log($"LogGetterModel.GetConsoleLogEntries()件数: {entries.Length}");
                        
                        // 各ログタイプ別の件数
                        LogEntryDto[] errorEntries = model.GetConsoleLogEntries("Error");
                        LogEntryDto[] warningEntries = model.GetConsoleLogEntries("Warning");
                        LogEntryDto[] logEntries = model.GetConsoleLogEntries("Log");
                        
                        Debug.Log($"エラーログ: {errorEntries.Length}件");
                        Debug.Log($"警告ログ: {warningEntries.Length}件");
                        Debug.Log($"通常ログ: {logEntries.Length}件");
                    }
                    
                    // LogGetter静的API経由でのテスト
                    int staticCount = LogGetter.GetConsoleLogCount();
                    Debug.Log($"LogGetter.GetConsoleLogCount(): {staticCount}");
                    
                    LogDisplayDto allLogs = LogGetter.GetConsoleLog();
                    Debug.Log($"LogGetter.GetConsoleLog()件数: {allLogs.LogEntries.Length}");
                }
                else
                {
                    Debug.LogError("UnityLogEntriesAccessorの初期化に失敗");
                }
            }
            
            Debug.Log("=== デバッグテスト終了 ===");
        }
        
        [MenuItem("Tools/ログゲッター/デバッグ：現在のConsoleフィルター状態確認")]
        public static void CheckConsoleFilterState()
        {
            Debug.Log("=== 現在のConsoleフィルター状態確認 ===");
            
            using (UnityLogEntriesAccessor accessor = new UnityLogEntriesAccessor())
            {
                Debug.Log($"UnityLogEntriesAccessor初期化状態: {accessor.IsInitialized}");
                Debug.Log($"フィルター制御利用可能: {accessor.IsFilterControlAvailable}");
                
                if (accessor.IsInitialized)
                {
                    // フィルター状態を保存（内部的に現在の状態が取得される）
                    accessor.SaveConsoleFlags();
                    Debug.Log("現在のフィルター状態を保存しました");
                    
                    // 全フィルターをONにして取得
                    accessor.EnableAllConsoleFlags();
                    int allCount = accessor.GetLogCount();
                    Debug.Log($"全フィルターON時のログ数: {allCount}");
                    
                    // フィルター状態を復元
                    accessor.RestoreConsoleFlags();
                    int restoredCount = accessor.GetLogCount();
                    Debug.Log($"フィルター復元後のログ数: {restoredCount}");
                    
                    if (allCount != restoredCount)
                    {
                        Debug.LogWarning("フィルター状態により表示されるログ数が異なります！");
                        Debug.LogWarning("これが修正したい問題です。");
                    }
                    else
                    {
                        Debug.Log("フィルター状態は現在すべてONになっています。");
                    }
                }
                else
                {
                    Debug.LogError("UnityLogEntriesAccessorの初期化に失敗");
                }
            }
            
            Debug.Log("=== フィルター状態確認終了 ===");
        }
    }
}