[日本語](FEATURES_ja.md)

# uMCP Features

This document provides detailed information about all Unity MCP (Model Context Protocol) commands and features.

## 📋 Common Parameters & Response Format

All Unity MCP commands share the following common elements:

### Common Parameters
- `TimeoutSeconds` (number): Command execution timeout in seconds (default: 10 seconds)

### Common Response Properties
All commands automatically include the following timing information:
- `StartedAt` (string): Command execution start time (local time)
- `EndedAt` (string): Command execution end time (local time)  
- `ExecutionTimeMs` (number): Command execution duration in milliseconds

---

## 🛠️ Core Unity Commands

### 1. compile
- **Description**: Executes Unity project compilation with detailed timing information after AssetDatabase.Refresh()
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

### 2. getlogs
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

### 3. runtests
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

### 4. clearconsole
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

---

## 🔍 Unity Search & Discovery Commands

### 5. unitysearch
- **Description**: Search Unity project using Unity Search API with comprehensive filtering and export options
- **Parameters**: 
  - `SearchQuery` (string): Search query string (supports Unity Search syntax) (default: "")
    - Examples: "*.cs", "t:Texture2D", "ref:MyScript", "p:MyPackage"
  - `Providers` (array): Specific search providers to use (empty = all active providers) (default: [])
    - Common providers: "asset", "scene", "menu", "settings", "packages"
  - `MaxResults` (number): Maximum number of search results to return (default: 50)
  - `IncludeDescription` (boolean): Whether to include detailed descriptions in results (default: true)
  - `IncludeThumbnails` (boolean): Whether to include thumbnail/preview information (default: false)
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

### 6. getproviderdetails
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

### 7. getmenuitems
- **Description**: Retrieve Unity MenuItems with detailed metadata for programmatic execution. Unlike Unity Search menu provider, this provides implementation details (method names, assemblies, execution compatibility) needed for automation and debugging
- **Parameters**: 
  - `FilterText` (string): Text to filter MenuItem paths (empty for all items) (default: "")
  - `FilterType` (enum): Type of filter to apply - "contains", "exact", "startswith" (default: "contains")
  - `IncludeValidation` (boolean): Include validation functions in the results (default: false)
  - `MaxCount` (number): Maximum number of menu items to retrieve (default: 200)
- **Response**: 
  - `MenuItems` (array): List of discovered MenuItems matching the filter criteria
  - `TotalCount` (number): Total number of MenuItems discovered before filtering
  - `FilteredCount` (number): Number of MenuItems returned after filtering
  - `AppliedFilter` (string): The filter text that was applied
  - `AppliedFilterType` (string): The filter type that was applied

### 8. executemenuitem
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

## ⚡ Advanced Features

### Type-Safe Parameter System
- All commands use strongly typed parameter schemas with automatic validation
- Enum parameters provide predefined value options for better user experience
- Optional parameters have default values automatically applied
- Comprehensive parameter descriptions guide proper usage

### BaseCommandResponse System
- **Automatic Timing Measurement**: All commands automatically measure and report execution time
- **Consistent Response Format**: All responses include standardized timing information
- **Local Time Display**: Timestamps converted to local time for better readability
- **Performance Monitoring**: Execution times help identify performance bottlenecks

### Dynamic Timeout Configuration
- **Per-Command Timeout**: Each command supports individual timeout settings via `TimeoutSeconds` parameter
- **Intelligent Defaults**: Reasonable default timeouts based on command complexity (ping: 5 seconds, tests: 30 seconds)
- **Buffer Management**: TypeScript client adds 10-second buffer to ensure Unity-side timeout triggers first
- **Timeout Handling**: Proper timeout responses with detailed error information

### Real-Time Tool Discovery
- **Event-Driven Updates**: Unity command changes automatically detected and propagated to LLM tools
- **Dynamic Tool Registration**: New custom commands appear in LLM tools without server restart
- **Domain Reload Recovery**: Automatic reconnection and tool synchronization after Unity compilation

### File Export System
- **Large Result Management**: Automatic file export for large search results to avoid token consumption
- **Multiple Formats**: Support for JSON, CSV, and TSV export formats
- **Automatic Cleanup**: Old export files automatically cleaned up to prevent disk space issues
- **Threshold-Based Export**: Configurable thresholds for automatic file saving

---

## 🔧 Custom Command Development

The uMCP system supports **dynamic custom command registration** that allows developers to add their own commands without modifying the core package. There are **two ways** to register custom commands:

