using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;
using System.Collections.Generic;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Console clear custom command
    /// Example of clearing Unity console
    /// </summary>
    public class ClearConsoleCommand : IUnityCommand
    {
        public string CommandName => "clearconsole";
        public string Description => "Clear Unity console logs";

        public CommandParameterSchema ParameterSchema => new CommandParameterSchema();

        public Task<object> ExecuteAsync(JToken paramsToken)
        {
            Debug.Log("ClearConsole command executed");
            
            // Clear Unity console
            LogGetter.ClearCustomLogs();
            
            return Task.FromResult<object>(new
            {
                message = "Unity console cleared successfully",
                timestamp = System.DateTime.Now,
                commandName = CommandName
            });
        }
    }
} 