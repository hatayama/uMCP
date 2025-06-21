using NUnit.Framework;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace io.github.hatayama.uMCP
{
    public class CompileCommandTests
    {
        private CompileCommand compileCommand;

        [SetUp]
        public void Setup()
        {
            compileCommand = new CompileCommand();
        }

        /// <summary>
        /// Test for command type.
        /// - Asserts that the command type is Compile.
        /// </summary>
        [Test]
        public void CommandType_ShouldReturnCompile()
        {
            // Assert
            Assert.That(compileCommand.CommandType, Is.EqualTo(CommandType.Compile));
        }

        /// <summary>
        /// Test for compilation execution with default parameters.
        /// *Skipped because the Unity compilation API cannot be executed in the test runner.
        /// </summary>
        [Ignore("Cannot run Unity's compilation API in the test runner.")]
        [Test]
        public async Task ExecuteAsync_WithDefaultParams_ShouldCompileWithoutForceRecompile()
        {
            // Arrange
            JToken paramsToken = new JObject();

            // Act
            object result = await compileCommand.ExecuteAsync(paramsToken);

            // Assert
            Assert.That(result, Is.Not.Null);
            var resultObj = result as dynamic;
            Assert.That(resultObj.success, Is.Not.Null);
            Assert.That(resultObj.errorCount, Is.Not.Null);
            Assert.That(resultObj.warningCount, Is.Not.Null);
            Assert.That(resultObj.completedAt, Is.Not.Null);
            Assert.That(resultObj.errors, Is.Not.Null);
            Assert.That(resultObj.warnings, Is.Not.Null);
        }

        /// <summary>
        /// Test for forced re-compilation.
        /// *Skipped because the Unity compilation API cannot be executed in the test runner.
        /// </summary>
        [Ignore("Cannot run Unity's compilation API in the test runner.")]
        [Test]
        public async Task ExecuteAsync_WithForceRecompile_ShouldCompileWithForceRecompile()
        {
            // Arrange
            JToken paramsToken = new JObject
            {
                ["forceRecompile"] = true
            };

            // Act
            object result = await compileCommand.ExecuteAsync(paramsToken);

            // Assert
            Assert.That(result, Is.Not.Null);
            var resultObj = result as dynamic;
            Assert.That(resultObj.success, Is.Not.Null);
            Assert.That(resultObj.errorCount, Is.Not.Null);
            Assert.That(resultObj.warningCount, Is.Not.Null);
            Assert.That(resultObj.completedAt, Is.Not.Null);
            Assert.That(resultObj.errors, Is.Not.Null);
            Assert.That(resultObj.warnings, Is.Not.Null);
        }
    }
} 