namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// ログのタイプを判定するためのユーティリティクラス
    /// </summary>
    public static class LogTypeDetector
    {
        // Unity LogModeの定数
        private const int LOG_MODE_WARNING = 1;
        private const int LOG_MODE_ERROR = 2;
        private const int LOG_MODE_ASSERT = 3;
        private const int LOG_MODE_EXCEPTION = 4;
        
        // ログタイプ名の定数
        private const string LogTypeName = "Log";
        private const string WarningTypeName = "Warning";
        private const string ErrorTypeName = "Error";
        private const string AssertTypeName = "Assert";

        public static string DetermineLogType(int mode)
        {
            // Unity LogEntryのmodeからログタイプを直接判定
            return GetLogTypeFromMode(mode);
        }

        private static string GetLogTypeFromMode(int mode)
        {
            switch (mode)
            {
                case LOG_MODE_WARNING:
                    return WarningTypeName;
                case LOG_MODE_ERROR:
                case LOG_MODE_EXCEPTION:
                    return ErrorTypeName;
                case LOG_MODE_ASSERT:
                    return AssertTypeName;
                default:
                    return LogTypeName;
            }
        }
    }
} 