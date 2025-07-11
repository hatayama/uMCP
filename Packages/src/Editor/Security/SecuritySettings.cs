using System.ComponentModel;

namespace io.github.hatayama.uLoopMCP
{
    /// <summary>
    /// セキュリティ設定の種類を定義するenum
    /// 各セキュリティ設定は、特定のコマンドの実行を制御します
    /// 
    /// 関連クラス:
    /// - McpSecurityChecker: セキュリティチェックのメインロジック
    /// - McpToolAttribute: コマンドのセキュリティ設定を指定する属性
    /// - ExecuteMenuItemCommand: メニューアイテム実行コマンド
    /// - RunTestsCommand: テスト実行コマンド
    /// </summary>
    public enum SecuritySettings
    {
        /// <summary>
        /// セキュリティ設定が不要（デフォルト）
        /// </summary>
        [Description("")]
        None,

        /// <summary>
        /// テストの実行を許可する設定
        /// run-testsコマンドで使用
        /// </summary>
        [Description("enableTestsExecution")]
        EnableTestsExecution,

        /// <summary>
        /// メニューアイテムの実行を許可する設定
        /// execute-menu-itemコマンドで使用
        /// </summary>
        [Description("allowMenuItemExecution")]
        AllowMenuItemExecution
    }

    /// <summary>
    /// SecuritySettings enumの拡張メソッド
    /// </summary>
    public static class SecuritySettingsExtensions
    {
        /// <summary>
        /// SecuritySettings enumから文字列値を取得
        /// </summary>
        /// <param name="setting">SecuritySettings enum値</param>
        /// <returns>対応する文字列値</returns>
        public static string ToStringValue(this SecuritySettings setting)
        {
            var field = setting.GetType().GetField(setting.ToString());
            var attribute = (DescriptionAttribute)System.Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute));
            return attribute?.Description ?? setting.ToString();
        }
    }
}