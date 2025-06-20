using System;
using System.Reflection;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// UnityのLogEntryオブジェクトをLogEntryDTOに変換するクラス
    /// </summary>
    public static class LogEntryConverter
    {
        public static LogEntryDto ConvertToDto(object logEntry)
        {
            if (logEntry == null)
                return null;

            // Unity LogEntryフィールドから情報を取得
            string fullMessage = GetField(logEntry, "message");
            string file = GetField(logEntry, "file");
            int mode = GetFieldInt(logEntry, "mode");
            int callstackStart = GetFieldInt(logEntry, "callstackTextStartUTF8");

            // callstackTextStartUTF8を使用してメッセージとスタックトレースを分離
            string pureMessage = fullMessage;
            string stackTrace = "";
            
            if (!string.IsNullOrEmpty(fullMessage) && callstackStart > 0 && callstackStart < fullMessage.Length)
            {
                // callstackStartの位置でメッセージとスタックトレースを分離
                pureMessage = fullMessage.Substring(0, callstackStart).Trim();
                stackTrace = fullMessage.Substring(callstackStart).Trim();
            }
            else if (!string.IsNullOrEmpty(fullMessage))
            {
                // callstackStartが無効な値の場合は全体をメッセージとして扱う
                pureMessage = fullMessage.Trim();
            }

            // ログタイプの判定
            string logType = LogTypeDetector.DetermineLogType(mode);

            return new LogEntryDto(pureMessage, logType, stackTrace, file);
        }
        

        /// <summary>
        /// フィールドから文字列値を取得する
        /// </summary>
        private static string GetField(object obj, string fieldName)
        {
            FieldInfo field = obj.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
            if (field != null)
            {
                object value = field.GetValue(obj);
                return value?.ToString() ?? "";
            }
            
            McpLogger.LogDebug($"Field '{fieldName}' not found in {obj.GetType().FullName}, returning empty string");
            return "";
        }

        /// <summary>
        /// フィールドから整数値を取得する
        /// </summary>
        private static int GetFieldInt(object obj, string fieldName)
        {
            FieldInfo field = obj.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
            if (field != null)
            {
                object value = field.GetValue(obj);
                if (value is int intValue)
                    return intValue;
                
                McpLogger.LogDebug($"Field '{fieldName}' is not of type int, actual type: {value?.GetType().FullName ?? "null"}, returning 0");
                return 0;
            }
            
            McpLogger.LogDebug($"Field '{fieldName}' not found in {obj.GetType().FullName}, returning 0");
            return 0;
        }
    }
} 