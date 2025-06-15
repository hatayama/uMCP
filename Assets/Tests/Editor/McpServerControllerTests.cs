using NUnit.Framework;
using UnityEditor;
using System.Threading.Tasks;

namespace io.github.hatayama.uMCP
{
    public class McpServerControllerTests
    {
        private const string SESSION_KEY_SERVER_RUNNING = "UnityMCP.ServerRunning";
        private const string SESSION_KEY_SERVER_PORT = "UnityMCP.ServerPort";

        [SetUp]
        public void Setup()
        {
            // テスト前にサーバーを停止
            if (McpServerController.IsServerRunning)
            {
                McpServerController.StopServer();
                // サーバーが完全に停止するまで少し待機
                Task.Delay(100).Wait();
            }
            
            // SessionStateを完全にクリーンアップ
            SessionState.EraseBool(SESSION_KEY_SERVER_RUNNING);
            SessionState.EraseInt(SESSION_KEY_SERVER_PORT);
        }

        [TearDown]
        public void TearDown()
        {
            // テスト後にサーバーを停止
            if (McpServerController.IsServerRunning)
            {
                McpServerController.StopServer();
                // サーバーが完全に停止するまで少し待機
                Task.Delay(100).Wait();
            }
            
            // SessionStateを完全にクリーンアップ
            SessionState.EraseBool(SESSION_KEY_SERVER_RUNNING);
            SessionState.EraseInt(SESSION_KEY_SERVER_PORT);
        }

        /// <summary>
        /// デフォルトポートでサーバーを起動した場合のテスト
        /// - サーバーが正常に起動すること
        /// - デフォルトポート（7400）で起動すること
        /// - テスト環境でサーバーを新規起動したため、wasRestoredはTrueになる
        /// </summary>
        [Test]
        public void StartServer_ShouldStartServerOnDefaultPort()
        {
            // Arrange
            int expectedPort = McpServerConfig.DEFAULT_PORT;

            // Act
            McpServerController.StartServer();
            // サーバーが起動するまで少し待機
            Task.Delay(100).Wait();

            // Assert
            var (isRunning, port, wasRestored) = McpServerController.GetServerStatus();
            Assert.That(isRunning, Is.True, "Server should be running");
            Assert.That(port, Is.EqualTo(expectedPort), "Server should be running on default port");
            // StartServer実行後はSessionStateにフラグが設定されるため、wasRestoredはTrueになる
            Assert.That(wasRestored, Is.True, "Server state should be tracked in session");
        }

        /// <summary>
        /// カスタムポートでサーバーを起動した場合のテスト
        /// - サーバーが正常に起動すること
        /// - 指定したポートで起動すること
        /// - テスト環境でサーバーを新規起動したため、wasRestoredはTrueになる
        /// </summary>
        [Test]
        public void StartServer_ShouldStartServerOnCustomPort()
        {
            // Arrange
            int customPort = 7500;

            // Act
            McpServerController.StartServer(customPort);
            // サーバーが起動するまで少し待機
            Task.Delay(100).Wait();

            // Assert
            var (isRunning, port, wasRestored) = McpServerController.GetServerStatus();
            Assert.That(isRunning, Is.True, "Server should be running");
            Assert.That(port, Is.EqualTo(customPort), "Server should be running on custom port");
            // StartServer実行後はSessionStateにフラグが設定されるため、wasRestoredはTrueになる
            Assert.That(wasRestored, Is.True, "Server state should be tracked in session");
        }

        /// <summary>
        /// サーバーを停止した場合のテスト
        /// - サーバーが正常に停止すること
        /// - ステータスが停止状態を示すこと
        /// </summary>
        [Test]
        public void StopServer_ShouldStopRunningServer()
        {
            // Arrange
            McpServerController.StartServer();
            // サーバーが起動するまで少し待機
            Task.Delay(100).Wait();

            // Act
            McpServerController.StopServer();
            // サーバーが停止するまで少し待機
            Task.Delay(100).Wait();

            // Assert
            var (isRunning, _, _) = McpServerController.GetServerStatus();
            Assert.That(isRunning, Is.False, "Server should be stopped");
        }

        /// <summary>
        /// サーバーステータス取得のテスト
        /// - 実行状態が正しく取得できること
        /// - ポート番号が正しく取得できること
        /// - セッション状態が正しく取得できること
        /// </summary>
        [Test]
        public void GetServerStatus_ShouldReturnCorrectStatus()
        {
            // Arrange
            int customPort = 7600;
            McpServerController.StartServer(customPort);
            // サーバーが起動するまで少し待機
            Task.Delay(100).Wait();

            // Act
            var (isRunning, port, wasRestored) = McpServerController.GetServerStatus();

            // Assert
            Assert.That(isRunning, Is.True, "Server should be running");
            Assert.That(port, Is.EqualTo(customPort), "Port should match custom port");
            // StartServer実行後はSessionStateにフラグが設定されるため、wasRestoredはTrueになる
            Assert.That(wasRestored, Is.True, "Server state should be tracked in session");
        }
    }
} 