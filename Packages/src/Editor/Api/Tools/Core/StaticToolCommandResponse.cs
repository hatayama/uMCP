using Newtonsoft.Json;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Response wrapper for static tool results
    /// </summary>
    public class StaticToolCommandResponse : BaseCommandResponse
    {
        [JsonProperty("result")] public object Result { get; }

        public StaticToolCommandResponse(object result)
        {
            Result = result;
        }
    }
}