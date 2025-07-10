[日本語](FEATURES_ja.md)

# uMCP Feature Specifications

This document provides detailed information about all Unity MCP (Model Context Protocol) tools and features.

## Common Parameters & Response Format

All Unity MCP tools share the following common elements:

### Common Parameters
- `TimeoutSeconds` (number): Tool execution timeout in seconds (default: 10 seconds)

### Common Response Properties
All tools automatically include the following timing information:
- `StartedAt` (string): Tool execution start time (local time)
- `EndedAt` (string): Tool execution end time (local time)  
- `ExecutionTimeMs` (number): Tool execution duration in milliseconds

---

## Unity Core Tools

### 1. compile
- **Description**: Executes compilation after AssetDatabase.Refresh(). Returns compilation results with detailed timing information.
- **Parameters**: 
  - `ForceRecompile` (boolean): Whether to perform forced recompilation (default: false)
- **Response**: 
  - `Success` (boolean): Whether compilation was successful
  - `ErrorCount` (number): Total number of errors
  - `WarningCount` (number): Total number of warnings
  - `CompletedAt` (string): Compilation completion timestamp (ISO format)
  - `Errors` (array): Array of compilation errors (if any)
    - `Message` (string): Error message
    - `File` (string): File path where error occurred
    - `Line` (number): Line number where error occurred
  - `Warnings` (array): Array of compilation warnings (if any)
    - `Message` (string): Warning message
    - `File` (string): File path where warning occurred
    - `Line` (number): Line number where warning occurred
  - `Message` (string): Optional message for additional information

### 2. get-logs
- **Description**: Retrieves log information from Unity console with filtering and search capabilities
- **Parameters**: 
  - `LogType` (enum): Log type to filter - "Error", "Warning", "Log", "All" (default: "All")
  - `MaxCount` (number): Maximum number of logs to retrieve (default: 100)
  - `SearchText` (string): Text to search within log messages (retrieve all if empty) (default: "")
  - `IncludeStackTrace` (boolean): Whether to display stack traces (default: true)
- **Response**: 
  - `TotalCount` (number): Total number of logs available
  - `DisplayedCount` (number): Number of logs displayed in this response
  - `LogType` (string): Log type filter used
  - `MaxCount` (number): Maximum count limit used
  - `SearchText` (string): Search text filter used
  - `IncludeStackTrace` (boolean): Whether stack trace was included
  - `Logs` (array): Array of log entries
    - `Type` (string): Log type (Error, Warning, Log)
    - `Message` (string): Log message
    - `StackTrace` (string): Stack trace (if IncludeStackTrace is true)
    - `File` (string): File name where log occurred

### 3. run-tests
- **Description**: Executes Unity Test Runner and retrieves test results with comprehensive reporting
- **Parameters**: 
  - `FilterType` (enum): Type of test filter - "all", "fullclassname" (default: "all")
  - `FilterValue` (string): Filter value (specify when FilterType is other than all) (default: "")
    - `fullclassname`: Full class name (e.g.: io.github.hatayama.uMCP.CompileCommandTests)
  - `TestMode` (enum): Test mode - "EditMode", "PlayMode" (default: "EditMode")
    - ⚠️ **PlayMode Warning**: During PlayMode test execution, domain reload is temporarily disabled
  - `SaveXml` (boolean): Whether to save test results as XML file (default: false)
    - XML files are saved to `TestResults/` folder (project root)
    - **Recommendation**: Add `TestResults/` to `.gitignore` to exclude from version control
- **Response**: 
  - `Success` (boolean): Whether test execution was successful
  - `Message` (string): Test execution message
  - `CompletedAt` (string): Test execution completion timestamp (ISO format)
  - `TestCount` (number): Total number of tests executed
  - `PassedCount` (number): Number of passed tests
  - `FailedCount` (number): Number of failed tests
  - `SkippedCount` (number): Number of skipped tests
  - `XmlPath` (string): XML result file path (if SaveXml is true)

### 4. clear-console
- **Description**: Clears Unity console logs for clean development workflow
- **Parameters**: 
  - `AddConfirmationMessage` (boolean): Whether to add a confirmation log message after clearing (default: true)
