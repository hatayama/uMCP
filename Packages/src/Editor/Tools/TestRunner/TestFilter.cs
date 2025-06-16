namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// テストフィルターの種類
    /// </summary>
    public enum TestFilterType
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
    /// テストフィルター情報を保持するクラス
    /// </summary>
    public class TestFilter
    {
        /// <summary>
        /// フィルタータイプ
        /// </summary>
        public TestFilterType FilterType { get; }
        
        /// <summary>
        /// フィルター値
        /// </summary>
        public string FilterValue { get; }
        
        /// <summary>
        /// テストフィルターを作成する
        /// </summary>
        public TestFilter(TestFilterType filterType, string filterValue)
        {
            FilterType = filterType;
            FilterValue = filterValue;
        }
        
        /// <summary>
        /// クラス名でフィルターを作成
        /// </summary>
        public static TestFilter ByClassName(string className)
        {
            return new TestFilter(TestFilterType.ClassName, className);
        }
        
        /// <summary>
        /// ネームスペースでフィルターを作成
        /// </summary>
        public static TestFilter ByNamespace(string namespaceName)
        {
            return new TestFilter(TestFilterType.Namespace, namespaceName);
        }
        
        /// <summary>
        /// テスト名でフィルターを作成
        /// </summary>
        public static TestFilter ByTestName(string testName)
        {
            return new TestFilter(TestFilterType.TestName, testName);
        }
        
        /// <summary>
        /// アセンブリ名でフィルターを作成
        /// </summary>
        public static TestFilter ByAssemblyName(string assemblyName)
        {
            return new TestFilter(TestFilterType.AssemblyName, assemblyName);
        }
    }
} 