using System;
using System.Reflection;
using UnityEditor;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// UnityEditor.LogEntriesへのリフレクションアクセスを提供するクラス
    /// </summary>
    public class UnityLogEntriesAccessor : IDisposable
    {
        private Type logEntriesType;
        private Type logEntryType;
        private MethodInfo getCountMethod;
        private MethodInfo getEntryInternalMethod;

        public bool IsInitialized => logEntriesType != null && getCountMethod != null && getEntryInternalMethod != null;

        public UnityLogEntriesAccessor()
        {
            InitializeReflection();
        }

        private void InitializeReflection()
        {
            logEntriesType = typeof(EditorWindow).Assembly.GetType("UnityEditor.LogEntries");
            logEntryType = typeof(EditorWindow).Assembly.GetType("UnityEditor.LogEntry");
            
            if (logEntriesType != null)
            {
                getCountMethod = logEntriesType.GetMethod("GetCount", BindingFlags.Static | BindingFlags.Public);
                getEntryInternalMethod = logEntriesType.GetMethod("GetEntryInternal", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            }
        }

        public int GetLogCount()
        {
            if (!IsInitialized)
                return 0;

            try
            {
                return (int)getCountMethod.Invoke(null, null);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public object GetLogEntry(int index)
        {
            if (!IsInitialized || logEntryType == null)
                return null;

            try
            {
                object logEntry = Activator.CreateInstance(logEntryType);
                object[] args = new object[] { index, logEntry };
                
                object result = getEntryInternalMethod.Invoke(null, args);
                
                if (result != null && (bool)result)
                {
                    return args[1];
                }
            }
            catch (Exception)
            {
                // エラーは静かに処理
            }

            return null;
        }

        public void Dispose()
        {
            logEntriesType = null;
            logEntryType = null;
            getCountMethod = null;
            getEntryInternalMethod = null;
        }
    }
} 