- **Response**: 
  - `Success` (boolean): Whether the console clear operation was successful
  - `ClearedLogCount` (number): Number of logs that were cleared from the console
  - `ClearedCounts` (object): Breakdown of cleared logs by type
    - `ErrorCount` (number): Number of error logs that were cleared
    - `WarningCount` (number): Number of warning logs that were cleared
    - `LogCount` (number): Number of info logs that were cleared
  - `Message` (string): Message describing the clear operation result
  - `ErrorMessage` (string): Error message if the operation failed

### 5. find-game-objects
- **Description**: Find multiple GameObjects with advanced search criteria (component type, tag, layer, etc.)
- **Parameters**: 
  - `NamePattern` (string): GameObject name pattern to search for (default: "")
  - `SearchMode` (enum): Search mode - "Exact", "Path", "Regex", "Contains" (default: "Exact")
  - `RequiredComponents` (array): Array of component type names that GameObjects must have (default: [])
  - `Tag` (string): Tag filter (default: "")
  - `Layer` (number): Layer filter (default: null)
  - `IncludeInactive` (boolean): Whether to include inactive GameObjects (default: false)
  - `MaxResults` (number): Maximum number of results to return (default: 20)
  - `IncludeInheritedProperties` (boolean): Whether to include inherited properties (default: false)
- **Response**: 
  - `results` (array): Array of found GameObjects
    - `name` (string): GameObject name
    - `path` (string): Full hierarchy path
    - `isActive` (boolean): Whether the GameObject is active
    - `tag` (string): GameObject tag
    - `layer` (number): GameObject layer
    - `components` (array): Array of components on the GameObject
      - `TypeName` (string): Component type name
      - `AssemblyQualifiedName` (string): Full assembly qualified name
      - `Properties` (object): Component properties (if IncludeInheritedProperties is true)
  - `totalFound` (number): Total number of GameObjects found
  - `errorMessage` (string): Error message if search failed

---

## Unity Search & Discovery Tools

### 6. unity-search
- **Description**: Search Unity project using Unity Search API with comprehensive filtering and export options
- **Parameters**: 
  - `SearchQuery` (string): Search query string (supports Unity Search syntax) (default: "")
    - Examples: "*.cs", "t:Texture2D", "ref:MyScript", "p:MyPackage"
  - `Providers` (array): Specific search providers to use (empty = all active providers) (default: [])
    - Common providers: "asset", "scene", "menu", "settings", "packages"
  - `MaxResults` (number): Maximum number of search results to return (default: 50)
  - `IncludeDescription` (boolean): Whether to include detailed descriptions in results (default: true)
  - `IncludeMetadata` (boolean): Whether to include file metadata (size, modified date) (default: false)
  - `SearchFlags` (enum): Search flags for controlling Unity Search behavior (default: "Default")
  - `SaveToFile` (boolean): Whether to save search results to external file (default: false)
  - `OutputFormat` (enum): Output file format when SaveToFile is enabled - "JSON", "CSV", "TSV" (default: "JSON")
  - `AutoSaveThreshold` (number): Threshold for automatic file saving (default: 100)
  - `FileExtensions` (array): Filter results by file extension (e.g., "cs", "prefab", "mat") (default: [])
  - `AssetTypes` (array): Filter results by asset type (e.g., "Texture2D", "GameObject", "MonoScript") (default: [])
  - `PathFilter` (string): Filter results by path pattern (supports wildcards) (default: "")
- **Response**: 
  - `Results` (array): Array of search result items (empty if results were saved to file)
  - `TotalCount` (number): Total number of search results found
  - `DisplayedCount` (number): Number of results displayed in this response
  - `SearchQuery` (string): Search query that was executed
  - `ProvidersUsed` (array): Search providers that were used for the search
  - `SearchDurationMs` (number): Search duration in milliseconds
  - `Success` (boolean): Whether the search was completed successfully
  - `ErrorMessage` (string): Error message if search failed
  - `ResultsFilePath` (string): Path to saved search results file (when SaveToFile is enabled)
  - `ResultsSavedToFile` (boolean): Whether results were saved to file
  - `SavedFileFormat` (string): File format of saved results
  - `SaveToFileReason` (string): Reason why results were saved to file

### 7. get-hierarchy
- **Description**: Get Unity Hierarchy structure in AI-friendly format
- **Parameters**: 
  - `IncludeInactive` (boolean): Whether to include inactive GameObjects (default: true)
  - `MaxDepth` (number): Maximum depth to traverse (-1 for unlimited) (default: -1)
  - `RootPath` (string): Starting root path (null for all root objects) (default: null)
  - `IncludeComponents` (boolean): Whether to include component information (default: true)
