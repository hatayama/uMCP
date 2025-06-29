using NUnit.Framework;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Collections;
using UnityEngine.TestTools;

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
        /// Test for command name.
        /// - Asserts that the command name is "getlogs".
        /// </summary>
        [Test]
        public void CommandName_ShouldReturnGetLogs()
        {
            // Assert
            Assert.That(getLogsCommand.CommandName, Is.EqualTo("getlogs"));
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
            System.Exception testException = null;

            // Act - Execute on main thread instead of Task.Run
            Task<BaseCommandResponse> task = null;
            try {
                // Convert schema to JToken for public ExecuteAsync method
                JToken paramsToken = JToken.FromObject(schema);
                task = getLogsCommand.ExecuteAsync(paramsToken);
            } catch (System.Exception ex) {
                testException = ex;
            }

            // Wait for task completion on main thread
            if (task != null)
            {
                yield return new UnityEngine.WaitUntil(() => task.IsCompleted);
                
                if (task.IsFaulted)
                {
                    testException = task.Exception?.GetBaseException();
                }
                else if (task.IsCompletedSuccessfully)
                {
                    result = task.Result as GetLogsResponse;
                }
            }

            // Assert
            if (testException != null)
            {
                Assert.Fail($"Test failed with exception: {testException.Message}\nStackTrace: {testException.StackTrace}");
            }
            
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
            GetLogsSchema schema = new GetLogsSchema(
                logType: McpLogType.Error,
                maxCount: 50
            );
            GetLogsResponse result = null;
            System.Exception testException = null;

            // Act - Execute on main thread instead of Task.Run
            Task<BaseCommandResponse> task = null;
            try {
                // Convert schema to JToken for public ExecuteAsync method
                JToken paramsToken = JToken.FromObject(schema);
                task = getLogsCommand.ExecuteAsync(paramsToken);
            } catch (System.Exception ex) {
                testException = ex;
            }

            // Wait for task completion on main thread
            if (task != null)
            {
                yield return new UnityEngine.WaitUntil(() => task.IsCompleted);
                
                if (task.IsFaulted)
                {
                    testException = task.Exception?.GetBaseException();
                }
                else if (task.IsCompletedSuccessfully)
                {
                    result = task.Result as GetLogsResponse;
                }
            }

            // Assert
            if (testException != null)
            {
                Assert.Fail($"Test failed with exception: {testException.Message}\nStackTrace: {testException.StackTrace}");
            }
            
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Logs, Is.Not.Null, "logs property should exist");
            Assert.That(result.TotalCount, Is.GreaterThanOrEqualTo(0), "totalCount should be non-negative");
            Assert.That(result.LogType, Is.EqualTo("Error"), "logType should be 'Error'");
            Assert.That(result.MaxCount, Is.EqualTo(50), "maxCount should be 50");
            Assert.That(result.Logs, Is.Not.Null, "logs should be an array");
        }
    }
} 