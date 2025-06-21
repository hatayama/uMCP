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
            Type consoleWindowUtilityType = typeof(EditorWindow).Assembly.GetType("UnityEditor.ConsoleWindowUtility");
            if (consoleWindowUtilityType != null)
            {
                System.Reflection.EventInfo consoleLogsChangedEvent = consoleWindowUtilityType.GetEvent("consoleLogsChanged");
                if (consoleLogsChangedEvent != null)
                {
                    Delegate handler = Delegate.CreateDelegate(consoleLogsChangedEvent.EventHandlerType, this, nameof(OnConsoleLogsChanged));
                    consoleLogsChangedEvent.AddEventHandler(null, handler);
                }
            }
        }

        private void OnLogMessageReceived(string logString, string stackTrace, LogType type)
        {
            string logTypeString = ConvertLogTypeToString(type);
            LogEntryDto logEntry = new LogEntryDto(logString, logTypeString, stackTrace ?? "", "");

            lock (lockObject)
            {
                logEntries.Add(logEntry);
            }
        }

        private void OnConsoleLogsChanged()
        {
            // If the Console is cleared, clear the custom logs as well.
            ConsoleWindowUtility.GetConsoleLogCounts(out int err, out int warn, out int log);
            if (err == 0 && warn == 0 && log == 0)
            {
                ClearLogs();
            }
        }

        private string ConvertLogTypeToString(LogType logType)
        {
            return logType switch
            {
                LogType.Error => "Error",
                LogType.Assert => "Assert",
                LogType.Warning => "Warning",
                LogType.Log => "Log",
                LogType.Exception => "Exception",
                _ => "Unknown"
            };
        }

        public LogEntryDto[] GetAllLogEntries()
        {
            lock (lockObject)
            {
                return logEntries.ToArray();
            }
        }

        public LogEntryDto[] GetLogEntriesByType(string logType)
        {
            lock (lockObject)
            {
                List<LogEntryDto> filteredEntries = new List<LogEntryDto>();
                
                foreach (LogEntryDto entry in logEntries)
                {
                    if (string.Equals(entry.LogType, logType, StringComparison.OrdinalIgnoreCase))
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

        public LogEntryDto[] GetLogEntriesByTypeAndMessage(string logType, string searchText)
        {
            lock (lockObject)
            {
                List<LogEntryDto> filteredEntries = new List<LogEntryDto>();
                
                foreach (LogEntryDto entry in logEntries)
                {
                    bool typeMatch = string.IsNullOrEmpty(logType) || logType == "All" || 
                                    string.Equals(entry.LogType, logType, StringComparison.OrdinalIgnoreCase);
                    
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
            
            lock (lockObject)
            {
                logEntries.Clear();
            }
        }
    }
}