using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// PlayerLoopTimingの列挙型（Editor版ではダミー実装）
    /// </summary>
    public enum PlayerLoopTiming
    {
        Initialization = 0,
        EarlyUpdate = 1,
        FixedUpdate = 2,
        PreUpdate = 3,
        Update = 4,
        PreLateUpdate = 5,
        PostLateUpdate = 6
    }
    /// <summary>
    /// UniTaskのSwitchToMainThreadと同等の機能を提供するクラス
    /// メインスレッドへの切り替えを行う
    /// </summary>
    public static class MainThreadSwitcher
    {
        private static int mainThreadId;
        private static SynchronizationContext unitySynchronizationContext;
        
        /// <summary>
        /// メインスレッドのIDを取得する
        /// </summary>
        public static int MainThreadId => mainThreadId;
        
        /// <summary>
        /// 現在のスレッドがメインスレッドかどうかを判定する
        /// </summary>
        public static bool IsMainThread => Thread.CurrentThread.ManagedThreadId == mainThreadId;

        /// <summary>
        /// UnitySynchronizationContextを取得する
        /// </summary>
        public static SynchronizationContext UnitySynchronizationContext => unitySynchronizationContext;

        [InitializeOnLoadMethod]
        static void Initialize()
        {
            // メインスレッドのIDとSynchronizationContextを記録
            mainThreadId = Thread.CurrentThread.ManagedThreadId;
            unitySynchronizationContext = SynchronizationContext.Current;
        }

        /// <summary>
        /// メインスレッドに切り替える（SynchronizationContext版）
        /// </summary>
        public static SwitchToMainThreadAwaitable SwitchToMainThread()
        {
            return new SwitchToMainThreadAwaitable(CancellationToken.None);
        }
        
        /// <summary>
        /// メインスレッドに切り替える（CancellationToken付き）
        /// </summary>
        public static SwitchToMainThreadAwaitable SwitchToMainThread(CancellationToken cancellationToken)
        {
            return new SwitchToMainThreadAwaitable(cancellationToken);
        }
        
        /// <summary>
        /// メインスレッドに切り替える（PlayerLoopTiming指定）
        /// Editor版ではPlayerLoopTimingは無視される
        /// </summary>
        public static SwitchToMainThreadAwaitable SwitchToMainThread(PlayerLoopTiming timing)
        {
            return new SwitchToMainThreadAwaitable(CancellationToken.None);
        }
        
        /// <summary>
        /// メインスレッドに切り替える（PlayerLoopTimingとCancellationToken指定）
        /// Editor版ではPlayerLoopTimingは無視される
        /// </summary>
        public static SwitchToMainThreadAwaitable SwitchToMainThread(PlayerLoopTiming timing, CancellationToken cancellationToken)
        {
            return new SwitchToMainThreadAwaitable(cancellationToken);
        }

        /// <summary>
        /// メインスレッドに切り替える（EditorApplication.delayCall版）
        /// </summary>
        public static SwitchToMainThreadDelayCallAwaitable SwitchToMainThreadDelayCall()
        {
            return new SwitchToMainThreadDelayCallAwaitable();
        }
    }

    /// <summary>
    /// SynchronizationContextを使ったメインスレッド切り替え用のAwaitable
    /// </summary>
    public struct SwitchToMainThreadAwaitable
    {
        private readonly CancellationToken cancellationToken;
        
        public SwitchToMainThreadAwaitable(CancellationToken cancellationToken)
        {
            this.cancellationToken = cancellationToken;
        }
        
        public Awaiter GetAwaiter() => new Awaiter(cancellationToken);

        public struct Awaiter : INotifyCompletion
        {
            private readonly CancellationToken cancellationToken;
            
            public Awaiter(CancellationToken cancellationToken)
            {
                this.cancellationToken = cancellationToken;
            }
            
            public bool IsCompleted
            {
                get
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return MainThreadSwitcher.IsMainThread;
                }
            }

            public void GetResult()
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            public void OnCompleted(Action continuation)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                if (MainThreadSwitcher.IsMainThread)
                {
                    continuation();
                    return;
                }

                // 保存されたUnitySynchronizationContextを優先使用
                if (MainThreadSwitcher.UnitySynchronizationContext != null)
                {
                    MainThreadSwitcher.UnitySynchronizationContext.Post(_ => continuation(), null);
                }
                else
                {
                    // フォールバック: EditorApplication.delayCallを使用
                    EditorApplication.delayCall += () => continuation();
                }
            }
        }
    }

    /// <summary>
    /// EditorApplication.delayCallを使ったメインスレッド切り替え用のAwaitable
    /// </summary>
    public struct SwitchToMainThreadDelayCallAwaitable
    {
        public Awaiter GetAwaiter() => new Awaiter();

        public struct Awaiter : INotifyCompletion
        {
            public bool IsCompleted => MainThreadSwitcher.IsMainThread;

            public void GetResult() { }

            public void OnCompleted(Action continuation)
            {
                if (MainThreadSwitcher.IsMainThread)
                {
                    continuation();
                    return;
                }

                // EditorApplication.delayCallを使ってメインスレッドで実行
                EditorApplication.delayCall += () => continuation();
            }
        }
    }

    /// <summary>
    /// TaskSchedulerを使った高度なメインスレッド切り替え
    /// </summary>
    public static class AdvancedMainThreadSwitcher
    {
        private static TaskScheduler unityTaskScheduler;

        [InitializeOnLoadMethod]
        static void Initialize()
        {
            // UnityのメインスレッドでTaskSchedulerを作成
            unityTaskScheduler = TaskScheduler.FromCurrentSynchronizationContext();
        }

        /// <summary>
        /// TaskSchedulerを使ってメインスレッドに切り替える
        /// </summary>
        public static Task SwitchToMainThreadAsync()
        {
            if (MainThreadSwitcher.IsMainThread)
            {
                return Task.CompletedTask;
            }

            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            
            Task.Factory.StartNew(() =>
            {
                tcs.SetResult(true);
            }, CancellationToken.None, TaskCreationOptions.None, unityTaskScheduler);

            return tcs.Task;
        }
    }
} 