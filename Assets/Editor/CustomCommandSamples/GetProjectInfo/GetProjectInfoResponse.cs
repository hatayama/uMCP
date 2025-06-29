using System;
using Newtonsoft.Json;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Response for GetProjectInfo command
    /// Contains comprehensive Unity project and system information
    /// Related classes:
    /// - BaseCommandResponse: Provides timing information
    /// - GetProjectInfoCommand: Creates instances of this response
    /// </summary>
    public class GetProjectInfoResponse : BaseCommandResponse
    {
        public string ProjectName { get; }
        public string CompanyName { get; }
        public string Version { get; }
        public string UnityVersion { get; }
        public string Platform { get; }
        public string DataPath { get; }
        public string PersistentDataPath { get; }
        public string TemporaryCachePath { get; }
        public bool IsEditor { get; }
        public bool IsPlaying { get; }
        public int TargetFrameRate { get; }
        public bool RunInBackground { get; }
        public string SystemLanguage { get; }
        public string InternetReachability { get; }
        public string DeviceType { get; }
        public string DeviceModel { get; }
        public string OperatingSystem { get; }
        public string ProcessorType { get; }
        public int ProcessorCount { get; }
        public int SystemMemorySize { get; }
        public string GraphicsDeviceName { get; }
        public DateTime Timestamp { get; }
        public string CommandName { get; }

        [JsonConstructor]
        public GetProjectInfoResponse(string projectName, string companyName, string version, 
                                    string unityVersion, string platform, string dataPath, 
                                    string persistentDataPath, string temporaryCachePath, 
                                    bool isEditor, bool isPlaying, int targetFrameRate, 
                                    bool runInBackground, string systemLanguage, string internetReachability,
                                    string deviceType, string deviceModel, string operatingSystem,
                                    string processorType, int processorCount, int systemMemorySize,
                                    string graphicsDeviceName, DateTime timestamp, string commandName)
        {
            ProjectName = projectName ?? string.Empty;
            CompanyName = companyName ?? string.Empty;
            Version = version ?? string.Empty;
            UnityVersion = unityVersion ?? string.Empty;
            Platform = platform ?? string.Empty;
            DataPath = dataPath ?? string.Empty;
            PersistentDataPath = persistentDataPath ?? string.Empty;
            TemporaryCachePath = temporaryCachePath ?? string.Empty;
            IsEditor = isEditor;
            IsPlaying = isPlaying;
            TargetFrameRate = targetFrameRate;
            RunInBackground = runInBackground;
            SystemLanguage = systemLanguage ?? string.Empty;
            InternetReachability = internetReachability ?? string.Empty;
            DeviceType = deviceType ?? string.Empty;
            DeviceModel = deviceModel ?? string.Empty;
            OperatingSystem = operatingSystem ?? string.Empty;
            ProcessorType = processorType ?? string.Empty;
            ProcessorCount = processorCount;
            SystemMemorySize = systemMemorySize;
            GraphicsDeviceName = graphicsDeviceName ?? string.Empty;
            Timestamp = timestamp;
            CommandName = commandName ?? string.Empty;
        }
    }
}