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
        // Unity内部型名の定数
        private const string LogEntriesTypeName = "UnityEditor.LogEntries";
        private const string LogEntryTypeName = "UnityEditor.LogEntry";
        private const string ConsoleFlagsTypeName = "UnityEditor.ConsoleWindow+ConsoleFlags";
        
        // Unity内部メソッド名の定数
        private const string GetCountMethodName = "GetCount";
        private const string GetEntryInternalMethodName = "GetEntryInternal";
        private const string GetConsoleFlagMethodName = "GetConsoleFlag";
        private const string GetConsoleFlagsAltMethodName = "GetConsoleFlags";
        private const string SetConsoleFlagMethodName = "SetConsoleFlag";
        private const string SetConsoleFlagsAltMethodName = "SetConsoleFlags";
        private const string GetConsoleFlagsMethodName = "get_consoleFlags";
        private const string StartGettingEntriesMethodName = "StartGettingEntries";
        private const string EndGettingEntriesMethodName = "EndGettingEntries";
        
        // ConsoleFlags ビットフラグの定数
        private const int ErrorFlagBit = 512;    // LogLevelError
        private const int WarningFlagBit = 256;  // LogLevelWarning
        private const int LogFlagBit = 128;      // LogLevelLog
        
        // ConsoleFlags Enum値名の定数
        private const string ErrorFlagName = "LogLevelError";
        private const string WarningFlagName = "LogLevelWarning";
        private const string LogFlagName = "LogLevelLog";
        private const string ErrorPauseFlagName = "ErrorPause";
        
        // フィルタータイプ名の定数
        private const string ErrorFilterType = "Error";
        private const string WarningFilterType = "Warning";
        private const string LogFilterType = "Log";
        
        private Type logEntriesType;
        private Type logEntryType;
        private Type consoleFlagsType;
        private MethodInfo getCountMethod;
        private MethodInfo getEntryInternalMethod;
        private MethodInfo getConsoleFlagMethod;
        private MethodInfo setConsoleFlagMethod;
        private MethodInfo getConsoleFlagsMethod;
        private MethodInfo startGettingEntriesMethod;
        private MethodInfo endGettingEntriesMethod;
        
        // ConsoleFlags列挙値
        private object showErrorFlag;
        private object showWarningFlag;
        private object showLogFlag;
        
        // 元のフィルター状態保存用
        private bool? originalErrorFlag;
        private bool? originalWarningFlag;
        private bool? originalLogFlag;

        public bool IsInitialized => logEntriesType != null && getCountMethod != null && getEntryInternalMethod != null;
        
        public bool IsFilterControlAvailable => IsInitialized && setConsoleFlagMethod != null && getConsoleFlagsMethod != null;

        public UnityLogEntriesAccessor()
        {
            InitializeReflection();
        }

        private void InitializeReflection()
        {
            InitializeTypes();
            InitializeMethods();
            InitializeConsoleFlags();
        }
        
        private void InitializeTypes()
        {
            logEntriesType = typeof(EditorWindow).Assembly.GetType(LogEntriesTypeName);
            logEntryType = typeof(EditorWindow).Assembly.GetType(LogEntryTypeName);
            consoleFlagsType = typeof(EditorWindow).Assembly.GetType(ConsoleFlagsTypeName);
        }
        
        private void InitializeMethods()
        {
            if (logEntriesType == null) return;
            
            getCountMethod = logEntriesType.GetMethod(GetCountMethodName, BindingFlags.Static | BindingFlags.Public);
            getEntryInternalMethod = logEntriesType.GetMethod(GetEntryInternalMethodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            
            InitializeFilterControlMethods();
            
            getConsoleFlagsMethod = logEntriesType.GetMethod(GetConsoleFlagsMethodName, BindingFlags.Static | BindingFlags.Public);
            startGettingEntriesMethod = logEntriesType.GetMethod(StartGettingEntriesMethodName, BindingFlags.Static | BindingFlags.Public);
            endGettingEntriesMethod = logEntriesType.GetMethod(EndGettingEntriesMethodName, BindingFlags.Static | BindingFlags.Public);
        }
        
        private void InitializeFilterControlMethods()
        {
            // フィルター制御メソッドを複数パターンで試行
            string[] possibleGetFlagMethods = { GetConsoleFlagMethodName, GetConsoleFlagsAltMethodName };
            string[] possibleSetFlagMethods = { SetConsoleFlagMethodName, SetConsoleFlagsAltMethodName };
            
            foreach (string methodName in possibleGetFlagMethods)
            {
                getConsoleFlagMethod = logEntriesType.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public);
                if (getConsoleFlagMethod != null) break;
            }
            
            foreach (string methodName in possibleSetFlagMethods)
            {
                setConsoleFlagMethod = logEntriesType.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public);
                if (setConsoleFlagMethod != null) break;
            }
        }
        
        private void InitializeConsoleFlags()
        {
            if (consoleFlagsType == null) return;
            
            // 実際のEnum値を使用（調査結果より）
            string[] possibleErrorFlags = { ErrorFlagName, ErrorPauseFlagName };
            string[] possibleWarningFlags = { WarningFlagName };
            string[] possibleLogFlags = { LogFlagName };
            
            showErrorFlag = FindEnumValue(possibleErrorFlags);
            showWarningFlag = FindEnumValue(possibleWarningFlags);
            showLogFlag = FindEnumValue(possibleLogFlags);
            
            // すべてのフラグが取得できなかった場合
            if (showErrorFlag == null || showWarningFlag == null || showLogFlag == null)
            {
                consoleFlagsType = null;
            }
        }
        
        private object FindEnumValue(string[] possibleFlagNames)
        {
            foreach (string flagName in possibleFlagNames)
            {
                if (Enum.IsDefined(consoleFlagsType, flagName))
                {
                    return Enum.Parse(consoleFlagsType, flagName);
                }
            }
            return null;
        }

        public int GetLogCount()
        {
            if (!IsInitialized)
                return 0;

            return (int)getCountMethod.Invoke(null, null);
        }

        public object GetLogEntry(int index)
        {
            if (!IsInitialized || logEntryType == null)
                return null;

            object logEntry = Activator.CreateInstance(logEntryType);
            object[] args = new object[] { index, logEntry };
            
            object result = getEntryInternalMethod.Invoke(null, args);
            
            if (result != null && (bool)result)
            {
                return args[1];
            }

            return null;
        }

        /// <summary>
        /// 現在のConsoleフィルター状態を保存する
        /// </summary>
        public void SaveConsoleFlags()
        {
            if (!IsFilterControlAvailable)
                return;

            originalErrorFlag = GetConsoleFlag(ErrorFilterType);
            originalWarningFlag = GetConsoleFlag(WarningFilterType);
            originalLogFlag = GetConsoleFlag(LogFilterType);
        }

        /// <summary>
        /// すべてのConsoleフィルターを有効にする
        /// </summary>
        public void EnableAllConsoleFlags()
        {
            if (!IsFilterControlAvailable)
                return;

            // SetConsoleFlag(Int32 bit, Boolean value) を使用
            setConsoleFlagMethod.Invoke(null, new object[] { ErrorFlagBit, true }); // LogLevelError
            setConsoleFlagMethod.Invoke(null, new object[] { WarningFlagBit, true }); // LogLevelWarning  
            setConsoleFlagMethod.Invoke(null, new object[] { LogFlagBit, true }); // LogLevelLog
        }

        /// <summary>
        /// 指定されたConsoleフィルターを設定する
        /// </summary>
        /// <param name="flagType">フィルタータイプ（"Error", "Warning", "Log"）</param>
        /// <param name="enabled">有効にするかどうか</param>
        public void SetConsoleFlag(string flagType, bool enabled)
        {
            McpLogger.LogInfo($"{flagType}フィルターを{(enabled ? "ON" : "OFF")}に設定");
            
            if (!IsFilterControlAvailable)
            {
                McpLogger.LogWarning("フィルター制御が利用できません");
                return;
            }

            int flagBit = 0;
            switch (flagType)
            {
                case ErrorFilterType:
                    flagBit = ErrorFlagBit; // LogLevelError
                    break;
                case WarningFilterType:
                    flagBit = WarningFlagBit; // LogLevelWarning
                    break;
                case LogFilterType:
                    flagBit = LogFlagBit; // LogLevelLog
                    break;
                default:
                    McpLogger.LogWarning($"不明なflagType: {flagType}");
                    return;
            }

            // SetConsoleFlag(Int32 bit, Boolean value) を呼び出し
            setConsoleFlagMethod.Invoke(null, new object[] { flagBit, enabled });
            McpLogger.LogDebug($"{flagType}フィルター設定完了 (bit:{flagBit}) - Unity Consoleで確認してください");
        }

        /// <summary>
        /// 指定されたConsoleフィルターの状態を取得する
        /// </summary>
        /// <param name="flagType">フィルタータイプ（"Error", "Warning", "Log"）</param>
        /// <returns>フィルターが有効かどうか</returns>
        public bool GetConsoleFlag(string flagType)
        {
            if (!IsFilterControlAvailable)
                return true; // フィルター制御が利用できない場合はtrueを返す

            int flagBit = 0;
            switch (flagType)
            {
                case ErrorFilterType:
                    flagBit = ErrorFlagBit; // LogLevelError
                    break;
                case WarningFilterType:
                    flagBit = WarningFlagBit; // LogLevelWarning
                    break;
                case LogFilterType:
                    flagBit = LogFlagBit; // LogLevelLog
                    break;
                default:
                    return true; // 不明なflagTypeの場合はtrueを返す
            }

            // get_consoleFlags() を使用してフィルター状態を取得
            object consoleFlagsObj = getConsoleFlagsMethod.Invoke(null, null);
            if (consoleFlagsObj != null)
            {
                int consoleFlags = (int)consoleFlagsObj;
                // ビットマスクでフィルター状態をチェック
                return (consoleFlags & flagBit) != 0;
            }
            return true;
        }

        /// <summary>
        /// 保存されたConsoleフィルター状態を復元する
        /// </summary>
        public void RestoreConsoleFlags()
        {
            if (!IsFilterControlAvailable)
                return;

            if (originalErrorFlag == null || originalWarningFlag == null || originalLogFlag == null)
                return;

            // 修正: int bitとbool valueの正しい引数形式で呼び出し
            setConsoleFlagMethod.Invoke(null, new object[] { ErrorFlagBit, originalErrorFlag.Value }); // LogLevelError
            setConsoleFlagMethod.Invoke(null, new object[] { WarningFlagBit, originalWarningFlag.Value }); // LogLevelWarning
            setConsoleFlagMethod.Invoke(null, new object[] { LogFlagBit, originalLogFlag.Value }); // LogLevelLog
            
            originalErrorFlag = null;
            originalWarningFlag = null;
            originalLogFlag = null;
        }

        /// <summary>
        /// StartGettingEntriesを呼び出す
        /// </summary>
        public void StartGettingEntries()
        {
            if (startGettingEntriesMethod != null)
            {
                startGettingEntriesMethod.Invoke(null, null);
            }
        }

        /// <summary>
        /// EndGettingEntriesを呼び出す
        /// </summary>
        public void EndGettingEntries()
        {
            if (endGettingEntriesMethod != null)
            {
                endGettingEntriesMethod.Invoke(null, null);
            }
        }

        /// <summary>
        /// フィルター状態を無視してログ数を取得する
        /// </summary>
        public int GetLogCountWithAllFlags()
        {
            if (!IsInitialized)
                return 0;

            // フィルター制御が利用できない場合は通常のGetLogCountを返す
            if (!IsFilterControlAvailable)
                return GetLogCount();

            SaveConsoleFlags();
            EnableAllConsoleFlags();
            
            int count = GetLogCount();
            RestoreConsoleFlags();
            
            return count;
        }

        /// <summary>
        /// フィルター状態を無視してログエントリを取得する
        /// </summary>
        public object GetLogEntryWithAllFlags(int index)
        {
            if (!IsInitialized)
                return null;

            // フィルター制御が利用できない場合は通常のGetLogEntryを返す
            if (!IsFilterControlAvailable)
                return GetLogEntry(index);

            SaveConsoleFlags();
            EnableAllConsoleFlags();
            
            object entry = GetLogEntry(index);
            RestoreConsoleFlags();
            
            return entry;
        }

        public void Dispose()
        {
            // フィルター状態が保存されている場合は復元
            if (originalErrorFlag != null || originalWarningFlag != null || originalLogFlag != null)
            {
                RestoreConsoleFlags();
            }
            
            logEntriesType = null;
            logEntryType = null;
            consoleFlagsType = null;
            getCountMethod = null;
            getEntryInternalMethod = null;
            getConsoleFlagMethod = null;
            setConsoleFlagMethod = null;
            getConsoleFlagsMethod = null;
            startGettingEntriesMethod = null;
            endGettingEntriesMethod = null;
            showErrorFlag = null;
            showWarningFlag = null;
            showLogFlag = null;
        }
    }
} 