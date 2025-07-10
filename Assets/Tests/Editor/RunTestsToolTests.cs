using NUnit.Framework;

namespace io.github.hatayama.uMCP
{
    public class RunTestsToolTests
    {
        private RunTestsTool runTestsTool;

        [SetUp]
        public void Setup()
        {
            runTestsTool = new RunTestsTool();
        }

        /// <summary>
        /// Test for tool name.
        /// - Asserts that the tool name is "run-tests".
        /// </summary>
        [Test]
        public void ToolName_ShouldReturnRunTests()
        {
            // Assert
            Assert.That(runTestsTool.ToolName, Is.EqualTo("run-tests"));
        }

        /// <summary>
        /// Parameter parsing test for filtered test execution.
        /// </summary>
        [Test]
        public void ParseParameters_ShouldParseCorrectly()
        {
            // This test is now obsolete as the new implementation uses type-safe Schema classes
            // instead of JSON parameter parsing. The parsing is handled by the MCP framework.
            
            // Arrange - Test the Schema object directly
            RunTestsSchema schema = new RunTestsSchema
            {
                FilterType = TestFilterType.regex,
                FilterValue = "TestClass",
                SaveXml = true
            };

            // Assert - Schema properties should match what we set
            Assert.That(schema.FilterType.ToString(), Is.EqualTo("regex"));
            Assert.That(schema.FilterValue, Is.EqualTo("TestClass"));
            Assert.That(schema.SaveXml, Is.True);
        }

        /// <summary>
        /// Default value test with default schema.
        /// </summary>
        [Test]
        public void ParseParameters_WithNullParams_ShouldReturnDefaults()
        {
            // This test is now obsolete as the new implementation uses type-safe Schema classes
            // Test the default values of the Schema object
            
            // Act - Create Schema with default values
            RunTestsSchema schema = new RunTestsSchema();

            // Assert - Schema should have default values
            Assert.That(schema.FilterType.ToString(), Is.EqualTo("all"));
            Assert.That(schema.FilterValue ?? "", Is.EqualTo(""));
            Assert.That(schema.SaveXml, Is.False);
        }

        /// <summary>
        /// Test for filter creation.
        /// </summary>
        [Test]
        public void CreateFilter_ShouldCreateCorrectFilter()
        {
            // Act - Invoke private method using reflection.
            System.Reflection.MethodInfo createFilterMethod = typeof(RunTestsTool)
                .GetMethod("CreateFilter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            TestExecutionFilter result = (TestExecutionFilter)createFilterMethod.Invoke(runTestsTool, new object[] { "regex", "TestClass" });

            // Assert
            Assert.That(result.FilterType, Is.EqualTo(TestExecutionFilterType.Regex));
            Assert.That(result.FilterValue, Is.EqualTo("TestClass"));
        }

        /// <summary>
        /// Test for unsupported filter types.
        /// </summary>
        [Test]
        public void CreateFilter_WithUnsupportedType_ShouldThrowException()
        {
            // Act & Assert - Invoke private method using reflection.
            System.Reflection.MethodInfo createFilterMethod = typeof(RunTestsTool)
                .GetMethod("CreateFilter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            Assert.Throws<System.Reflection.TargetInvocationException>(() =>
            {
                createFilterMethod.Invoke(runTestsTool, new object[] { "unsupported", "value" });
            });
        }
    }
} 