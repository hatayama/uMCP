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
        /// <summary>
        /// コマンドレジストリを取得する
        /// 新しいコマンドを追加する場合はこのレジストリを使用する
        /// </summary>
        public static UnityCommandRegistry CommandRegistry => CustomCommandManager.GetRegistry();

        /// <summary>
        /// 汎用コマンド実行メソッド
        /// 新しいコマンドベースの構造を使用する
        /// </summary>
        /// <param name="commandName">コマンド名</param>
        /// <param name="paramsToken">パラメータ</param>
        /// <returns>実行結果</returns>
        public static async Task<object> ExecuteCommandAsync(string commandName, JToken paramsToken)
        {
            // 特別なメタコマンドをチェック
            if (commandName == "getAvailableCommands")
            {
                return await HandleGetAvailableCommands(paramsToken);
            }
            if (commandName == "getCommandDetails")
            {
                return await HandleGetCommandDetails(paramsToken);
            }

            return await CustomCommandManager.GetRegistry().ExecuteCommandAsync(commandName, paramsToken);
        }

        /// <summary>
        /// 利用可能なコマンド一覧を取得する
        /// </summary>
        private static Task<object> HandleGetAvailableCommands(JToken request)
        {
            UnityCommandRegistry registry = CustomCommandManager.GetRegistry();
            string[] commandNames = registry.GetRegisteredCommandNames();
            
            McpLogger.LogDebug($"GetAvailableCommands: Returning {commandNames.Length} commands");
            return Task.FromResult<object>(commandNames);
        }

        /// <summary>
        /// コマンドの詳細情報を取得する
        /// </summary>
        private static Task<object> HandleGetCommandDetails(JToken request)
        {
            UnityCommandRegistry registry = CustomCommandManager.GetRegistry();
            CommandInfo[] commands = registry.GetRegisteredCommands();
            
            McpLogger.LogDebug($"GetCommandDetails: Returning {commands.Length} command details");
            return Task.FromResult<object>(commands);
        }
    }
} 