namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Response schema for GetVersion command
    /// Provides Unity version and project information
    /// </summary>
    public class GetVersionResponse : BaseCommandResponse
    {
        public string UnityVersion { get; set; }
        public string Platform { get; set; }
        public string DataPath { get; set; }
        public string PersistentDataPath { get; set; }
        public string TemporaryCachePath { get; set; }
        public bool IsEditor { get; set; }
        public string ProductName { get; set; }
        public string CompanyName { get; set; }
        public string Version { get; set; }

        public GetVersionResponse()
        {
        }
    }
}