using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Advanced custom command example
    /// Demonstrates complex parameter handling and file operations
    /// </summary>
    public class AdvancedCustomCommand : IUnityCommand
    {
        public string CommandName => "advancedcustom";
        public string Description => "Advanced custom command with multiple parameter types and file operations";

        public Task<object> ExecuteAsync(JToken paramsToken)
        {
            // Parse various parameter types
            AdvancedParameters parameters = ParseParameters(paramsToken);
            
            Debug.Log($"Advanced command executed with operation: {parameters.Operation}");
            
            // Execute different operations based on the operation type
            object result = parameters.Operation.ToLower() switch
            {
                "listfiles" => ListFiles(parameters),
                "createfile" => CreateFile(parameters),
                "readfile" => ReadFile(parameters),
                "getinfo" => GetSystemInfo(parameters),
                _ => new { error = $"Unknown operation: {parameters.Operation}", availableOperations = new[] { "listfiles", "createfile", "readfile", "getinfo" } }
            };
            
            return Task.FromResult(result);
        }
        
        /// <summary>
        /// Parse parameters from JSON token
        /// </summary>
        private AdvancedParameters ParseParameters(JToken paramsToken)
        {
            string operation = paramsToken?["operation"]?.ToString() ?? "getinfo";
            string path = paramsToken?["path"]?.ToString() ?? Application.dataPath;
            string content = paramsToken?["content"]?.ToString() ?? "";
            string[] extensions = paramsToken?["extensions"]?.ToObject<string[]>() ?? new[] { ".cs", ".txt" };
            int maxResults = paramsToken?["maxResults"]?.ToObject<int>() ?? 10;
            bool includeHidden = paramsToken?["includeHidden"]?.ToObject<bool>() ?? false;
            bool recursive = paramsToken?["recursive"]?.ToObject<bool>() ?? false;
            
            return new AdvancedParameters(operation, path, content, extensions, maxResults, includeHidden, recursive);
        }
        
        /// <summary>
        /// List files in specified directory
        /// </summary>
        private object ListFiles(AdvancedParameters parameters)
        {
            try
            {
                if (!Directory.Exists(parameters.Path))
                {
                    return new { error = $"Directory not found: {parameters.Path}" };
                }
                
                SearchOption searchOption = parameters.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                List<string> files = new List<string>();
                
                foreach (string extension in parameters.Extensions)
                {
                    string pattern = $"*{extension}";
                    files.AddRange(Directory.GetFiles(parameters.Path, pattern, searchOption));
                }
                
                // Filter hidden files if not included
                if (!parameters.IncludeHidden)
                {
                    files = files.Where(f => !Path.GetFileName(f).StartsWith(".")).ToList();
                }
                
                // Limit results
                if (files.Count > parameters.MaxResults)
                {
                    files = files.Take(parameters.MaxResults).ToList();
                }
                
                return new
                {
                    operation = "listfiles",
                    path = parameters.Path,
                    totalFound = files.Count,
                    maxResults = parameters.MaxResults,
                    extensions = parameters.Extensions,
                    recursive = parameters.Recursive,
                    includeHidden = parameters.IncludeHidden,
                    files = files.Select(f => new
                    {
                        name = Path.GetFileName(f),
                        fullPath = f,
                        relativePath = Path.GetRelativePath(Application.dataPath, f),
                        size = new FileInfo(f).Length,
                        lastModified = File.GetLastWriteTime(f)
                    }).ToArray()
                };
            }
            catch (System.Exception ex)
            {
                return new { error = $"Failed to list files: {ex.Message}" };
            }
        }
        
        /// <summary>
        /// Create a new file with specified content
        /// </summary>
        private object CreateFile(AdvancedParameters parameters)
        {
            try
            {
                string directory = Path.GetDirectoryName(parameters.Path);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                File.WriteAllText(parameters.Path, parameters.Content);
                
                return new
                {
                    operation = "createfile",
                    path = parameters.Path,
                    contentLength = parameters.Content.Length,
                    created = File.GetCreationTime(parameters.Path),
                    success = true
                };
            }
            catch (System.Exception ex)
            {
                return new { error = $"Failed to create file: {ex.Message}" };
            }
        }
        
        /// <summary>
        /// Read content from specified file
        /// </summary>
        private object ReadFile(AdvancedParameters parameters)
        {
            try
            {
                if (!File.Exists(parameters.Path))
                {
                    return new { error = $"File not found: {parameters.Path}" };
                }
                
                string content = File.ReadAllText(parameters.Path);
                FileInfo fileInfo = new FileInfo(parameters.Path);
                
                return new
                {
                    operation = "readfile",
                    path = parameters.Path,
                    content = content,
                    size = fileInfo.Length,
                    lastModified = fileInfo.LastWriteTime,
                    lineCount = content.Split('\n').Length
                };
            }
            catch (System.Exception ex)
            {
                return new { error = $"Failed to read file: {ex.Message}" };
            }
        }
        
        /// <summary>
        /// Get system information
        /// </summary>
        private object GetSystemInfo(AdvancedParameters parameters)
        {
            return new
            {
                operation = "getinfo",
                unity = new
                {
                    version = Application.unityVersion,
                    platform = Application.platform.ToString(),
                    isEditor = Application.isEditor,
                    isPlaying = Application.isPlaying,
                    dataPath = Application.dataPath,
                    persistentDataPath = Application.persistentDataPath
                },
                system = new
                {
                    operatingSystem = SystemInfo.operatingSystem,
                    processorType = SystemInfo.processorType,
                    processorCount = SystemInfo.processorCount,
                    systemMemorySize = SystemInfo.systemMemorySize,
                    graphicsDeviceName = SystemInfo.graphicsDeviceName,
                    deviceType = SystemInfo.deviceType.ToString()
                },
                timestamp = System.DateTime.Now
            };
        }
    }
    
    /// <summary>
    /// Parameter class for advanced command
    /// </summary>
    public class AdvancedParameters
    {
        public string Operation { get; }
        public string Path { get; }
        public string Content { get; }
        public string[] Extensions { get; }
        public int MaxResults { get; }
        public bool IncludeHidden { get; }
        public bool Recursive { get; }

        public AdvancedParameters(string operation, string path, string content, string[] extensions, int maxResults, bool includeHidden, bool recursive)
        {
            Operation = operation;
            Path = path;
            Content = content;
            Extensions = extensions;
            MaxResults = maxResults;
            IncludeHidden = includeHidden;
            Recursive = recursive;
        }
    }
} 