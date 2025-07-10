namespace io.github.hatayama.uMCP
{
    public class FindGameObjectsResponse : BaseCommandResponse
    {
        public FindGameObjectResult[] results { get; set; }
        public int totalFound { get; set; }
        public string errorMessage { get; set; }
    }
    
    public class FindGameObjectResult
    {
        public string name { get; set; }
        public string path { get; set; }
        public bool isActive { get; set; }
        public string tag { get; set; }
        public int layer { get; set; }
        public ComponentInfo[] components { get; set; }
    }
}