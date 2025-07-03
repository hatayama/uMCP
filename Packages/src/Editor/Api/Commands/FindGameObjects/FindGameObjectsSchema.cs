namespace io.github.hatayama.uMCP
{
    public class FindGameObjectsSchema : BaseCommandSchema
    {
        // Search criteria
        public string NamePattern { get; set; } = "";
        public bool UseRegex { get; set; } = false;
        public string[] RequiredComponents { get; set; } = new string[0];
        public string Tag { get; set; } = "";
        public int? Layer { get; set; } = null;
        public bool IncludeInactive { get; set; } = false;
        
        // Result control
        public int MaxResults { get; set; } = 100;
        public bool IncludeInheritedProperties { get; set; } = false;
    }
}