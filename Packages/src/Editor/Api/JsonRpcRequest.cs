using Newtonsoft.Json.Linq;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Represents a parsed JSON-RPC request
    /// </summary>
    internal class JsonRpcRequest
    {
        public string Method { get; set; }
        /// <summary>
        /// JSON-RPC 2.0 spec allows params to be object, array, or null.
        /// We use JToken to accept any format for maximum flexibility.
        /// </summary>
        public JToken Params { get; set; }
        /// <summary>
        /// JSON-RPC 2.0 spec requires id type to match the request.
        /// Must be string, number, or null - same as received.
        /// </summary>
        public object Id { get; set; }
        /// <summary>
        /// JSON-RPC 2.0 notification flag. True when id is null/missing.
        /// Notifications are fire-and-forget messages that don't expect a response.
        /// Regular requests (with id) expect a response, notifications do not.
        /// </summary>
        public bool IsNotification => Id == null;
    }
}