using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using System.Linq;
using System.Text;

namespace io.github.hatayama.uMCP
{
    public class UnityApiInvestigator
    {
        [MenuItem("Tools/ログゲッター/Unity API 詳細調査")]
        public static void InvestigateUnityApis()
        {
            StringBuilder report = new StringBuilder();
            report.AppendLine("=== Unity Editor API 詳細調査レポート ===");
            report.AppendLine($"Unity Version: {Application.unityVersion}");
            report.AppendLine($"調査時刻: {DateTime.Now}");
            report.AppendLine();

            // 1. UnityEditor.LogEntries の調査
            InvestigateLogEntries(report);
            
            // 2. Console関連の型の調査
            InvestigateConsoleTypes(report);
            
            // 3. ConsoleFlags の調査
            InvestigateConsoleFlags(report);

            // レポートをConsoleに出力
            Debug.Log(report.ToString());
            
            // ファイルにも保存
            try
            {
                string filePath = "Assets/unity_api_investigation_report.txt";
                System.IO.File.WriteAllText(filePath, report.ToString());
                AssetDatabase.Refresh();
                Debug.Log($"調査レポートをファイルに保存しました: {filePath}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"ファイル保存でエラー: {ex.Message}");
            }
        }

        private static void InvestigateLogEntries(StringBuilder report)
        {
            report.AppendLine("### 1. UnityEditor.LogEntries クラスの調査 ###");
            
            try
            {
                Type logEntriesType = typeof(EditorWindow).Assembly.GetType("UnityEditor.LogEntries");
                
                if (logEntriesType == null)
                {
                    report.AppendLine("UnityEditor.LogEntries が見つかりません");
                    return;
                }
                
                report.AppendLine("UnityEditor.LogEntries が見つかりました");
                report.AppendLine($"型: {logEntriesType.FullName}");
                report.AppendLine();
                
                // 全メソッドを調査
                report.AppendLine("--- 利用可能なメソッド ---");
                MethodInfo[] methods = logEntriesType.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                
                foreach (MethodInfo method in methods.OrderBy(m => m.Name))
                {
                    ParameterInfo[] parameters = method.GetParameters();
                    string paramStr = string.Join(", ", parameters.Select(p => $"{p.ParameterType.Name} {p.Name}"));
                    report.AppendLine($"  {method.ReturnType.Name} {method.Name}({paramStr})");
                }
                
                report.AppendLine();
                
                // 特定メソッドの存在確認
                report.AppendLine("--- 特定メソッドの存在確認 ---");
                CheckMethod(logEntriesType, "GetCount", report);
                CheckMethod(logEntriesType, "GetEntryInternal", report);
                CheckMethod(logEntriesType, "GetConsoleFlag", report);
                CheckMethod(logEntriesType, "SetConsoleFlag", report);
                CheckMethod(logEntriesType, "GetConsoleFlags", report);
                CheckMethod(logEntriesType, "SetConsoleFlags", report);
                CheckMethod(logEntriesType, "StartGettingEntries", report);
                CheckMethod(logEntriesType, "EndGettingEntries", report);
                
            }
            catch (Exception ex)
            {
                report.AppendLine($"LogEntries調査中にエラー: {ex.Message}");
            }
            
            report.AppendLine();
        }

        private static void CheckMethod(Type type, string methodName, StringBuilder report)
        {
            MethodInfo method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (method != null)
            {
                ParameterInfo[] parameters = method.GetParameters();
                string paramStr = string.Join(", ", parameters.Select(p => $"{p.ParameterType.Name} {p.Name}"));
                report.AppendLine($"  {methodName}({paramStr}) : {method.ReturnType.Name}");
            }
            else
            {
                report.AppendLine($"  {methodName} が見つかりません");
            }
        }

        private static void InvestigateConsoleTypes(StringBuilder report)
        {
            report.AppendLine("### 2. Console関連の型の調査 ###");
            
            try
            {
                Assembly editorAssembly = typeof(EditorWindow).Assembly;
                Type[] allTypes = editorAssembly.GetTypes();
                
                var consoleTypes = allTypes.Where(t => 
                    t.Name.ToLower().Contains("console") || 
                    t.FullName.ToLower().Contains("console")
                ).ToArray();
                
                report.AppendLine($"Console関連の型: {consoleTypes.Length}個見つかりました");
                
                foreach (Type type in consoleTypes.OrderBy(t => t.FullName))
                {
                    report.AppendLine($"  {type.FullName}");
                    
                    // ネストした型もチェック
                    Type[] nestedTypes = type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic);
                    foreach (Type nested in nestedTypes)
                    {
                        report.AppendLine($"    └ {nested.Name}");
                    }
                }
                
            }
            catch (Exception ex)
            {
                report.AppendLine($"Console型調査中にエラー: {ex.Message}");
            }
            
            report.AppendLine();
        }

        private static void InvestigateConsoleFlags(StringBuilder report)
        {
            report.AppendLine("### 3. ConsoleFlags の調査 ###");
            
            try
            {
                Assembly editorAssembly = typeof(EditorWindow).Assembly;
                
                // 様々な可能性を調査
                string[] possibleNames = {
                    "UnityEditor.ConsoleFlags",
                    "UnityEditor.ConsoleWindow+ConsoleFlags", 
                    "UnityEditor.Console+ConsoleFlags",
                    "UnityEditor.ConsoleWindow+Flags",
                    "UnityEditor.LogEntries+ConsoleFlags",
                    "UnityEditor.LogEntries+Flags"
                };
                
                foreach (string typeName in possibleNames)
                {
                    Type flagsType = editorAssembly.GetType(typeName);
                    if (flagsType != null)
                    {
                        report.AppendLine($"{typeName} が見つかりました");
                        
                        if (flagsType.IsEnum)
                        {
                            report.AppendLine("  Enum値:");
                            foreach (object enumValue in Enum.GetValues(flagsType))
                            {
                                report.AppendLine($"    {enumValue} = {(int)enumValue}");
                            }
                        }
                        else
                        {
                            report.AppendLine("  Enumではありません");
                        }
                        report.AppendLine();
                    }
                    else
                    {
                        report.AppendLine($"{typeName} が見つかりません");
                    }
                }
                
                // すべてのEnum型を検索してConsole関連を探す
                report.AppendLine("--- すべてのEnum型からConsole関連を検索 ---");
                Type[] allTypes = editorAssembly.GetTypes();
                var consoleEnums = allTypes.Where(t => 
                    t.IsEnum && 
                    (t.Name.ToLower().Contains("console") || 
                     t.Name.ToLower().Contains("flag") ||
                     t.FullName.ToLower().Contains("console"))
                ).ToArray();
                
                foreach (Type enumType in consoleEnums)
                {
                    report.AppendLine($"  {enumType.FullName}");
                    foreach (object enumValue in Enum.GetValues(enumType))
                    {
                        report.AppendLine($"    {enumValue} = {(int)enumValue}");
                    }
                    report.AppendLine();
                }
                
            }
            catch (Exception ex)
            {
                report.AppendLine($"ConsoleFlags調査中にエラー: {ex.Message}");
            }
        }
    }
}