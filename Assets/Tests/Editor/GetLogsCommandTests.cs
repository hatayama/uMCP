using NUnit.Framework;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace io.github.hatayama.uMCP
{
    public class GetLogsCommandTests
    {
        private GetLogsCommand getLogsCommand;

        [SetUp]
        public void Setup()
        {
            getLogsCommand = new GetLogsCommand();
        }

        /// <summary>
        /// コマンドタイプのテスト
        /// - コマンドタイプがGetLogsであることを確認
        /// </summary>
        [Test]
        public void CommandType_ShouldReturnGetLogs()
        {
            // Assert
            Assert.That(getLogsCommand.CommandType, Is.EqualTo(CommandType.GetLogs));
        }

        /// <summary>
        /// デフォルトパラメータでのログ取得テスト
        /// - すべてのログタイプを取得できること
        /// - デフォルトの最大取得数（100）が設定されていること
        /// - 結果オブジェクトに必要なプロパティが含まれていること
        /// - ログの総数が取得できること
        /// </summary>
        [Test]
        public async Task ExecuteAsync_WithDefaultParams_ShouldReturnAllLogs()
        {
            // Arrange
            JToken paramsToken = new JObject();

            // Act
            object result = await getLogsCommand.ExecuteAsync(paramsToken);

            // Assert
            Assert.That(result, Is.Not.Null);
            
            // resultをJTokenとして変換して検証
            JToken resultToken = JToken.FromObject(result);
            Assert.That(resultToken["logs"], Is.Not.Null, "logs property should exist");
            Assert.That(resultToken["totalCount"], Is.Not.Null, "totalCount property should exist");
            Assert.That(resultToken["requestedLogType"]?.ToString(), Is.EqualTo("All"), "requestedLogType should be 'All'");
            Assert.That(resultToken["requestedMaxCount"]?.ToObject<int>(), Is.EqualTo(100), "requestedMaxCount should be 100");
            
            // logs配列の型チェック
            JArray logsArray = resultToken["logs"] as JArray;
            Assert.That(logsArray, Is.Not.Null, "logs should be an array");
        }

        /// <summary>
        /// カスタムパラメータでのログ取得テスト
        /// - 特定のログタイプ（Error）のみを取得できること
        /// - カスタムの最大取得数（50）が設定されていること
        /// - 結果オブジェクトに必要なプロパティが含まれていること
        /// - ログの総数が取得できること
        /// </summary>
        [Test]
        public async Task ExecuteAsync_WithCustomParams_ShouldReturnFilteredLogs()
        {
            // Arrange
            JToken paramsToken = new JObject
            {
                ["logType"] = "Error",
                ["maxCount"] = 50
            };

            // Act
            object result = await getLogsCommand.ExecuteAsync(paramsToken);

            // Assert
            Assert.That(result, Is.Not.Null);
            
            // resultをJTokenとして変換して検証
            JToken resultToken = JToken.FromObject(result);
            Assert.That(resultToken["logs"], Is.Not.Null, "logs property should exist");
            Assert.That(resultToken["totalCount"], Is.Not.Null, "totalCount property should exist");
            Assert.That(resultToken["requestedLogType"]?.ToString(), Is.EqualTo("Error"), "requestedLogType should be 'Error'");
            Assert.That(resultToken["requestedMaxCount"]?.ToObject<int>(), Is.EqualTo(50), "requestedMaxCount should be 50");
            
            // logs配列の型チェック
            JArray logsArray = resultToken["logs"] as JArray;
            Assert.That(logsArray, Is.Not.Null, "logs should be an array");
        }
    }
} 