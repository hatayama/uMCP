using UnityEditor.Compilation;
using System.Text;

namespace io.github.hatayama.uMCP
{
    public class CompileLogDisplay : System.IDisposable
    {
        private StringBuilder logBuilder = new StringBuilder();

        public string LogText => logBuilder?.ToString() ?? "コンパイル結果がここに表示されるで";

        public void Clear()
        {
            if (logBuilder == null) return;
            logBuilder.Clear();
            logBuilder.AppendLine("コンパイル結果がここに表示されるで");
        }

        public void RestoreFromText(string text)
        {
            if (logBuilder == null) return;
            logBuilder.Clear();
            logBuilder.Append(text);
        }

        public void AppendStartMessage(string message)
        {
            if (logBuilder == null) return;
            logBuilder.AppendLine(message);
        }

        public void AppendAssemblyMessage(string assemblyName, CompilerMessage[] messages)
        {
            if (logBuilder == null) return;
            logBuilder.AppendLine($"アセンブリ [{assemblyName}] コンパイル完了");

            foreach (CompilerMessage message in messages)
            {
                if (message.type == CompilerMessageType.Error)
                {
                    logBuilder.AppendLine($"  [エラー] {message.message}");
                }
                else if (message.type == CompilerMessageType.Warning)
                {
                    logBuilder.AppendLine($"  [警告] {message.message}");
                }
            }
        }

        public void AppendCompletionMessage(CompileResult result)
        {
            if (logBuilder == null) return;

            string resultMessage = result.Success ?
                "コンパイル成功じゃ！問題なしや。" :
                "コンパイル失敗じゃ！エラーを確認してくれや。";

            logBuilder.AppendLine();

            // アセンブリが処理されなかった場合（変更なし）の表示
            if (result.Messages.Length == 0)
            {
                logBuilder.AppendLine("変更がないから、コンパイルはスキップされたで");
                logBuilder.AppendLine("でも問題なく終わったで！");
            }

            logBuilder.AppendLine("=== コンパイル完了 ===");
            logBuilder.AppendLine($"結果: {resultMessage}");
            logBuilder.AppendLine($"エラーあり: {!result.Success}");
            logBuilder.AppendLine($"完了時刻: {result.CompletedAt:HH:mm:ss}");

            if (result.Messages.Length > 0)
            {
                logBuilder.AppendLine($"エラー数: {result.ErrorCount}, 警告数: {result.WarningCount}");
            }
            else
            {
                logBuilder.AppendLine("処理されたアセンブリ: なし（変更なし）");
            }
        }

        public CompileLogDisplay()
        {
            Clear();
        }

        public void Dispose()
        {
            logBuilder?.Clear();
            logBuilder = null;
        }
    }
}