using UnityEngine;
using UnityEditor;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// LogGetter汎用APIの使用例
    /// </summary>
    public class LogGetterUsageExample
    {
        
        [MenuItem("Tools/ログゲッター/LogGetter直接テスト")]
        public static void TestLogGetter()
        {
            Debug.Log("=== LogGetter直接テスト開始 ===");

            var logData = LogGetter.GetConsoleLog();
            Debug.Log($"LogGetter結果: TotalCount={logData.TotalCount}, LogEntries.Length={logData.LogEntries.Length}");

            foreach (var entry in logData.LogEntries)
            {
                Debug.Log($"ログエントリ: Type={entry.LogType}, Message={entry.Message}");
            }

            Debug.Log("=== LogGetter直接テスト終了 ===");
        }

        [MenuItem("Tools/ログゲッター/使用例実行")]
        public static void RunUsageExamples()
        {
            Debug.Log("=== LogGetter使用例開始 ===");

            // 基本的な使用方法
            BasicUsage();

            // フィルタリング使用例
            FilteringUsage();

            // ログ数取得例
            CountUsage();

            Debug.Log("=== LogGetter使用例完了 ===");
        }

        private static void BasicUsage()
        {
            Debug.Log("--- 基本的な使用方法 ---");

            // まさみちが要求した使い方
            LogDisplayDto log = LogGetter.GetConsoleLog();
            Debug.Log($"取得したログ数: {log.TotalCount}");

            // ログエントリを直接取得
            LogEntryDto[] entries = LogGetter.GetConsoleLogEntries();
            Debug.Log($"ログエントリ配列の長さ: {entries.Length}");

            // 各ログの詳細を表示
            foreach (LogEntryDto entry in entries)
            {
                Debug.Log($"[{entry.LogType}] {entry.Message}");
            }
        }

        private static void FilteringUsage()
        {
            Debug.Log("--- フィルタリング使用例 ---");

            // エラーログのみ取得
            LogDisplayDto errorLogs = LogGetter.GetConsoleLog("Error");
            Debug.Log($"エラーログ数: {errorLogs.TotalCount}");

            // 警告ログのみ取得
            LogDisplayDto warningLogs = LogGetter.GetConsoleLog("Warning");
            Debug.Log($"警告ログ数: {warningLogs.TotalCount}");

            // 通常ログのみ取得
            LogDisplayDto normalLogs = LogGetter.GetConsoleLog("Log");
            Debug.Log($"通常ログ数: {normalLogs.TotalCount}");
        }

        private static void CountUsage()
        {
            Debug.Log("--- ログ数取得例 ---");

            // ログの総数のみ取得（軽量）
            int totalCount = LogGetter.GetConsoleLogCount();
            Debug.Log($"Console総ログ数: {totalCount}");
        }

        [MenuItem("Tools/ログゲッター/カスタム処理例")]
        public static void CustomProcessingExample()
        {
            Debug.Log("=== カスタム処理例 ===");

            // ログを取得してカスタム処理
            LogDisplayDto logs = LogGetter.GetConsoleLog();

            int errorCount = 0;
            int warningCount = 0;
            int logCount = 0;

            foreach (LogEntryDto entry in logs.LogEntries)
            {
                switch (entry.LogType)
                {
                    case "Error":
                        errorCount++;
                        break;
                    case "Warning":
                        warningCount++;
                        break;
                    case "Log":
                        logCount++;
                        break;
                }
            }

            Debug.Log($"ログ統計 - エラー: {errorCount}, 警告: {warningCount}, 通常: {logCount}");
        }
    }
}