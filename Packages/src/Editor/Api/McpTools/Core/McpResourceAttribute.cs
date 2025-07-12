using System;

namespace io.github.hatayama.uLoopMCP
{
    /// <summary>
    /// MCP Resource用のアトリビュート
    /// 
    /// 設計ドキュメント参照: McpToolAttributeと同様の自動発見機能を提供
    /// 
    /// 関連クラス:
    /// - McpResourceManager: このアトリビュートを使用してResourceを自動発見・登録
    /// - McpResourceProvider: このアトリビュートが付与される基底クラス
    /// - McpToolAttribute: Toolsの同等アトリビュート（設計パターンを参考）
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class McpResourceAttribute : Attribute
    {
        /// <summary>
        /// リソースの説明
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 開発者向けリソースかどうか
        /// 開発時のみ表示されるリソース（デバッグ用など）
        /// </summary>
        public bool DisplayDevelopmentOnly { get; set; }

        /// <summary>
        /// リソースのカテゴリ
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// MCP Resourceアトリビュートのコンストラクタ
        /// </summary>
        /// <param name="description">リソースの説明</param>
        public McpResourceAttribute(string description = "")
        {
            Description = description;
            DisplayDevelopmentOnly = false;
            Category = "";
        }
    }
}