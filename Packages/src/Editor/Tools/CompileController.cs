using UnityEditor;
using UnityEditor.Compilation;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading.Tasks;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Unityのコンパイル処理を非同期で実行し、結果を監視するクラス
    /// コンパイルの開始、進行状況の監視、結果の取得を行う
    /// </summary>
    public class CompileChecker : System.IDisposable
    {
        private bool isCompiling = false;
        private List<CompilerMessage> compileMessages = new List<CompilerMessage>();
        private TaskCompletionSource<CompileResult> currentCompileTask;

        /// <summary>
        /// コンパイル完了時に発生するイベント
        /// </summary>
        public event Action<CompileResult> OnCompileCompleted;
        
        /// <summary>
        /// コンパイル開始時に発生するイベント
        /// </summary>
        public event Action<string> OnCompileStarted;
        
        /// <summary>
        /// アセンブリのコンパイル完了時に発生するイベント
        /// </summary>
        public event Action<string, CompilerMessage[]> OnAssemblyCompiled;

        /// <summary>
        /// 現在コンパイル中かどうかを取得する
        /// </summary>
        public bool IsCompiling => isCompiling;
        
        /// <summary>
        /// 現在のコンパイルメッセージ一覧を取得する
        /// </summary>
        public IReadOnlyList<CompilerMessage> CompileMessages => compileMessages.AsReadOnly();

        /// <summary>
        /// 非同期でコンパイルを実行する
        /// </summary>
        /// <param name="forceRecompile">強制再コンパイルを行うかどうか</param>
        /// <returns>コンパイル結果</returns>
        /// <exception cref="InvalidOperationException">コンパイル中にタスクが見つからない場合</exception>
        public async Task<CompileResult> TryCompileAsync(bool forceRecompile = false)
        {
            if (isCompiling)
            {
                // 既にコンパイル中の場合は現在のタスクを待つ
                if (currentCompileTask != null)
                {
                    return await currentCompileTask.Task;
                }
                throw new InvalidOperationException("コンパイル中じゃが、タスクが見つからんで");
            }

            isCompiling = true;
            compileMessages.Clear();
            currentCompileTask = new TaskCompletionSource<CompileResult>();

            // アセットリフレッシュを実行
            AssetDatabase.Refresh();

            // イベント登録
            CompilationPipeline.compilationFinished += HandleCompileFinished;
            CompilationPipeline.assemblyCompilationFinished += HandleAssemblyFinished;

            string startMessage = forceRecompile ? "アセットリフレッシュ後、強制再コンパイル開始したで..." : "アセットリフレッシュ後、コンパイル開始したで...";
            OnCompileStarted?.Invoke(startMessage);

            if (forceRecompile)
            {
                CompilationPipeline.RequestScriptCompilation(RequestScriptCompilationOptions.CleanBuildCache);
            }
            else
            {
                CompilationPipeline.RequestScriptCompilation();
            }

            return await currentCompileTask.Task;
        }

        /// <summary>
        /// コンパイルメッセージをクリアする
        /// </summary>
        public void ClearMessages()
        {
            compileMessages.Clear();
        }

        /// <summary>
        /// コンパイル完了時のハンドラー
        /// </summary>
        /// <param name="context">コンパイルコンテキスト</param>
        private void HandleCompileFinished(object context)
        {
            // イベント解除
            CompilationPipeline.compilationFinished -= HandleCompileFinished;
            CompilationPipeline.assemblyCompilationFinished -= HandleAssemblyFinished;

            isCompiling = false;

            CompileResult result = CreateCompileResult();
            OnCompileCompleted?.Invoke(result);

            // TaskCompletionSourceに結果を設定
            TaskCompletionSource<CompileResult> task = currentCompileTask;
            currentCompileTask = null;
            task?.SetResult(result);
        }

        /// <summary>
        /// アセンブリコンパイル完了時のハンドラー
        /// </summary>
        /// <param name="asmPath">アセンブリパス</param>
        /// <param name="messages">コンパイルメッセージ</param>
        private void HandleAssemblyFinished(string asmPath, CompilerMessage[] messages)
        {
            string assemblyName = System.IO.Path.GetFileName(asmPath);

            foreach (CompilerMessage message in messages)
            {
                compileMessages.Add(message);
            }

            OnAssemblyCompiled?.Invoke(assemblyName, messages);
        }

        /// <summary>
        /// コンパイル結果を作成する
        /// </summary>
        /// <returns>コンパイル結果</returns>
        private CompileResult CreateCompileResult()
        {
            int errorCount = compileMessages.Count(m => m.type == CompilerMessageType.Error);
            int warningCount = compileMessages.Count(m => m.type == CompilerMessageType.Warning);

            CompilerMessage[] errors = compileMessages.Where(m => m.type == CompilerMessageType.Error).ToArray();
            CompilerMessage[] warnings = compileMessages.Where(m => m.type == CompilerMessageType.Warning).ToArray();

            return new CompileResult(
                success: errorCount == 0,
                errorCount: errorCount,
                warningCount: warningCount,
                completedAt: DateTime.Now,
                messages: compileMessages.ToArray(),
                errors: errors,
                warnings: warnings
            );
        }

        /// <summary>
        /// リソースをクリーンアップする
        /// </summary>
        public void Cleanup()
        {
            // 念のためイベント解除
            CompilationPipeline.compilationFinished -= HandleCompileFinished;
            CompilationPipeline.assemblyCompilationFinished -= HandleAssemblyFinished;

            // 未完了のタスクがあればキャンセル
            if (currentCompileTask != null && !currentCompileTask.Task.IsCompleted)
            {
                currentCompileTask.SetCanceled();
                currentCompileTask = null;
            }
        }

        /// <summary>
        /// リソースを解放する
        /// </summary>
        public void Dispose()
        {
            Cleanup();
            compileMessages?.Clear();
            compileMessages = null;

            // イベントを全てクリア
            OnCompileCompleted = null;
            OnCompileStarted = null;
            OnAssemblyCompiled = null;
        }
    }

    /// <summary>
    /// コンパイル結果を表すクラス
    /// エラー、警告の情報とコンパイル完了時刻を含む
    /// </summary>
    public class CompileResult
    {
        /// <summary>
        /// コンパイルが成功したかどうか
        /// </summary>
        public bool Success { get; }
        
        /// <summary>
        /// エラー数
        /// </summary>
        public int ErrorCount { get; }
        
        /// <summary>
        /// 警告数
        /// </summary>
        public int WarningCount { get; }
        
        /// <summary>
        /// コンパイル完了時刻
        /// </summary>
        public DateTime CompletedAt { get; }
        
        /// <summary>
        /// 全てのコンパイルメッセージ
        /// </summary>
        public CompilerMessage[] Messages { get; }
        
        /// <summary>
        /// エラーメッセージのみ
        /// </summary>
        public CompilerMessage[] Errors { get; }
        
        /// <summary>
        /// 警告メッセージのみ
        /// </summary>
        public CompilerMessage[] Warnings { get; }

        /// <summary>
        /// エラーメッセージのエイリアス（下位互換用）
        /// </summary>
        public CompilerMessage[] error => Errors;
        
        /// <summary>
        /// 警告メッセージのエイリアス（下位互換用）
        /// </summary>
        public CompilerMessage[] warning => Warnings;

        /// <summary>
        /// コンパイル結果を初期化する
        /// </summary>
        /// <param name="success">コンパイル成功フラグ</param>
        /// <param name="errorCount">エラー数</param>
        /// <param name="warningCount">警告数</param>
        /// <param name="completedAt">完了時刻</param>
        /// <param name="messages">全メッセージ</param>
        /// <param name="errors">エラーメッセージ</param>
        /// <param name="warnings">警告メッセージ</param>
        public CompileResult(bool success, int errorCount, int warningCount, DateTime completedAt, CompilerMessage[] messages, CompilerMessage[] errors, CompilerMessage[] warnings)
        {
            Success = success;
            ErrorCount = errorCount;
            WarningCount = warningCount;
            CompletedAt = completedAt;
            Messages = messages;
            Errors = errors;
            Warnings = warnings;
        }
    }
}