- **Response**: 
  - `Success` (boolean): Whether the operation was successful
  - `Hierarchy` (object): Hierarchical structure of GameObjects
    - `RootObjects` (array): Array of root level GameObjects
      - `Name` (string): GameObject name
      - `Path` (string): Full hierarchy path
      - `IsActive` (boolean): Whether the GameObject is active
      - `Tag` (string): GameObject tag
      - `Layer` (number): GameObject layer
      - `LayerName` (string): GameObject layer name
      - `Components` (array): Array of component type names (if IncludeComponents is true)
      - `Children` (array): Recursive array of child GameObjects with same structure
  - `TotalGameObjectCount` (number): Total number of GameObjects in the hierarchy
  - `MaxDepthReached` (number): The actual maximum depth reached during traversal
  - `Message` (string): Operation message
  - `ErrorMessage` (string): Error message if operation failed

### 8. get-provider-details
- **Description**: Get detailed information about Unity Search providers including display names, descriptions, active status, and capabilities
- **Parameters**: 
  - `ProviderId` (string): Specific provider ID to get details for (empty = all providers) (default: "")
    - Examples: "asset", "scene", "menu", "settings"
  - `ActiveOnly` (boolean): Whether to include only active providers (default: false)
  - `SortByPriority` (boolean): Sort providers by priority (lower number = higher priority) (default: true)
  - `IncludeDescriptions` (boolean): Include detailed descriptions for each provider (default: true)
- **Response**: 
  - `Providers` (array): Array of provider information
  - `TotalCount` (number): Total number of providers found
  - `ActiveCount` (number): Number of active providers
  - `InactiveCount` (number): Number of inactive providers
  - `Success` (boolean): Whether the request was successful
  - `ErrorMessage` (string): Error message if request failed
  - `AppliedFilter` (string): Filter applied (specific provider ID or "all")
  - `SortedByPriority` (boolean): Whether results are sorted by priority

### 9. get-menu-items
- **Description**: Retrieve Unity MenuItems with detailed metadata for programmatic execution. Unlike Unity Search menu provider, this provides implementation details (method names, assemblies, execution compatibility) needed for automation and debugging
- **Parameters**: 
  - `FilterText` (string): Text to filter MenuItem paths (empty for all items) (default: "")
  - `FilterType` (enum): Type of filter to apply - "contains", "exact", "startswith" (default: "contains")
  - `IncludeValidation` (boolean): Include validation functions in the results (default: false)
  - `MaxCount` (number): Maximum number of menu items to retrieve (default: 200)
- **Response**: 
  - `MenuItems` (array): List of discovered MenuItems matching the filter criteria
    - `Path` (string): MenuItem path
    - `MethodName` (string): Execution method name
    - `TypeName` (string): Implementation class name
    - `AssemblyName` (string): Assembly name
    - `Priority` (number): Menu item priority
    - `IsValidateFunction` (boolean): Whether it's a validation function
  - `TotalCount` (number): Total number of MenuItems discovered before filtering
  - `FilteredCount` (number): Number of MenuItems returned after filtering
  - `AppliedFilter` (string): The filter text that was applied
  - `AppliedFilterType` (string): The filter type that was applied

### 10. execute-menu-item
- **Description**: Execute Unity MenuItem by path
- **Parameters**: 
  - `MenuItemPath` (string): The menu item path to execute (e.g., "GameObject/Create Empty") (default: "")
  - `UseReflectionFallback` (boolean): Whether to use reflection as fallback if EditorApplication.ExecuteMenuItem fails (default: true)
- **Response**: 
  - `MenuItemPath` (string): The menu item path that was executed
  - `Success` (boolean): Whether the execution was successful
  - `ExecutionMethod` (string): The execution method used (EditorApplication or Reflection)
  - `ErrorMessage` (string): Error message if execution failed
  - `Details` (string): Additional information about the execution
  - `MenuItemFound` (boolean): Whether the menu item was found in the system

---

## Related Documentation

- [Main README](README_ja.md) - Project overview and setup
- [Architecture Documentation](Editor/ARCHITECTURE.md) - Technical architecture details
- [TypeScript Server Architecture](TypeScriptServer~/ARCHITECTURE.md) - TypeScript server implementation
- [Changelog](CHANGELOG.md) - Version history and updates