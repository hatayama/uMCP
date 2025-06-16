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
        /// コマンドタイプのテスト
        /// - コマンドタイプがRunTestsであることを確認
        /// </summary>
        [Test]
        public void CommandType_ShouldReturnRunTests()
        {
            // Assert
            Assert.That(runTestsCommand.CommandType, Is.EqualTo(CommandType.RunTests));
        }

        /// <summary>
        /// デフォルトパラメータでのテスト実行
        /// ※実際のテスト実行は時間がかかるためスキップ
        /// </summary>
        [Ignore("実際のテスト実行は時間がかかるためスキップ")]
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
        /// フィルター付きテスト実行のパラメータ解析テスト
        /// </summary>
        [Test]
        public void ParseParameters_ShouldParseCorrectly()
        {
            // Arrange
            JObject paramsToken = new JObject
            {
                ["filterType"] = "classname",
                ["filterValue"] = "TestClass",
                ["saveXml"] = true
            };

            // Act - リフレクションを使ってプライベートメソッドを呼び出し
            System.Reflection.MethodInfo parseMethod = typeof(RunTestsCommand)
                .GetMethod("ParseParameters", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            TestExecutionParameters result = (TestExecutionParameters)parseMethod.Invoke(runTestsCommand, new object[] { paramsToken });

            // Assert
            Assert.That(result.FilterType, Is.EqualTo("classname"));
            Assert.That(result.FilterValue, Is.EqualTo("TestClass"));
            Assert.That(result.SaveXml, Is.True);
        }

        /// <summary>
        /// nullパラメータでのデフォルト値テスト
        /// </summary>
        [Test]
        public void ParseParameters_WithNullParams_ShouldReturnDefaults()
        {
            // Act - リフレクションを使ってプライベートメソッドを呼び出し
            System.Reflection.MethodInfo parseMethod = typeof(RunTestsCommand)
                .GetMethod("ParseParameters", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            TestExecutionParameters result = (TestExecutionParameters)parseMethod.Invoke(runTestsCommand, new object[] { null });

            // Assert
            Assert.That(result.FilterType, Is.EqualTo("all"));
            Assert.That(result.FilterValue, Is.EqualTo(""));
            Assert.That(result.SaveXml, Is.False);
        }

        /// <summary>
        /// フィルター作成のテスト
        /// </summary>
        [Test]
        public void CreateFilter_ShouldCreateCorrectFilter()
        {
            // Act - リフレクションを使ってプライベートメソッドを呼び出し
            System.Reflection.MethodInfo createFilterMethod = typeof(RunTestsCommand)
                .GetMethod("CreateFilter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            TestExecutionFilter result = (TestExecutionFilter)createFilterMethod.Invoke(runTestsCommand, new object[] { "classname", "TestClass" });

            // Assert
            Assert.That(result.FilterType, Is.EqualTo(TestExecutionFilterType.ClassName));
            Assert.That(result.FilterValue, Is.EqualTo("TestClass"));
        }

        /// <summary>
        /// 未対応フィルタータイプのテスト
        /// </summary>
        [Test]
        public void CreateFilter_WithUnsupportedType_ShouldThrowException()
        {
            // Act & Assert - リフレクションを使ってプライベートメソッドを呼び出し
            System.Reflection.MethodInfo createFilterMethod = typeof(RunTestsCommand)
                .GetMethod("CreateFilter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            Assert.Throws<System.Reflection.TargetInvocationException>(() =>
            {
                createFilterMethod.Invoke(runTestsCommand, new object[] { "unsupported", "value" });
            });
        }
    }
} 