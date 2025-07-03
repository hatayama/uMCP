namespace io.github.hatayama.uMCP
{
    public class FindGameObjectSchema : BaseCommandSchema
    {
        public string Path { get; set; } = "";
        public bool IncludeInheritedProperties { get; set; } = false;
    }
}