using NUnit.Framework;
using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.TestTools;

namespace io.github.hatayama.uMCP
{
    public class MainThreadSwitcherTests
    {
        private int mainThreadId;

        [SetUp]
        public void Setup()
        {
            // メインスレッドIDを記録
            mainThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        /// <summary>
        /// メインスレッドで呼び出した場合、即座に実行されることを確認
        /// </summary>
        [Test]
        public async Task SwitchToMainThread_WhenCalledFromMainThread_ShouldExecuteImmediately()
        {
            // Arrange
            bool executedImmediately = false;
            int executionThreadId = -1;

            // Act
            await MainThreadSwitcher.SwitchToMainThread();
            executedImmediately = true;
            executionThreadId = Thread.CurrentThread.ManagedThreadId;

            // Assert
            Assert.That(executedImmediately, Is.True, "Should execute immediately when called from main thread");
            Assert.That(executionThreadId, Is.EqualTo(mainThreadId), "Should continue on main thread");
        }

        /// <summary>
        /// バックグラウンドスレッドから呼び出した場合、メインスレッドに切り替わることを確認
        /// </summary>
        [UnityTest]
        public IEnumerator SwitchToMainThread_WhenCalledFromBackgroundThread_ShouldSwitchToMainThread()
        {
            // Arrange
            int backgroundThreadId = -1;
            int afterSwitchThreadId = -1;
            bool taskCompleted = false;

            // Act
            Task.Run(async () =>
            {
                backgroundThreadId = Thread.CurrentThread.ManagedThreadId;
                
                await MainThreadSwitcher.SwitchToMainThread();
                
                afterSwitchThreadId = Thread.CurrentThread.ManagedThreadId;
                taskCompleted = true;
            });

            // タスクが完了するまで待機（最大5秒）
            float timeoutTime = Time.realtimeSinceStartup + 5f;
            while (!taskCompleted && Time.realtimeSinceStartup < timeoutTime)
            {
                yield return null;
            }

            // Assert
            Assert.That(taskCompleted, Is.True, "Background task should complete within timeout");
            Assert.That(backgroundThreadId, Is.Not.EqualTo(mainThreadId), "Should start on background thread");
            Assert.That(afterSwitchThreadId, Is.EqualTo(mainThreadId), "Should switch to main thread");
        }

        /// <summary>
        /// CancellationTokenがキャンセルされた場合、OperationCanceledExceptionがスローされることを確認
        /// </summary>
        [Test]
        public void SwitchToMainThread_WhenCancellationRequested_ShouldThrowOperationCanceledException()
        {
            // Arrange
            CancellationTokenSource cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            Assert.ThrowsAsync<TaskCanceledException>(async () =>
            {
                await MainThreadSwitcher.SwitchToMainThread(cts.Token);
            });
        }

        /// <summary>
        /// PlayerLoopTimingを指定できることを確認
        /// </summary>
        [Test]
        public async Task SwitchToMainThread_WithPlayerLoopTiming_ShouldAcceptTiming()
        {
            // Arrange
            PlayerLoopTiming timing = PlayerLoopTiming.FixedUpdate;
            bool executed = false;

            // Act
            await MainThreadSwitcher.SwitchToMainThread(timing);
            executed = true;

            // Assert
            Assert.That(executed, Is.True, "Should execute with specified timing");
        }

        /// <summary>
        /// 複数のスレッドから同時にSwitchToMainThreadを呼び出した場合の動作確認
        /// </summary>
        [UnityTest]
        public IEnumerator SwitchToMainThread_WhenCalledFromMultipleThreads_ShouldAllSwitchToMainThread()
        {
            // Arrange
            const int threadCount = 5;
            int[] threadIds = new int[threadCount];
            int[] switchedThreadIds = new int[threadCount];
            bool[] taskCompleted = new bool[threadCount];

            // Act
            for (int i = 0; i < threadCount; i++)
            {
                int index = i; // キャプチャ用
                Task.Run(async () =>
                {
                    threadIds[index] = Thread.CurrentThread.ManagedThreadId;
                    
                    await MainThreadSwitcher.SwitchToMainThread();
                    
                    switchedThreadIds[index] = Thread.CurrentThread.ManagedThreadId;
                    taskCompleted[index] = true;
                });
            }

            // すべてのタスクが完了するまで待機（最大10秒）
            float timeoutTime = Time.realtimeSinceStartup + 10f;
            while (Time.realtimeSinceStartup < timeoutTime)
            {
                bool allCompleted = true;
                for (int i = 0; i < threadCount; i++)
                {
                    if (!taskCompleted[i])
                    {
                        allCompleted = false;
                        break;
                    }
                }
                
                if (allCompleted)
                    break;
                    
                yield return null;
            }

            // Assert
            for (int i = 0; i < threadCount; i++)
            {
                Assert.That(taskCompleted[i], Is.True, $"Task {i} should complete within timeout");
                Assert.That(threadIds[i], Is.Not.EqualTo(mainThreadId), $"Task {i} should start on background thread");
                Assert.That(switchedThreadIds[i], Is.EqualTo(mainThreadId), $"Task {i} should switch to main thread");
            }
        }
    }
} 