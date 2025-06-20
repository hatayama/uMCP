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

            string message = GetProperty(logEntry, "message");
            string file = GetProperty(logEntry, "file");
            string condition = GetProperty(logEntry, "condition");
            int mode = GetPropertyInt(logEntry, "mode");

            // メッセージが空の場合はconditionを使用
            string finalMessage = !string.IsNullOrEmpty(message) ? message : condition;
            finalMessage = finalMessage?.Trim() ?? "";

            // ログタイプの判定
            string logType = LogTypeDetector.DetermineLogType(mode);

            return new LogEntryDto(finalMessage, logType, "", file);
        }

        private static string GetProperty(object obj, string propertyName)
        {
            PropertyInfo property = obj.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (property != null)
            {
                object value = property.GetValue(obj);
                return value?.ToString() ?? "";
            }
            
            FieldInfo field = obj.GetType().GetField(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (field != null)
            {
                object value = field.GetValue(obj);
                return value?.ToString() ?? "";
            }
            
            throw new InvalidOperationException($"Property or field '{propertyName}' not found in {obj.GetType().FullName}");
        }

        private static int GetPropertyInt(object obj, string propertyName)
        {
            PropertyInfo property = obj.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (property != null)
            {
                object value = property.GetValue(obj);
                if (value is int intValue)
                    return intValue;
                throw new InvalidOperationException($"Property '{propertyName}' is not of type int, actual type: {value?.GetType().FullName ?? "null"}");
            }
            
            FieldInfo field = obj.GetType().GetField(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (field != null)
            {
                object value = field.GetValue(obj);
                if (value is int intValue)
                    return intValue;
                throw new InvalidOperationException($"Field '{propertyName}' is not of type int, actual type: {value?.GetType().FullName ?? "null"}");
            }
            
            throw new InvalidOperationException($"Property or field '{propertyName}' not found in {obj.GetType().FullName}");
        }
    }
} 