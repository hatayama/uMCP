using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Hello World custom command
    /// Basic implementation example of a custom command
    /// </summary>
    public class HelloWorldCommand : IUnityCommand
    {
        public string CommandName => "helloworld";
        public string Description => "Personalized hello world command with name parameter";

        public Task<object> ExecuteAsync(JToken paramsToken)
        {
            // Parse parameters with default values
            string name = paramsToken?["Name"]?.ToString() ?? "World";
            string language = paramsToken?["Language"]?.ToString() ?? "english";
            bool includeTimestamp = paramsToken?["IncludeTimestamp"]?.ToObject<bool>() ?? true;
            
            // Generate greeting based on language
            string greeting = GenerateGreeting(language, name);
            
            Debug.Log($"HelloWorld command executed with name: {name}, language: {language}, includeTimestamp: {includeTimestamp}");
            
            // Build response object
            object response = new
            {
                message = greeting,
                name = name,
                language = language,
                includeTimestamp = includeTimestamp,
                timestamp = includeTimestamp ? System.DateTime.Now : (System.DateTime?)null,
                commandName = CommandName
            };
            
            return Task.FromResult(response);
        }
        
        /// <summary>
        /// Generate greeting message based on language
        /// </summary>
        private string GenerateGreeting(string language, string name)
        {
            return language.ToLower() switch
            {
                "japanese" => $"こんにちは、{name}さん！これはUnityからのカスタムコマンドです。",
                "spanish" => $"¡Hola, {name}! Este es un comando personalizado de Unity.",
                "french" => $"Bonjour, {name}! Ceci est une commande personnalisée d'Unity.",
                "english" or _ => $"Hello, {name}! This is a custom command from Unity."
            };
        }
    }
} 