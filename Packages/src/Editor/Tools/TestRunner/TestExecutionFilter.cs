namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// テスト実行フィルターの種類
    /// </summary>
    public enum TestExecutionFilterType
    {
        /// <summary>
        /// 特定のテスト名でフィルタリング
        /// </summary>
        TestName,
        
        /// <summary>
        /// クラス名でフィルタリング
        /// </summary>
        ClassName,
        
        /// <summary>
        /// ネームスペースでフィルタリング
        /// </summary>
        Namespace,
        
        /// <summary>
        /// アセンブリ名でフィルタリング
        /// </summary>
        AssemblyName
    }
    
    /// <summary>
    /// テスト実行フィルター情報を保持するクラス
    /// </summary>
    public class TestExecutionFilter
    {
        /// <summary>
        /// フィルタータイプ
        /// </summary>
        public TestExecutionFilterType FilterType { get; }
        
        /// <summary>
        /// フィルター値
        /// </summary>
        public string FilterValue { get; }
        
        /// <summary>
        /// テスト実行フィルターを作成する
        /// </summary>
        public TestExecutionFilter(TestExecutionFilterType filterType, string filterValue)
        {
            FilterType = filterType;
            FilterValue = filterValue;
        }
        
        /// <summary>
        /// クラス名でフィルターを作成
        /// </summary>
        public static TestExecutionFilter ByClassName(string className)
        {
            return new TestExecutionFilter(TestExecutionFilterType.ClassName, className);
        }
        
        /// <summary>
        /// ネームスペースでフィルターを作成
        /// </summary>
        public static TestExecutionFilter ByNamespace(string namespaceName)
        {
            return new TestExecutionFilter(TestExecutionFilterType.Namespace, namespaceName);
        }
        
        /// <summary>
        /// テスト名でフィルターを作成
        /// </summary>
        public static TestExecutionFilter ByTestName(string testName)
        {
            return new TestExecutionFilter(TestExecutionFilterType.TestName, testName);
        }
        
        /// <summary>
        /// アセンブリ名でフィルターを作成
        /// </summary>
        public static TestExecutionFilter ByAssemblyName(string assemblyName)
        {
            return new TestExecutionFilter(TestExecutionFilterType.AssemblyName, assemblyName);
        }
    }
} 