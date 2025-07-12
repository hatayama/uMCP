using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using uLoopMCP.Editor.Api.Commands.GetMenuItems;

namespace io.github.hatayama.uLoopMCP
{
    /// <summary>
    /// Unity MenuItems resource provider
    /// 
    /// Design document reference: Provides Unity MenuItems as MCP Resources
    /// 
    /// Related classes:
    /// - MenuItemDiscoveryService: MenuItem discovery service
    /// - MenuItemInfo: Data class for storing MenuItem information
    /// - McpResourceProvider: Base class for resource providers
    /// - McpResourceManager: Resource management class
    /// </summary>
    [McpResource(Description = "Unity Editor Menu Items with detailed metadata including method names, assemblies, and execution compatibility information")]
    public class MenuItemsResourceProvider : McpResourceProvider
    {
        private const string MENU_ITEMS_URI = "unity://menu-items";
        private const string MENU_ITEMS_MIME_TYPE = "application/json";

        /// <summary>
        /// リソースのURI
        /// </summary>
        public override string ResourceUri => MENU_ITEMS_URI;

        /// <summary>
        /// リソース一覧を取得
        /// </summary>
        /// <returns>リソース一覧</returns>
        public override Task<McpResourceInfo[]> GetResourcesAsync()
        {
            try
            {
                // MenuItemsの総数を取得（キャッシュ済みの場合は高速）
                List<MenuItemInfo> menuItems = MenuItemDiscoveryService.DiscoverAllMenuItems();
                
                McpResourceInfo resourceInfo = new McpResourceInfo
                {
                    Uri = MENU_ITEMS_URI,
                    Name = "Unity Menu Items",
                    Description = "Complete list of Unity Editor menu items with detailed metadata including method names, assemblies, and execution compatibility information",
                    MimeType = MENU_ITEMS_MIME_TYPE
                };

                return Task.FromResult(new[] { resourceInfo });
            }
            catch (Exception ex)
            {
                McpLogger.LogError($"Error getting menu items resources: {ex.Message}");
                return Task.FromResult(new McpResourceInfo[0]);
            }
        }

        /// <summary>
        /// リソースの内容を読み取り
        /// </summary>
        /// <param name="uri">リソースのURI</param>
        /// <returns>リソース内容</returns>
        public override Task<McpResourceContent> ReadResourceAsync(string uri)
        {
            try
            {
                if (uri != MENU_ITEMS_URI)
                {
                    throw new ArgumentException($"Invalid URI: {uri}");
                }

                // MenuItemsを取得
                List<MenuItemInfo> menuItems = MenuItemDiscoveryService.DiscoverAllMenuItems();
                
                // JSON形式に変換
                MenuItemsResourceData resourceData = new MenuItemsResourceData
                {
                    MenuItems = menuItems.ToArray(),
                    TotalCount = menuItems.Count,
                    GeneratedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                };

                string jsonContent = JsonConvert.SerializeObject(resourceData, Formatting.Indented);

                McpResourceContent content = new McpResourceContent
                {
                    Uri = uri,
                    MimeType = MENU_ITEMS_MIME_TYPE,
                    Text = jsonContent
                };

                return Task.FromResult(content);
            }
            catch (Exception ex)
            {
                McpLogger.LogError($"Error reading menu items resource: {ex.Message}");
                throw;
            }
        }

    }

    /// <summary>
    /// MenuItemsリソースデータ
    /// </summary>
    [Serializable]
    public class MenuItemsResourceData
    {
        [JsonProperty("menuItems")]
        public MenuItemInfo[] MenuItems { get; set; }

        [JsonProperty("totalCount")]
        public int TotalCount { get; set; }

        [JsonProperty("generatedAt")]
        public string GeneratedAt { get; set; }
    }
}