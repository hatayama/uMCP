namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Options for advanced GameObject search
    /// </summary>
    public class GameObjectSearchOptions
    {
        public string NamePattern { get; set; } = "";
        public bool UseRegex { get; set; } = false;
        public string[] RequiredComponents { get; set; } = new string[0];
        public string Tag { get; set; } = "";
        public int? Layer { get; set; } = null;
        public bool IncludeInactive { get; set; } = false;
        public int MaxResults { get; set; } = 100;
    }
}