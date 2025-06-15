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
        /// コマンドタイプのテスト
        /// - コマンドタイプがCompileであることを確認
        /// </summary>
        [Test]
        public void CommandType_ShouldReturnCompile()
        {
            // Assert
            Assert.That(compileCommand.CommandType, Is.EqualTo(CommandType.Compile));
        }

        /// <summary>
        /// デフォルトパラメータでのコンパイル実行テスト
        /// ※UnityのコンパイルAPIはテストランナーで実行不可のためスキップ
        /// </summary>
        [Ignore("UnityのコンパイルAPIはテストランナーで実行不可のためスキップ")]
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
        /// 強制再コンパイルのテスト
        /// ※UnityのコンパイルAPIはテストランナーで実行不可のためスキップ
        /// </summary>
        [Ignore("UnityのコンパイルAPIはテストランナーで実行不可のためスキップ")]
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