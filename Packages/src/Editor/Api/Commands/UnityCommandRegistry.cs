using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Unity MCPコマンドのレジストリクラス
    /// 動的なコマンド登録をサポートし、ユーザーが独自のコマンドを追加できる
    /// </summary>
    public class UnityCommandRegistry
    {
        private readonly Dictionary<string, IUnityCommand> commands = new Dictionary<string, IUnityCommand>();

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
            RegisterCommand(new RunTestsCommand());
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

            if (string.IsNullOrWhiteSpace(command.CommandName))
            {
                throw new ArgumentException("Command name cannot be null or empty", nameof(command));
            }

            commands[command.CommandName] = command;
            McpLogger.LogDebug($"Command registered: {command.CommandName}");
        }

        /// <summary>
        /// コマンドを登録解除する
        /// </summary>
        /// <param name="commandName">登録解除するコマンド名</param>
        public void UnregisterCommand(string commandName)
        {
            if (commands.Remove(commandName))
            {
                McpLogger.LogDebug($"Command unregistered: {commandName}");
            }
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
            if (!commands.TryGetValue(commandName, out IUnityCommand command))
            {
                throw new ArgumentException($"Unknown command: {commandName}");
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
            return commands.Keys.ToArray();
        }

        /// <summary>
        /// 登録されているコマンドの詳細情報を取得する
        /// </summary>
        /// <returns>コマンド情報の配列</returns>
        public CommandInfo[] GetRegisteredCommands()
        {
            return commands.Values.Select(cmd => new CommandInfo(cmd.CommandName, cmd.Description)).ToArray();
        }

        /// <summary>
        /// 指定されたコマンドが登録されているかを確認する
        /// </summary>
        /// <param name="commandName">コマンド名</param>
        /// <returns>登録されている場合はtrue</returns>
        public bool IsCommandRegistered(string commandName)
        {
            return commands.ContainsKey(commandName);
        }
    }

    /// <summary>
    /// コマンド情報を表すクラス
    /// </summary>
    public class CommandInfo
    {
        [JsonProperty("name")]
        public string Name { get; }
        
        [JsonProperty("description")]
        public string Description { get; }

        public CommandInfo(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }
} 