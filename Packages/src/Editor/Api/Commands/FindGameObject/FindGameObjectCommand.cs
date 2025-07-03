using System.Threading.Tasks;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Command to find GameObject by path and return detailed component information
    /// Related classes:
    /// - GameObjectFinderService: Core logic for finding GameObjects
    /// - ComponentSerializer: Serializes component information
    /// </summary>
    [McpTool]
    public class FindGameObjectCommand : AbstractUnityCommand<FindGameObjectSchema, FindGameObjectResponse>
    {
        public override string CommandName => "findgameobject";
        public override string Description => "Find GameObject by path and return detailed component information";
        
        protected override Task<FindGameObjectResponse> ExecuteAsync(FindGameObjectSchema parameters)
        {
            GameObjectFinderService service = new GameObjectFinderService();
            ComponentSerializer serializer = new ComponentSerializer();
            
            GameObjectDetails details = service.FindGameObject(parameters.Path);
            
            if (!details.Found)
            {
                return Task.FromResult(new FindGameObjectResponse
                {
                    found = false,
                    errorMessage = details.ErrorMessage
                });
            }
            
            ComponentInfo[] components = serializer.SerializeComponents(details.GameObject, parameters.IncludeInheritedProperties);
            
            FindGameObjectResponse response = new FindGameObjectResponse
            {
                found = true,
                name = details.Name,
                path = details.Path,
                isActive = details.IsActive,
                components = components
            };
            
            return Task.FromResult(response);
        }
    }
}