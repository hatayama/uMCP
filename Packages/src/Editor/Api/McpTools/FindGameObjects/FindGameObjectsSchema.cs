using System.ComponentModel;

namespace io.github.hatayama.uMCP
{
    public class FindGameObjectsSchema : BaseToolSchema
    {
        // Search criteria
        public string NamePattern { get; set; } = "";
        [Description("Search mode (Exact(0), Path(1), Regex(2), Contains(3))")]
        public SearchMode SearchMode { get; set; } = SearchMode.Exact;
        public string[] RequiredComponents { get; set; } = new string[0];
        public string Tag { get; set; } = "";
        public int? Layer { get; set; } = null;
        public bool IncludeInactive { get; set; } = false;
        
        // Result control
        public int MaxResults { get; set; } = 20;  // Reduced from 100 to prevent performance issues
        public bool IncludeInheritedProperties { get; set; } = false;
    }
    
    public enum SearchMode
    {
        Exact,      // Exact match (default)
        Path,       // Hierarchy path search (e.g. "Canvas/Button")
        Regex,      // Regular expression
        Contains    // Partial match
    }
}