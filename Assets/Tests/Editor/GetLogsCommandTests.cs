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
        /// Test for command type.
        /// - Asserts that the command type is GetLogs.
        /// </summary>
        [Test]
        public void CommandType_ShouldReturnGetLogs()
        {
            // Assert
            Assert.That(getLogsCommand.CommandType, Is.EqualTo(CommandType.GetLogs));
        }

        /// <summary>
        /// Test for getting logs with default parameters.
        /// - All log types can be retrieved.
        /// - The default maximum number of logs to retrieve (100) is set.
        /// - The result object contains the required properties.
        /// - The total number of logs can be retrieved.
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
            
            // Convert result to JToken for verification.
            JToken resultToken = JToken.FromObject(result);
            Assert.That(resultToken["logs"], Is.Not.Null, "logs property should exist");
            Assert.That(resultToken["totalCount"], Is.Not.Null, "totalCount property should exist");
            Assert.That(resultToken["requestedLogType"]?.ToString(), Is.EqualTo("All"), "requestedLogType should be 'All'");
            Assert.That(resultToken["requestedMaxCount"]?.ToObject<int>(), Is.EqualTo(100), "requestedMaxCount should be 100");
            
            // Type check for the logs array.
            JArray logsArray = resultToken["logs"] as JArray;
            Assert.That(logsArray, Is.Not.Null, "logs should be an array");
        }

        /// <summary>
        /// Test for getting logs with custom parameters.
        /// - Only a specific log type (Error) can be retrieved.
        /// - A custom maximum number of logs to retrieve (50) is set.
        /// - The result object contains the required properties.
        /// - The total number of logs can be retrieved.
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
            
            // Convert result to JToken for verification.
            JToken resultToken = JToken.FromObject(result);
            Assert.That(resultToken["logs"], Is.Not.Null, "logs property should exist");
            Assert.That(resultToken["totalCount"], Is.Not.Null, "totalCount property should exist");
            Assert.That(resultToken["requestedLogType"]?.ToString(), Is.EqualTo("Error"), "requestedLogType should be 'Error'");
            Assert.That(resultToken["requestedMaxCount"]?.ToObject<int>(), Is.EqualTo(50), "requestedMaxCount should be 50");
            
            // Type check for the logs array.
            JArray logsArray = resultToken["logs"] as JArray;
            Assert.That(logsArray, Is.Not.Null, "logs should be an array");
        }
    }
} 