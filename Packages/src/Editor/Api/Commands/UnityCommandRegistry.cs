using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Unity MCPコマンドのレジストリクラス
    /// Open-Closed原則に従い、新しいコマンドの追加を容易にする
    /// </summary>
    public class UnityCommandRegistry
    {
        private readonly Dictionary<CommandType, IUnityCommand> commands = new Dictionary<CommandType, IUnityCommand>();

        /// <summary>
        /// デフォルトコンストラクタ
        /// 標準コマンドを自動登録する
        /// </summary>
        public UnityCommandRegistry()
        {
            RegisterDefaultCommands();
        }

        /// <summary>
        /// 標準コマンドを登録する
        /// </summary>
        private void RegisterDefaultCommands()
        {
            RegisterCommand(new PingCommand());
            RegisterCommand(new CompileCommand());
            RegisterCommand(new GetLogsCommand());
            
            // 新しいコマンドの追加例
            RegisterCommand(new GetVersionCommand());
        }

        /// <summary>
        /// コマンドを登録する
        /// </summary>
        /// <param name="command">登録するコマンド</param>
        public void RegisterCommand(IUnityCommand command)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            commands[command.CommandType] = command;
            McpLogger.LogDebug($"Command registered: {command.CommandType}");
        }

        /// <summary>
        /// コマンドを実行する
        /// </summary>
        /// <param name="commandName">コマンド名</param>
        /// <param name="paramsToken">パラメータ</param>
        /// <returns>実行結果</returns>
        /// <exception cref="ArgumentException">未知のコマンドの場合</exception>
        public async Task<object> ExecuteCommandAsync(string commandName, JToken paramsToken)
        {
            if (!Enum.TryParse<CommandType>(commandName, true, out CommandType commandType))
            {
                throw new ArgumentException($"Unknown command: {commandName}");
            }

            if (!commands.TryGetValue(commandType, out IUnityCommand command))
            {
                throw new ArgumentException($"Command not registered: {commandName}");
            }

            McpLogger.LogDebug($"Executing command: {commandName}");
            await MainThreadSwitcher.SwitchToMainThread();
            return await command.ExecuteAsync(paramsToken);
        }

        /// <summary>
        /// 登録されているコマンド名の一覧を取得する
        /// </summary>
        /// <returns>コマンド名の配列</returns>
        public string[] GetRegisteredCommandNames()
        {
            string[] commandNames = new string[commands.Count];
            int i = 0;
            foreach (CommandType commandType in commands.Keys)
            {
                commandNames[i++] = commandType.GetCommandName();
            }
            return commandNames;
        }

        /// <summary>
        /// 指定されたコマンドが登録されているかを確認する
        /// </summary>
        /// <param name="commandName">コマンド名</param>
        /// <returns>登録されている場合はtrue</returns>
        public bool IsCommandRegistered(string commandName)
        {
            return Enum.TryParse<CommandType>(commandName, true, out CommandType commandType) &&
                   commands.ContainsKey(commandType);
        }
    }
} 