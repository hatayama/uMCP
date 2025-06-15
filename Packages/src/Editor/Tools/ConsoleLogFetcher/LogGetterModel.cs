using System;
using System.Collections.Generic;
using UnityEngine;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Consoleログの取得を管理するModelクラス
    /// </summary>
    public class LogGetterModel : IDisposable
    {
        private readonly UnityLogEntriesAccessor logAccessor;

        public LogGetterModel()
        {
            logAccessor = new UnityLogEntriesAccessor();
        }

        public LogEntryDto[] GetConsoleLogEntries()
        {
            List<LogEntryDto> logEntries = new List<LogEntryDto>();

            if (!logAccessor.IsInitialized)
            {
                return logEntries.ToArray();
            }

            try
            {
                int logCount = logAccessor.GetLogCount();

                for (int i = 0; i < logCount; i++)
                {
                    object logEntry = logAccessor.GetLogEntry(i);
                    LogEntryDto dto = LogEntryConverter.ConvertToDto(logEntry);
                    
                    if (dto != null)
                    {
                        logEntries.Add(dto);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"ログ取得中にエラーが発生したで: {ex.Message}");
            }

            return logEntries.ToArray();
        }

        public void Dispose()
        {
            logAccessor?.Dispose();
        }
    }
} 