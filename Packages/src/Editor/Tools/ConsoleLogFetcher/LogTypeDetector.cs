namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// ログのタイプを判定するためのユーティリティクラス
    /// </summary>
    public static class LogTypeDetector
    {
        private const int LOG_MODE_WARNING = 1;
        private const int LOG_MODE_ERROR = 2;
        private const int LOG_MODE_ASSERT = 3;
        private const int LOG_MODE_EXCEPTION = 4;

        public static string DetermineLogType(int mode, string message)
        {
            if (string.IsNullOrEmpty(message))
                return "Log";

            // Unity コンパイルエラー/警告の正確な判定（最優先）
            if (IsCompilerError(message))
                return "Error";
            if (IsCompilerWarning(message))
                return "Warning";
            
            // Unity例外の判定
            if (IsUnityException(message))
                return "Error";
            
            // Unity Assert の判定
            if (IsUnityAssert(message))
                return "Assert";
            
            // modeによる判定（フォールバック）
            string typeFromMode = GetLogTypeFromMode(mode);
            if (typeFromMode != "Log")
                return typeFromMode;

            // その他のエラー/警告パターン
            if (IsGeneralError(message))
                return "Error";
            if (IsGeneralWarning(message))
                return "Warning";

            return "Log";
        }

        private static string GetLogTypeFromMode(int mode)
        {
            switch (mode)
            {
                case LOG_MODE_WARNING:
                    return "Warning";
                case LOG_MODE_ERROR:
                case LOG_MODE_EXCEPTION:
                    return "Error";
                case LOG_MODE_ASSERT:
                    return "Assert";
                default:
                    return "Log";
            }
        }

        /// <summary>
        /// Unityコンパイルエラーかどうかを判定
        /// </summary>
        private static bool IsCompilerError(string message)
        {
            return message.Contains(": error CS") || 
                   message.Contains("): error CS") ||
                   message.Contains("error CS") && (message.Contains(".cs(") || message.Contains(".cs:"));
        }

        /// <summary>
        /// Unityコンパイル警告かどうかを判定
        /// </summary>
        private static bool IsCompilerWarning(string message)
        {
            return message.Contains(": warning CS") || 
                   message.Contains("): warning CS") ||
                   message.Contains("warning CS") && (message.Contains(".cs(") || message.Contains(".cs:"));
        }

        /// <summary>
        /// Unity例外かどうかを判定
        /// </summary>
        private static bool IsUnityException(string message)
        {
            return message.Contains("Exception:") ||
                   message.Contains("NullReferenceException") ||
                   message.Contains("ArgumentException") ||
                   message.Contains("InvalidOperationException") ||
                   message.Contains("UnityException") ||
                   message.EndsWith("Exception");
        }

        /// <summary>
        /// Unity Assertかどうかを判定
        /// </summary>
        private static bool IsUnityAssert(string message)
        {
            return message.StartsWith("Assertion failed") ||
                   message.Contains("UnityEngine.Assertions") ||
                   message.Contains("Assert.");
        }

        /// <summary>
        /// 一般的なエラーパターンかどうかを判定
        /// </summary>
        private static bool IsGeneralError(string message)
        {
            string lowerMessage = message.ToLower();
            return (lowerMessage.Contains("error") && !lowerMessage.Contains("no error")) ||
                   lowerMessage.Contains("failed") ||
                   lowerMessage.Contains("exception");
        }

        /// <summary>
        /// 一般的な警告パターンかどうかを判定
        /// </summary>
        private static bool IsGeneralWarning(string message)
        {
            string lowerMessage = message.ToLower();
            return lowerMessage.Contains("warning") ||
                   lowerMessage.Contains("deprecated") ||
                   lowerMessage.Contains("obsolete");
        }
    }
} 