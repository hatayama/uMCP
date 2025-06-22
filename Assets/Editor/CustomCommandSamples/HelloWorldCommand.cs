using System;
using System.Threading.Tasks;
using UnityEngine;
using System.Collections.Generic;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Hello World custom command - Type-safe implementation using Schema and Response
    /// Basic implementation example of a custom command with strongly typed parameters and response
    /// </summary>
    public class HelloWorldCommand : AbstractUnityCommand<HelloWorldSchema, HelloWorldResponse>
    {
        public override string CommandName => "helloworld";
        public override string Description => "Personalized hello world command with name parameter";

        public override CommandParameterSchema ParameterSchema => 
            CommandParameterSchemaGenerator.FromDto<HelloWorldSchema>();

        protected override Task<HelloWorldResponse> ExecuteAsync(HelloWorldSchema parameters)
        {
            // Type-safe parameter access - no more string parsing!
            string name = parameters.Name;
            GreetingLanguage language = parameters.Language;
            bool includeTimestamp = parameters.IncludeTimestamp;

            // Generate greeting based on language
            string greeting = language switch
            {
                GreetingLanguage.japanese => $"こんにちは、{name}さん！",
                GreetingLanguage.spanish => $"¡Hola, {name}!",
                GreetingLanguage.french => $"Bonjour, {name}!",
                _ => $"Hello, {name}!"
            };

            // Create type-safe response
            HelloWorldResponse response = new HelloWorldResponse(
                message: greeting,
                language: language.ToString().ToLower(),
                timestamp: includeTimestamp ? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") : null
            );

            return Task.FromResult(response);
        }
    }
} 