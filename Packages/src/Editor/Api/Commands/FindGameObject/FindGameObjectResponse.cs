namespace io.github.hatayama.uMCP
{
    public class FindGameObjectResponse : BaseCommandResponse
    {
        public string name { get; set; }
        public string path { get; set; }
        public bool isActive { get; set; }
        public ComponentInfo[] components { get; set; }
        public bool found { get; set; }
        public string errorMessage { get; set; }
    }
    
    public class ComponentInfo
    {
        public string type { get; set; }
        public string fullTypeName { get; set; }
        public ComponentPropertyInfo[] properties { get; set; }
    }
    
    public class ComponentPropertyInfo
    {
        public string name { get; set; }
        public string type { get; set; }
        public object value { get; set; }
    }
}