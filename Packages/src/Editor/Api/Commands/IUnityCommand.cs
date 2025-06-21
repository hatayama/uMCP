using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Unity MCPコマンドハンドラーの基底インターフェース
    /// Open-Closed原則に従い、新しいコマンドを追加する際は
    /// このインターフェースを実装した新しいクラスを作成する
    /// </summary>
    public interface IUnityCommand
    {
        /// <summary>
        /// コマンド名を取得する
        /// </summary>
        string CommandName { get; }
        
        /// <summary>
        /// コマンドの説明を取得する
        /// </summary>
        string Description { get; }
        
        /// <summary>
        /// コマンドを実行する
        /// </summary>
        /// <param name="paramsToken">パラメータのJSONトークン</param>
        /// <returns>実行結果</returns>
        Task<object> ExecuteAsync(JToken paramsToken);
    }
} 