### Method 1: Automatic Registration with [McpTool] Attribute (Recommended)

This is the **easiest and recommended method**. Commands are automatically discovered and registered when Unity compiles.

**Step 1: Create a Schema Class** (defines parameters):
```csharp
using System.ComponentModel;

public class MyCustomSchema : BaseCommandSchema
{
    [Description("Your parameter description")]
    public string MyParameter { get; set; } = "default_value";
    
    [Description("Example enum parameter")]
    public MyEnum EnumParameter { get; set; } = MyEnum.Option1;
}

public enum MyEnum
{
    Option1,
    Option2,
    Option3
}
```

**Step 2: Create a Response Class** (defines return data):
```csharp
public class MyCustomResponse : BaseCommandResponse
{
    public string Result { get; set; }
    public bool Success { get; set; }
    
    public MyCustomResponse(string result, bool success)
    {
        Result = result;
        Success = success;
    }
    
    // Required parameterless constructor
    public MyCustomResponse() { }
}
```

**Step 3: Create the Command Class**:
```csharp
[McpTool]  // ← This attribute enables automatic registration!
public class MyCustomCommand : AbstractUnityCommand<MyCustomSchema, MyCustomResponse>
{
    public override string CommandName => "myCustomCommand";
    public override string Description => "My custom command description";
    
    // Executed on main thread
    protected override Task<MyCustomResponse> ExecuteAsync(MyCustomSchema parameters)
    {
        // Type-safe parameter access
        string param = parameters.MyParameter;
        MyEnum enumValue = parameters.EnumParameter;
        
        // Implement custom logic here
        string result = ProcessCustomLogic(param, enumValue);
        bool success = !string.IsNullOrEmpty(result);
        
        return Task.FromResult(new MyCustomResponse(result, success));
    }
    
    private string ProcessCustomLogic(string input, MyEnum enumValue)
    {
        // Implement custom logic
        return $"Processed '{input}' with enum '{enumValue}'";
    }
}
```

### Method 2: Manual Registration via CustomCommandManager

This method gives you **complete control** over when commands are registered/unregistered.

**Steps 1-2: Schema and Response Classes** (same as Method 1, but without `[McpTool]` attribute)

**Step 3: Command Class** (without `[McpTool]` attribute):
```csharp
// No [McpTool] attribute for manual registration
public class MyManualCommand : AbstractUnityCommand<MyCustomSchema, MyCustomResponse>
{
    public override string CommandName => "myManualCommand";
    public override string Description => "Manually registered custom command";
    
    protected override Task<MyCustomResponse> ExecuteAsync(MyCustomSchema parameters)
    {
        // Same implementation as Method 1
        string result = ProcessCustomLogic(parameters.MyParameter, parameters.EnumParameter);
        return Task.FromResult(new MyCustomResponse(result, true));
    }
}
```

**Step 4: Manual Registration**:
```csharp
using UnityEngine;
using UnityEditor;

public static class MyCommandRegistration
{
    // Register command via Unity menu
    [MenuItem("MyProject/Register Custom Commands")]
    public static void RegisterMyCommands()
    {
        CustomCommandManager.RegisterCustomCommand(new MyManualCommand());
        Debug.Log("Custom command registered!");
        
        // Optional: Manually notify LLM tools of changes
        CustomCommandManager.NotifyCommandChanges();
    }
    
    // Unregister command via Unity menu  
    [MenuItem("MyProject/Unregister Custom Commands")]
    public static void UnregisterMyCommands()
    {
        CustomCommandManager.UnregisterCustomCommand("myManualCommand");
        Debug.Log("Custom command unregistered!");
    }
}
```

### Debugging Custom Commands

```csharp
// Display all registered commands
[MenuItem("uMCP/Debug/Show Registered Commands")]
public static void ShowCommands()
{
    CommandInfo[] commands = CustomCommandManager.GetRegisteredCustomCommands();
    foreach (var cmd in commands)
    {
        Debug.Log($"Command: {cmd.Name} - {cmd.Description}");
    }
}
```

---

## 📚 Related Documentation

- [Main README](README.md) - Project overview and setup
- [Architecture Documentation](Editor/ARCHITECTURE.md) - Technical architecture details
- [TypeScript Server Architecture](.TypeScriptServer/ARCHITECTURE.md) - TypeScript server implementation
- [Changelog](CHANGELOG.md) - Version history and updates 