using System;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Presenter class for LogGetterEditorWindow.
    /// </summary>
    public class LogGetterPresenter : IDisposable
    {
        public event Action<LogDisplayDto> OnLogDataUpdated;

        public void GetLogs()
        {
            LogDisplayDto displayData = LogGetter.GetConsoleLog();
            OnLogDataUpdated?.Invoke(displayData);
        }

        public void GetLogs(string logType)
        {
            LogDisplayDto displayData;
            
            if (logType == "All")
            {
                displayData = LogGetter.GetConsoleLog();
            }
            else
            {
                displayData = LogGetter.GetConsoleLog(logType);
            }
            
            OnLogDataUpdated?.Invoke(displayData);
        }

        public void ClearLogs()
        {
            LogDisplayDto displayData = new LogDisplayDto(new LogEntryDto[0], 0);
            OnLogDataUpdated?.Invoke(displayData);
        }

        public void Dispose()
        {
            OnLogDataUpdated = null;
        }
    }
} 