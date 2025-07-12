using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace io.github.hatayama.uLoopMCP
{
    /// <summary>
    /// MCP Resource Manager
    /// 
    /// Design document reference: Implemented according to MCP Resources specification
    /// 
    /// Related classes:
    /// - MenuItemsResourceProvider: Provides Unity MenuItem list
    /// - SearchProvidersResourceProvider: Provides Unity Search provider details
    /// - McpResourceProvider: Base class for resource providers
    /// - McpResourceInfo: Data class for storing resource information
    /// - McpResourceListResponse: Response class for resources/list
    /// - McpResourceReadResponse: Response class for resources/read
    /// </summary>
    public class McpResourceManager
    {
        private static McpResourceManager _instance;
        private readonly Dictionary<string, McpResourceProvider> _resourceProviders;
        private readonly Dictionary<string, McpResourceInfo> _resourceCache;

        /// <summary>
        /// Singleton instance
        /// </summary>
        public static McpResourceManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    McpLogger.LogDebug("Creating new McpResourceManager instance...");
                    _instance = new McpResourceManager();
                    McpLogger.LogDebug("McpResourceManager instance created successfully");
                }
                return _instance;
            }
        }

        /// <summary>
        /// Private constructor
        /// </summary>
        private McpResourceManager()
        {
            _resourceProviders = new Dictionary<string, McpResourceProvider>();
            _resourceCache = new Dictionary<string, McpResourceInfo>();
            
            RegisterResourcesWithAttributes();
        }

        /// <summary>
        /// アトリビュートベースでリソースプロバイダーを自動発見・登録
        /// </summary>
        private void RegisterResourcesWithAttributes()
        {
            try
            {
                McpLogger.LogDebug("Starting resource registration with attributes...");
                
                // Get all assemblies
                Assembly[] assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
                McpLogger.LogDebug($"Found {assemblies.Length} assemblies to scan");

                List<Type> resourceTypes = new List<Type>();

                foreach (Assembly assembly in assemblies)
                {
                    // Find classes that inherit from McpResourceProvider and have McpResource attribute
                    Type[] types = assembly.GetTypes()
                        .Where(type => type.GetCustomAttribute<McpResourceAttribute>() != null)
                        .Where(type => typeof(McpResourceProvider).IsAssignableFrom(type))
                        .Where(type => !type.IsAbstract && !type.IsInterface)
                        .ToArray();

                    if (types.Length > 0)
                    {
                        McpLogger.LogDebug($"Found {types.Length} resource types in assembly {assembly.FullName}");
                        foreach (Type type in types)
                        {
                            McpLogger.LogDebug($"  - {type.FullName}");
                        }
                    }

                    resourceTypes.AddRange(types);
                }

                McpLogger.LogDebug($"Total resource types found: {resourceTypes.Count}");

                // Register all resources
                foreach (Type type in resourceTypes)
                {
                    // Security: Validate type before creating instance
                    if (!IsValidResourceType(type))
                    {
                        UnityEngine.Debug.LogWarning($"{McpConstants.SECURITY_LOG_PREFIX} Skipping invalid resource type: {type.FullName}");
                        continue;
                    }
                    
                    McpResourceProvider resource = (McpResourceProvider)Activator.CreateInstance(type);
                    RegisterResourceProvider(resource);
                    McpLogger.LogDebug($"Successfully registered resource provider: {type.FullName} -> {resource.ResourceUri}");
                }
                
                McpLogger.LogDebug($"Resource registration completed. Total registered: {_resourceProviders.Count}");
            }
            catch (Exception ex)
            {
                McpLogger.LogError($"Failed to register resources with attributes: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// リソースプロバイダーを登録
        /// </summary>
        /// <param name="provider">リソースプロバイダー</param>
        public void RegisterResourceProvider(McpResourceProvider provider)
        {
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            _resourceProviders[provider.ResourceUri] = provider;
        }

        /// <summary>
        /// セキュリティ: リソースタイプが安全にインスタンス化できるかを検証
        /// </summary>
        /// <param name="type">検証するタイプ</param>
        /// <returns>安全な場合はtrue</returns>
        private bool IsValidResourceType(Type type)
        {
            try
            {
                // Must inherit from McpResourceProvider
                if (!typeof(McpResourceProvider).IsAssignableFrom(type))
                {
                    return false;
                }
                
                // Must not be abstract class or interface
                if (type.IsAbstract || type.IsInterface)
                {
                    return false;
                }
                
                // Must have McpResource attribute
                if (type.GetCustomAttribute<McpResourceAttribute>() == null)
                {
                    return false;
                }
                
                // Must be in uLoopMCP namespace (security restriction)
                if (!type.Namespace?.StartsWith(McpConstants.ULOOPMCP_NAMESPACE_PREFIX) == true)
                {
                    return false;
                }
                
                // Must have parameterless constructor
                if (type.GetConstructor(Type.EmptyTypes) == null)
                {
                    return false;
                }
                
                return true;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"{McpConstants.SECURITY_LOG_PREFIX} Error validating resource type {type?.FullName}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// resources/listリクエストを処理
        /// </summary>
        /// <param name="paramsToken">リクエストパラメータ</param>
        /// <returns>リソース一覧レスポンス</returns>
        public async Task<BaseToolResponse> HandleResourcesListAsync(JToken paramsToken)
        {
            try
            {
                // パラメータの解析
                string cursor = null;
                if (paramsToken != null && paramsToken["cursor"] != null)
                {
                    cursor = paramsToken["cursor"].ToString();
                }

                // 全てのリソースを取得
                List<McpResourceInfo> allResources = new List<McpResourceInfo>();
                
                foreach (McpResourceProvider provider in _resourceProviders.Values)
                {
                    try
                    {
                        McpResourceInfo[] providerResources = await provider.GetResourcesAsync();
                        allResources.AddRange(providerResources);
                    }
                    catch (Exception ex)
                    {
                        McpLogger.LogError($"Error getting resources from provider {provider.ResourceUri}: {ex.Message}");
                    }
                }

                // カーソルベースのページネーション（簡易実装）
                McpResourceInfo[] resources = allResources.ToArray();
                string nextCursor = null;

                // レスポンスを作成
                McpResourceListResponse response = new McpResourceListResponse
                {
                    Resources = resources,
                    NextCursor = nextCursor
                };
                
                McpLogger.LogDebug($"Created McpResourceListResponse with {resources.Length} resources");
                foreach (McpResourceInfo resource in resources)
                {
                    McpLogger.LogDebug($"Resource: {resource.Uri} - {resource.Name}");
                }
                
                return response;
            }
            catch (Exception ex)
            {
                McpLogger.LogError($"Error handling resources/list: {ex.Message}");
                return new McpResourceListResponse
                {
                    Resources = new McpResourceInfo[0],
                    NextCursor = null
                };
            }
        }

        /// <summary>
        /// resources/readリクエストを処理
        /// </summary>
        /// <param name="paramsToken">リクエストパラメータ</param>
        /// <returns>リソース読み取りレスポンス</returns>
        public async Task<BaseToolResponse> HandleResourcesReadAsync(JToken paramsToken)
        {
            try
            {
                // パラメータの解析
                if (paramsToken == null || paramsToken["uri"] == null)
                {
                    return new McpResourceReadResponse
                    {
                        Error = "URI parameter is required"
                    };
                }

                string uri = paramsToken["uri"].ToString();

                // リソースプロバイダーを検索
                McpResourceProvider provider = _resourceProviders.Values
                    .FirstOrDefault(p => p.CanHandleUri(uri));

                if (provider == null)
                {
                    return new McpResourceReadResponse
                    {
                        Error = $"Resource not found: {uri}"
                    };
                }

                // リソースの内容を取得
                McpResourceContent content = await provider.ReadResourceAsync(uri);

                return new McpResourceReadResponse
                {
                    Contents = new[] { content }
                };
            }
            catch (Exception ex)
            {
                McpLogger.LogError($"Error handling resources/read: {ex.Message}");
                return new McpResourceReadResponse
                {
                    Error = $"Error reading resource: {ex.Message}"
                };
            }
        }
    }

    /// <summary>
    /// リソース情報を格納するデータクラス
    /// MCP Resource schema準拠: uri(必須), name(必須), description(オプション), mimeType(オプション)
    /// </summary>
    [Serializable]
    public class McpResourceInfo
    {
        [JsonProperty("uri")]
        public string Uri { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("mimeType")]
        public string MimeType { get; set; }
    }

    /// <summary>
    /// リソース内容を格納するデータクラス
    /// MCP Resource content準拠: uri, mimeType, text または blob
    /// </summary>
    [Serializable]
    public class McpResourceContent
    {
        [JsonProperty("uri")]
        public string Uri { get; set; }

        [JsonProperty("mimeType")]
        public string MimeType { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("blob")]
        public string Blob { get; set; }
    }

    /// <summary>
    /// resources/listのレスポンスクラス
    /// </summary>
    [Serializable]
    public class McpResourceListResponse : BaseToolResponse
    {
        [JsonProperty("resources")]
        public McpResourceInfo[] Resources { get; set; }

        [JsonProperty("nextCursor")]
        public string NextCursor { get; set; }
    }

    /// <summary>
    /// resources/readのレスポンスクラス
    /// </summary>
    [Serializable]
    public class McpResourceReadResponse : BaseToolResponse
    {
        [JsonProperty("contents")]
        public McpResourceContent[] Contents { get; set; }

        [JsonProperty("error")]
        public string Error { get; set; }
    }

    /// <summary>
    /// リソースプロバイダーの基底クラス
    /// </summary>
    public abstract class McpResourceProvider
    {
        /// <summary>
        /// リソースのURI
        /// </summary>
        public abstract string ResourceUri { get; }

        /// <summary>
        /// 指定されたURIを処理できるかチェック
        /// </summary>
        /// <param name="uri">URI</param>
        /// <returns>処理可能な場合はtrue</returns>
        public virtual bool CanHandleUri(string uri)
        {
            return uri == ResourceUri;
        }

        /// <summary>
        /// リソース一覧を取得
        /// </summary>
        /// <returns>リソース一覧</returns>
        public abstract Task<McpResourceInfo[]> GetResourcesAsync();

        /// <summary>
        /// リソースの内容を読み取り
        /// </summary>
        /// <param name="uri">リソースのURI</param>
        /// <returns>リソース内容</returns>
        public abstract Task<McpResourceContent> ReadResourceAsync(string uri);
    }
}