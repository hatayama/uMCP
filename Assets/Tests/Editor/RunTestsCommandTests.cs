using NUnit.Framework;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace io.github.hatayama.uMCP
{
    public class RunTestsCommandTests
    {
        private RunTestsCommand runTestsCommand;

        [SetUp]
        public void Setup()
        {
            runTestsCommand = new RunTestsCommand();
        }

        /// <summary>
        /// Test for command type.
        /// - Asserts that the command type is RunTests.
        /// </summary>
        [Test]
        public void CommandType_ShouldReturnRunTests()
        {
            // Assert
            Assert.That(runTestsCommand.CommandType, Is.EqualTo(CommandType.RunTests));
        }

        /// <summary>
        /// Test execution with default parameters.
        /// *Skipped because actual test execution takes time.
        /// </summary>
        [Ignore("Skipping because actual test execution is time-consuming.")]
        [Test]
        public async Task ExecuteAsync_WithDefaultParams_ShouldRunAllTests()
        {
            // Arrange
            JToken paramsToken = new JObject();

            // Act
            object result = await runTestsCommand.ExecuteAsync(paramsToken);

            // Assert
            Assert.That(result, Is.Not.Null);
            var resultObj = result as dynamic;
            Assert.That(resultObj.success, Is.Not.Null);
            Assert.That(resultObj.message, Is.Not.Null);
            Assert.That(resultObj.completedAt, Is.Not.Null);
        }

        /// <summary>
        /// Parameter parsing test for filtered test execution.
        /// </summary>
        [Test]
        public void ParseParameters_ShouldParseCorrectly()
        {
            // Arrange
            JObject paramsToken = new JObject
            {
                ["filterType"] = "fullclassname",
                ["filterValue"] = "TestClass",
                ["saveXml"] = true
            };

            // Act - Invoke private method using reflection.
            System.Reflection.MethodInfo parseMethod = typeof(RunTestsCommand)
                .GetMethod("ParseParameters", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            TestExecutionParameters result = (TestExecutionParameters)parseMethod.Invoke(runTestsCommand, new object[] { paramsToken });

            // Assert
            Assert.That(result.FilterType, Is.EqualTo("fullclassname"));
            Assert.That(result.FilterValue, Is.EqualTo("TestClass"));
            Assert.That(result.SaveXml, Is.True);
        }

        /// <summary>
        /// Default value test with null parameters.
        /// </summary>
        [Test]
        public void ParseParameters_WithNullParams_ShouldReturnDefaults()
        {
            // Act - Invoke private method using reflection.
            System.Reflection.MethodInfo parseMethod = typeof(RunTestsCommand)
                .GetMethod("ParseParameters", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            TestExecutionParameters result = (TestExecutionParameters)parseMethod.Invoke(runTestsCommand, new object[] { null });

            // Assert
            Assert.That(result.FilterType, Is.EqualTo("all"));
            Assert.That(result.FilterValue, Is.EqualTo(""));
            Assert.That(result.SaveXml, Is.False);
        }

        /// <summary>
        /// Test for filter creation.
        /// </summary>
        [Test]
        public void CreateFilter_ShouldCreateCorrectFilter()
        {
            // Act - Invoke private method using reflection.
            System.Reflection.MethodInfo createFilterMethod = typeof(RunTestsCommand)
                .GetMethod("CreateFilter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            TestExecutionFilter result = (TestExecutionFilter)createFilterMethod.Invoke(runTestsCommand, new object[] { "fullclassname", "TestClass" });

            // Assert
            Assert.That(result.FilterType, Is.EqualTo(TestExecutionFilterType.ClassName));
            Assert.That(result.FilterValue, Is.EqualTo("TestClass"));
        }

        /// <summary>
        /// Test for unsupported filter types.
        /// </summary>
        [Test]
        public void CreateFilter_WithUnsupportedType_ShouldThrowException()
        {
            // Act & Assert - Invoke private method using reflection.
            System.Reflection.MethodInfo createFilterMethod = typeof(RunTestsCommand)
                .GetMethod("CreateFilter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            Assert.Throws<System.Reflection.TargetInvocationException>(() =>
            {
                createFilterMethod.Invoke(runTestsCommand, new object[] { "unsupported", "value" });
            });
        }
    }
} 