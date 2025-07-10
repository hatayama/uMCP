using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Tool for exporting hierarchy results to external files
    /// Related classes:
    /// - HierarchyNodeNested: Data structure for hierarchy nodes
    /// - GetHierarchyResponse: Response structure containing hierarchy data
    /// - HierarchyContext: Context information for hierarchy
    /// </summary>
    public static class HierarchyResultExporter
    {
        private const string EXPORT_DIR = "HierarchyResults";
        private const string FILE_PREFIX = "hierarchy";
        
        /// <summary>
        /// Export hierarchy results to JSON file
        /// </summary>
        /// <param name="hierarchyNodes">List of hierarchy nodes to export</param>
        /// <param name="context">Context information about the hierarchy</param>
        /// <returns>Relative path to the exported file</returns>
        public static string ExportHierarchyResults(List<HierarchyNodeNested> hierarchyNodes, HierarchyContext context)
        {
            if (hierarchyNodes == null || hierarchyNodes.Count == 0)
            {
                throw new ArgumentException("Cannot export empty hierarchy results");
            }
            
            // Create export directory if it doesn't exist
            string exportDir = Path.Combine(Application.dataPath, "..", EXPORT_DIR);
            Directory.CreateDirectory(exportDir);
            
            // Generate filename with timestamp
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string filename = $"{FILE_PREFIX}_{timestamp}.json";
            string filePath = Path.Combine(exportDir, filename);
            
            // Create export data structure
            var exportData = new HierarchyExportData
            {
                ExportTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Hierarchy = hierarchyNodes,
                Context = context
            };
            
            // Export to JSON using Newtonsoft.Json for proper serialization
            var settings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                MaxDepth = McpServerConfig.DEFAULT_JSON_MAX_DEPTH,
                Formatting = Formatting.Indented
            };
            string jsonContent = JsonConvert.SerializeObject(exportData, settings);
            File.WriteAllText(filePath, jsonContent);
            
            // Return relative path
            return Path.Combine(EXPORT_DIR, filename);
        }
        
        /// <summary>
        /// Data structure for hierarchy export
        /// </summary>
        [Serializable]
        public class HierarchyExportData
        {
            public string ExportTimestamp;
            public List<HierarchyNodeNested> Hierarchy;
            public HierarchyContext Context;
        }
    }
}