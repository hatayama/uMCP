namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Unity MCP Editor UI constants
    /// Centralized management of UI-related constants
    /// </summary>
    public static class McpUIConstants
    {
        // Communication log area
        public const float DEFAULT_COMMUNICATION_LOG_HEIGHT = 300f;

        // UI spacing and dimensions
        public const float BUTTON_HEIGHT_LARGE = 30f;
        public const float BUTTON_HEIGHT_MEDIUM = 25f;
        public const float BUTTON_WIDTH_CLEAR = 60f;
        
        // Content area heights
        public const float JSON_SCROLL_FIXED_HEIGHT = 150f;
        public const float JSON_CONTENT_PADDING = 50f;
        
        // UI colors (as floats for Unity Color)

        // Communication log settings
        public const int MAX_COMMUNICATION_LOG_ENTRIES = 20;
        
        // Connected clients display
        public const float CLIENT_ITEM_SPACING = 3f;
        public const string CONNECTED_TOOLS_FOLDOUT_TEXT = "Connected LLM Tools";
        public const string CLIENT_ICON = "● ";
        public const string ENDPOINT_ARROW = "→ ";
        public const string RECONNECTING_MESSAGE = "Reconnecting...";
    }
}