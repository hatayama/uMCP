using NUnit.Framework;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Collections;
using UnityEngine.TestTools;

namespace io.github.hatayama.uMCP
{
    public class GetLogsToolTests
    {
        private GetLogsTool getLogsTool;

        [SetUp]
        public void Setup()
        {
            getLogsTool = new GetLogsTool();
        }

        /// <summary>
        /// Test for tool name.
        /// - Asserts that the tool name is "get-logs".
        /// </summary>
        [Test]
        public void ToolName_ShouldReturnGetLogs()
        {
            // Assert
            Assert.That(getLogsTool.ToolName, Is.EqualTo("get-logs"));
        }

        /// <summary>
        /// Test for getting logs with default parameters.
        /// - All log types can be retrieved.
        /// - The default maximum number of logs to retrieve (100) is set.
        /// - The result object contains the required properties.
        /// - The total number of logs can be retrieved.
        /// </summary>
        [UnityTest]
        public IEnumerator ExecuteAsync_WithDefaultParams_ShouldReturnAllLogs()
        {
            // Arrange
            GetLogsSchema schema = new GetLogsSchema();
            GetLogsResponse result = null;
            bool completed = false;

            // Act
            Task.Run(async () => {
                try {
                    // Convert schema to JToken for public ExecuteAsync method
                    JToken paramsToken = JToken.FromObject(schema);
                    object response = await getLogsTool.ExecuteAsync(paramsToken);
                    result = response as GetLogsResponse;
                    completed = true;
                } catch (System.Exception ex) {
                    UnityEngine.Debug.LogError($"Test failed: {ex.Message}");
                    completed = true;
                }
            });

            // Wait for completion
            yield return new UnityEngine.WaitUntil(() => completed);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Logs, Is.Not.Null, "logs property should exist");
            Assert.That(result.TotalCount, Is.GreaterThanOrEqualTo(0), "totalCount should be non-negative");
            Assert.That(result.LogType, Is.EqualTo("All"), "logType should be 'All'");
            Assert.That(result.MaxCount, Is.EqualTo(100), "maxCount should be 100");
            Assert.That(result.Logs, Is.Not.Null, "logs should be an array");
        }

        /// <summary>
        /// Test for getting logs with custom parameters.
        /// - Only a specific log type (Error) can be retrieved.
        /// - A custom maximum number of logs to retrieve (50) is set.
        /// - The result object contains the required properties.
        /// - The total number of logs can be retrieved.
        /// </summary>
        [UnityTest]
        public IEnumerator ExecuteAsync_WithCustomParams_ShouldReturnFilteredLogs()
        {
            // Arrange
            GetLogsSchema schema = new GetLogsSchema
            {
                LogType = McpLogType.Error,
                MaxCount = 50
            };
            GetLogsResponse result = null;
            bool completed = false;

            // Act
            Task.Run(async () => {
                try {
                    // Convert schema to JToken for public ExecuteAsync method
                    JToken paramsToken = JToken.FromObject(schema);
                    object response = await getLogsTool.ExecuteAsync(paramsToken);
                    result = response as GetLogsResponse;
                    completed = true;
                } catch (System.Exception ex) {
                    UnityEngine.Debug.LogError($"Test failed: {ex.Message}");
                    completed = true;
                }
            });

            // Wait for completion
            yield return new UnityEngine.WaitUntil(() => completed);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Logs, Is.Not.Null, "logs property should exist");
            Assert.That(result.TotalCount, Is.GreaterThanOrEqualTo(0), "totalCount should be non-negative");
            Assert.That(result.LogType, Is.EqualTo("Error"), "logType should be 'Error'");
            Assert.That(result.MaxCount, Is.EqualTo(50), "maxCount should be 50");
            Assert.That(result.Logs, Is.Not.Null, "logs should be an array");
        }
    }
} 