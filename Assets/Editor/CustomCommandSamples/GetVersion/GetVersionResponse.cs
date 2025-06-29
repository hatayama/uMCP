using Newtonsoft.Json;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Response for GetVersion command
    /// Contains Unity version and project information
    /// Related classes:
    /// - BaseCommandResponse: Provides timing information
    /// - GetVersionCommand: Creates instances of this response
    /// </summary>
    public class GetVersionResponse : BaseCommandResponse
    {
        public string UnityVersion { get; }
        public string Platform { get; }
        public string DataPath { get; }
        public string PersistentDataPath { get; }
        public string TemporaryCachePath { get; }
        public bool IsEditor { get; }
        public string ProductName { get; }
        public string CompanyName { get; }
        public string Version { get; }

        [JsonConstructor]
        public GetVersionResponse(string unityVersion, string platform, string dataPath,
                                string persistentDataPath, string temporaryCachePath, bool isEditor,
                                string productName, string companyName, string version)
        {
            UnityVersion = unityVersion ?? string.Empty;
            Platform = platform ?? string.Empty;
            DataPath = dataPath ?? string.Empty;
            PersistentDataPath = persistentDataPath ?? string.Empty;
            TemporaryCachePath = temporaryCachePath ?? string.Empty;
            IsEditor = isEditor;
            ProductName = productName ?? string.Empty;
            CompanyName = companyName ?? string.Empty;
            Version = version ?? string.Empty;
        }
    }
}