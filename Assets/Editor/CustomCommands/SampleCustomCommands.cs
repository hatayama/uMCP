using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEditor;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// カスタムコマンドのサンプル実装
    /// ユーザーが独自のコマンドを追加する際の参考例
    /// </summary>
    [InitializeOnLoad]
    public static class SampleCustomCommands
    {
        /// <summary>
        /// Unity起動時に自動実行される静的コンストラクタ
        /// </summary>
        static SampleCustomCommands()
        {
            // Unity起動時にサンプルコマンドを自動登録
            RegisterSampleCommandsAutomatically();
        }

        /// <summary>
        /// Unity起動時にサンプルコマンドを自動登録する
        /// </summary>
        private static void RegisterSampleCommandsAutomatically()
        {
            try
            {
                CustomCommandManager.RegisterCustomCommand(new HelloWorldCommand());
                CustomCommandManager.RegisterCustomCommand(new GetProjectInfoCommand());
                CustomCommandManager.RegisterCustomCommand(new ClearConsoleCommand());
                CustomCommandManager.RegisterCustomCommand(new GetSceneInfoCommand());
                
                Debug.Log("[uMCP] Sample custom commands auto-registered successfully!");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[uMCP] Failed to auto-register sample commands: {ex.Message}");
            }
        }

        /// <summary>
        /// カスタムコマンドを登録する
        /// </summary>
        [MenuItem("uMCP/Custom Commands/Register Sample Commands")]
        public static void RegisterSampleCommands()
        {
            CustomCommandManager.RegisterCustomCommand(new HelloWorldCommand());
            CustomCommandManager.RegisterCustomCommand(new GetProjectInfoCommand());
            CustomCommandManager.RegisterCustomCommand(new ClearConsoleCommand());
            CustomCommandManager.RegisterCustomCommand(new GetSceneInfoCommand());
            
            Debug.Log("Sample custom commands registered successfully!");
            Debug.Log("Available commands: " + string.Join(", ", CustomCommandManager.GetRegisteredCustomCommands().Select(c => c.Name)));
        }

        /// <summary>
        /// カスタムコマンドを登録解除する
        /// </summary>
        [MenuItem("uMCP/Custom Commands/Unregister Sample Commands")]
        public static void UnregisterSampleCommands()
        {
            CustomCommandManager.UnregisterCustomCommand("helloworld");
            CustomCommandManager.UnregisterCustomCommand("getprojectinfo");
            CustomCommandManager.UnregisterCustomCommand("clearconsole");
            CustomCommandManager.UnregisterCustomCommand("getsceneinfo");
            
            Debug.Log("Sample custom commands unregistered successfully!");
        }

        /// <summary>
        /// 現在登録されているコマンドの一覧を表示する
        /// </summary>
        [MenuItem("uMCP/Custom Commands/Show Registered Commands")]
        public static void ShowRegisteredCommands()
        {
            CommandInfo[] commands = CustomCommandManager.GetRegisteredCustomCommands();
            Debug.Log($"Currently registered commands ({commands.Length}):");
            
            for (int i = 0; i < commands.Length; i++)
            {
                Debug.Log($"{i + 1}. {commands[i].Name} - {commands[i].Description}");
            }
            
            // さらに詳細なデバッグ情報
            Debug.Log("=== Debug Info ===");
            Debug.Log(CustomCommandManager.GetDebugInfo());
        }
    }

    /// <summary>
    /// Hello World カスタムコマンド
    /// 基本的なカスタムコマンドの実装例
    /// </summary>
    public class HelloWorldCommand : IUnityCommand
    {
        public string CommandName => "helloworld";
        public string Description => "Simple hello world command example";

        public Task<object> ExecuteAsync(JToken paramsToken)
        {
            string name = paramsToken?["name"]?.ToString() ?? "World";
            string message = $"Hello, {name}! This is a custom command from Unity.";
            
            Debug.Log($"HelloWorld command executed with name: {name}");
            
            return Task.FromResult<object>(new
            {
                message = message,
                timestamp = System.DateTime.Now,
                commandName = CommandName
            });
        }
    }

    /// <summary>
    /// プロジェクト情報取得カスタムコマンド
    /// Unityプロジェクトの詳細情報を取得する例
    /// </summary>
    public class GetProjectInfoCommand : IUnityCommand
    {
        public string CommandName => "getprojectinfo";
        public string Description => "Get detailed Unity project information";

        public Task<object> ExecuteAsync(JToken paramsToken)
        {
            Debug.Log("GetProjectInfo command executed");
            
            return Task.FromResult<object>(new
            {
                projectName = Application.productName,
                companyName = Application.companyName,
                version = Application.version,
                unityVersion = Application.unityVersion,
                platform = Application.platform.ToString(),
                dataPath = Application.dataPath,
                persistentDataPath = Application.persistentDataPath,
                temporaryCachePath = Application.temporaryCachePath,
                isEditor = Application.isEditor,
                isPlaying = Application.isPlaying,
                targetFrameRate = Application.targetFrameRate,
                runInBackground = Application.runInBackground,
                systemLanguage = Application.systemLanguage.ToString(),
                internetReachability = Application.internetReachability.ToString(),
                deviceType = SystemInfo.deviceType.ToString(),
                deviceModel = SystemInfo.deviceModel,
                operatingSystem = SystemInfo.operatingSystem,
                processorType = SystemInfo.processorType,
                processorCount = SystemInfo.processorCount,
                systemMemorySize = SystemInfo.systemMemorySize,
                graphicsDeviceName = SystemInfo.graphicsDeviceName,
                graphicsMemorySize = SystemInfo.graphicsMemorySize
            });
        }
    }

    /// <summary>
    /// コンソールクリアカスタムコマンド
    /// Unityコンソールをクリアする例
    /// </summary>
    public class ClearConsoleCommand : IUnityCommand
    {
        public string CommandName => "clearconsole";
        public string Description => "Clear Unity console logs";

        public Task<object> ExecuteAsync(JToken paramsToken)
        {
            Debug.Log("ClearConsole command executed");
            
            // Unityコンソールをクリアする
            LogGetter.ClearCustomLogs();
            
            return Task.FromResult<object>(new
            {
                message = "Unity console cleared successfully",
                timestamp = System.DateTime.Now,
                commandName = CommandName
            });
        }
    }

    /// <summary>
    /// シーン情報取得カスタムコマンド
    /// 現在のシーン情報を取得する例
    /// </summary>
    public class GetSceneInfoCommand : IUnityCommand
    {
        public string CommandName => "getsceneinfo";
        public string Description => "Get current scene information";

        public Task<object> ExecuteAsync(JToken paramsToken)
        {
            Debug.Log("GetSceneInfo command executed");
            
            UnityEngine.SceneManagement.Scene currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            
            return Task.FromResult<object>(new
            {
                sceneName = currentScene.name,
                scenePath = currentScene.path,
                sceneIndex = currentScene.buildIndex,
                isLoaded = currentScene.isLoaded,
                rootCount = currentScene.rootCount,
                gameObjectCount = currentScene.GetRootGameObjects().Length,
                timestamp = System.DateTime.Now,
                commandName = CommandName
            });
        }
    }
} 