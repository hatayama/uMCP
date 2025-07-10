using NUnit.Framework;

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


    }
} 