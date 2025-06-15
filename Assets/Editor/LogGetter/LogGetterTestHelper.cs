using UnityEngine;
using UnityEditor;

namespace io.github.hatayama.uMCP
{
    public class LogGetterTestHelper
    {
        [MenuItem("Tools/ログゲッター/テストログ出力")]
        public static void OutputTestLogs()
        {
            Debug.Log("これは通常のログじゃ");
            Debug.LogWarning("これは警告ログじゃで");
            Debug.LogError("これはエラーログじゃけぇ");
            
            Debug.Log("スタックトレース付きのログじゃ\nUnityEngine.Debug:Log(Object)\nLogGetterTestHelper:OutputTestLogs() (at Assets/Editor/LogGetter/LogGetterTestHelper.cs:12)");
            
            Debug.LogException(new System.Exception("テスト用の例外じゃ"));
            
            Debug.Log("ログゲッターのテスト完了じゃ！");
            
            Debug.Log("追加ログ1: 情報メッセージ");
            Debug.Log("追加ログ2: デバッグ情報");
            Debug.LogWarning("追加警告: 注意が必要じゃ");
            Debug.LogError("追加エラー: 問題が発生したで");
        }
    }
} 