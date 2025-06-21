using System;
using System.Collections.Generic;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// カスタムコマンドを管理する静的クラス
    /// ユーザーが独自のコマンドを登録・管理できる
    /// </summary>
    public static class CustomCommandManager
    {
        private static UnityCommandRegistry _sharedRegistry;

        /// <summary>
        /// 共有レジストリを取得する（遅延初期化）
        /// </summary>
        private static UnityCommandRegistry SharedRegistry
        {
            get
            {
                if (_sharedRegistry == null)
                {
                    _sharedRegistry = new UnityCommandRegistry();
                    // UnityCommandRegistryのコンストラクタで標準コマンドが自動登録される
                }
                return _sharedRegistry;
            }
        }

        /// <summary>
        /// カスタムコマンドを登録する
        /// </summary>
        /// <param name="command">登録するコマンド</param>
        public static void RegisterCustomCommand(IUnityCommand command)
        {
            SharedRegistry.RegisterCommand(command);
            McpLogger.LogInfo($"Custom command registered: {command.CommandName}");
        }

        /// <summary>
        /// カスタムコマンドを登録解除する
        /// </summary>
        /// <param name="commandName">登録解除するコマンド名</param>
        public static void UnregisterCustomCommand(string commandName)
        {
            SharedRegistry.UnregisterCommand(commandName);
            McpLogger.LogInfo($"Custom command unregistered: {commandName}");
        }

        /// <summary>
        /// 登録されているすべてのコマンドの一覧を取得する（標準+カスタム）
        /// </summary>
        /// <returns>コマンド情報の配列</returns>
        public static CommandInfo[] GetRegisteredCustomCommands()
        {
            return SharedRegistry.GetRegisteredCommands();
        }

        /// <summary>
        /// 指定されたコマンドが登録されているかを確認する
        /// </summary>
        /// <param name="commandName">コマンド名</param>
        /// <returns>登録されている場合はtrue</returns>
        public static bool IsCustomCommandRegistered(string commandName)
        {
            return SharedRegistry.IsCommandRegistered(commandName);
        }

        /// <summary>
        /// 内部レジストリを取得する（MCPサーバー用）
        /// </summary>
        /// <returns>UnityCommandRegistryインスタンス</returns>
        internal static UnityCommandRegistry GetRegistry()
        {
            return SharedRegistry;
        }

        /// <summary>
        /// デバッグ用：登録されているコマンド名の配列を取得する
        /// </summary>
        /// <returns>コマンド名の配列</returns>
        public static string[] GetRegisteredCommandNames()
        {
            return SharedRegistry.GetRegisteredCommandNames();
        }

        /// <summary>
        /// デバッグ用：レジストリの詳細情報を取得する
        /// </summary>
        /// <returns>デバッグ情報</returns>
        public static string GetDebugInfo()
        {
            return $"Registry instance: {SharedRegistry.GetHashCode()}, Commands: [{string.Join(", ", SharedRegistry.GetRegisteredCommandNames())}]";
        }
    }
} 