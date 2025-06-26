namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// Unity MCP Editor UI constants
    /// Centralized management of UI-related constants
    /// </summary>
    public static class McpUIConstants
    {
        // Window size constraints
        public const float MIN_WINDOW_WIDTH = 400f;
        public const float MIN_WINDOW_HEIGHT = 200f;
        
        // Communication log area
        public const float DEFAULT_COMMUNICATION_LOG_HEIGHT = 300f;
        public const float MIN_COMMUNICATION_LOG_HEIGHT = 100f;
        public const float MAX_COMMUNICATION_LOG_HEIGHT = 800f;
        
        // UI element sizes
        public const float RESIZE_HANDLE_HEIGHT = 8f;
        public const float DOT_SIZE = 2f;
        public const float DOT_SPACING = 4f;
        
        // UI spacing and dimensions
        public const float LABEL_WIDTH_SERVER_STATUS = 85f;
        public const float LABEL_WIDTH_STATUS = 80f;
        public const float BUTTON_HEIGHT_LARGE = 30f;
        public const float BUTTON_HEIGHT_MEDIUM = 25f;
        public const float BUTTON_WIDTH_CLEAR = 60f;
        
        // Content area heights
        public const float JSON_CONTENT_MIN_HEIGHT = 80f;
        public const float JSON_CONTENT_MAX_HEIGHT = 200f;
        public const float JSON_SCROLL_FIXED_HEIGHT = 150f;
        public const float JSON_CONTENT_PADDING = 50f;
        
        // UI colors (as floats for Unity Color)
        public const float RESIZE_HANDLE_BACKGROUND_ALPHA = 0.3f;
        public const float RESIZE_HANDLE_DOT_ALPHA = 0.8f;
        public const float RESIZE_HANDLE_DOT_BRIGHTNESS = 0.6f;
        
        // Communication log settings
        public const int MAX_COMMUNICATION_LOG_ENTRIES = 20;
        
        // Connected clients display
        public const float CONNECTED_CLIENT_ITEM_HEIGHT = 20f;
        public const float CONNECTED_CLIENT_INDENT = 16f;
    }
}