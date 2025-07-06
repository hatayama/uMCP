namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Menu item filter type
    /// </summary>
    public enum MenuItemFilterType
    {
        /// <summary>
        /// Filter by containing text
        /// </summary>
        Contains = 0,
        
        /// <summary>
        /// Filter by exact match
        /// </summary>
        Exact = 1,
        
        /// <summary>
        /// Filter by starts with
        /// </summary>
        StartsWith = 2
    }

    /// <summary>
    /// Menu item information
    /// </summary>
    public class MenuItemInfo
    {
        public string Path { get; set; }
        public int Priority { get; set; }
        public bool IsChecked { get; set; }
        public bool IsValidated { get; set; }
        public string MethodName { get; set; }
        public string TypeName { get; set; }
        public string AssemblyName { get; set; }
        public bool IsSeparator { get; set; }
        public string Shortcut { get; set; }
        public bool CanExecute { get; set; }
        public string Category { get; set; }
        
        public MenuItemInfo()
        {
            CanExecute = true;
        }
    }

    /// <summary>
    /// Test filter type
    /// </summary>
    public enum TestFilterType
    {
        /// <summary>
        /// Run all tests
        /// </summary>
        All = 0,
        
        /// <summary>
        /// Filter by full class name
        /// </summary>
        FullClassName = 1,
        
        /// <summary>
        /// Filter by namespace
        /// </summary>
        Namespace = 2,
        
        /// <summary>
        /// Filter by test name
        /// </summary>
        TestName = 3,
        
        /// <summary>
        /// Filter by assembly
        /// </summary>
        Assembly = 4
    }
}