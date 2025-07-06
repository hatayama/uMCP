using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Base interface for Unity MCP command handlers
    /// Following the Open-Closed principle, when adding new commands,
    /// create a new class that implements this interface
    /// </summary>
    public interface IUnityCommand
    {
        /// <summary>
        /// Get command name
        /// </summary>
        string CommandName { get; }
        
        /// <summary>
        /// Get command description
        /// </summary>
        string Description { get; }
        
        /// <summary>
        /// Get parameter schema information for TypeScript side
        /// </summary>
        CommandParameterSchema ParameterSchema { get; }
        
        /// <summary>
        /// Execute command
        /// </summary>
        /// <param name="paramsToken">JSON token for parameters</param>
        /// <returns>Execution result</returns>
        Task<BaseCommandResponse> ExecuteAsync(JToken paramsToken);
    }
} 