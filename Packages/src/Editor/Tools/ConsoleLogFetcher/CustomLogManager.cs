using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Custom log management system using Application.logMessageReceived.
    /// </summary>
    public class CustomLogManager : IDisposable
    {
        private static CustomLogManager instance;
        private readonly List<LogEntryDto> logEntries;
        private readonly object lockObject = new object();

        public CustomLogManager()
        {
            logEntries ??= new List<LogEntryDto>();
            Initialize();
        }

        private void Initialize()
        {
            Application.logMessageReceived += OnLogMessageReceived;
            
            // Detect Console log clearing.
            ConsoleUtility.consoleLogsChanged += OnConsoleLogsChanged;
        }

        private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            McpLogType mcpLogType = ConvertLogTypeToMcpLogType(type);
            LogEntryDto logEntry = new LogEntryDto(mcpLogType, condition, stackTrace ?? "");

            lock (lockObject)
            {
                logEntries.Add(logEntry);
            }
        }

        private void OnConsoleLogsChanged()
        {
            // If the Console is cleared, clear the custom logs as well.
            ConsoleUtility.GetConsoleLogCounts(out int err, out int warn, out int log);
            if (err == 0 && warn == 0 && log == 0)
            {
                ClearLogs();
            }
        }

        private McpLogType ConvertLogTypeToMcpLogType(LogType logType)
        {
            return logType switch
            {
                LogType.Error => McpLogType.Error,
                LogType.Warning => McpLogType.Warning,
                LogType.Log => McpLogType.Log,
                _ => McpLogType.None
            };
        }

        public LogEntryDto[] GetAllLogEntries()
        {
            lock (lockObject)
            {
                return logEntries.ToArray();
            }
        }

        public LogEntryDto[] GetLogEntriesByType(McpLogType logType)
        {
            lock (lockObject)
            {
                List<LogEntryDto> filteredEntries = new List<LogEntryDto>();
                
                foreach (LogEntryDto entry in logEntries)
                {
                    if (entry.LogType == logType)
                    {
                        filteredEntries.Add(entry);
                    }
                }
                
                return filteredEntries.ToArray();
            }
        }

        public LogEntryDto[] GetLogEntriesByMessage(string searchText)
        {
            lock (lockObject)
            {
                if (string.IsNullOrEmpty(searchText))
                {
                    return logEntries.ToArray();
                }

                List<LogEntryDto> filteredEntries = new List<LogEntryDto>();
                
                foreach (LogEntryDto entry in logEntries)
                {
                    if (entry.Message.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        filteredEntries.Add(entry);
                    }
                }
                
                return filteredEntries.ToArray();
            }
        }

        public LogEntryDto[] GetLogEntriesByTypeAndMessage(McpLogType logType, string searchText)
        {
            lock (lockObject)
            {
                List<LogEntryDto> filteredEntries = new List<LogEntryDto>();
                
                foreach (LogEntryDto entry in logEntries)
                {
                    bool typeMatch = logType == McpLogType.All || entry.LogType == logType;
                    
                    bool messageMatch = string.IsNullOrEmpty(searchText) || 
                                       entry.Message.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
                    
                    if (typeMatch && messageMatch)
                    {
                        filteredEntries.Add(entry);
                    }
                }
                
                return filteredEntries.ToArray();
            }
        }

        public int GetLogCount()
        {
            lock (lockObject)
            {
                return logEntries.Count;
            }
        }

        public void ClearLogs()
        {
            lock (lockObject)
            {
                logEntries.Clear();
            }
        }

        public void Dispose()
        {
            Application.logMessageReceived -= OnLogMessageReceived;
            ConsoleUtility.consoleLogsChanged -= OnConsoleLogsChanged;
            
            lock (lockObject)
            {
                logEntries.Clear();
            }
        }
    }
}