using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Unity API呼び出しを専門に扱うクラス
    /// 新しいコマンドベースの構造に対応
    /// </summary>
    public static class UnityApiHandler
    {
        private static readonly UnityCommandRegistry commandRegistry = new UnityCommandRegistry();

        /// <summary>
        /// コマンドレジストリを取得する
        /// 新しいコマンドを追加する場合はこのレジストリを使用する
        /// </summary>
        public static UnityCommandRegistry CommandRegistry => commandRegistry;



        /// <summary>
        /// 汎用コマンド実行メソッド
        /// 新しいコマンドベースの構造を使用する
        /// </summary>
        /// <param name="commandName">コマンド名</param>
        /// <param name="paramsToken">パラメータ</param>
        /// <returns>実行結果</returns>
        public static async Task<object> ExecuteCommandAsync(string commandName, JToken paramsToken)
        {
            return await commandRegistry.ExecuteCommandAsync(commandName, paramsToken);
        }
    }
} 