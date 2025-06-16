namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Unityコマンドの種類を定義する列挙型
    /// </summary>
    public enum CommandType
    {
        Ping,
        Compile,
        GetLogs,
        RunTests,
        GetVersion
    }

    /// <summary>
    /// CommandTypeの拡張メソッド
    /// </summary>
    public static class CommandTypeExtensions
    {
        /// <summary>
        /// コマンド名を文字列として取得する
        /// </summary>
        public static string GetCommandName(this CommandType commandType)
        {
            return commandType.ToString().ToLower();
        }
    }
} 