namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// コマンドの種類を定義するenum
    /// </summary>
    public enum CommandType
    {
        Compile,
        GetLogs,
        GetVersion,
        Ping,
        RunTests
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