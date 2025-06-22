[日本語](README_ja.md)

[![Unity](https://img.shields.io/badge/Unity-2022.3+-red.svg)](https://unity3d.com/)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE.md)

# uMCP

Connects Unity Editor to LLM tools using Model Context Protocol.  
It has an automatic connection function to `Cursor` and `Claude Code`.  
This enables you to call the following functions:  

## ✨ Features

### 📋 Common Parameters & Response Format

All Unity MCP commands share the following common elements:

#### Common Parameters
- `TimeoutSeconds` (number): Timeout for command execution in seconds (default: 300 seconds = 5 minutes)

#### Common Response Properties
All commands automatically include the following timing information:
- `StartedAt` (string): Command execution start time (local time)
- `EndedAt` (string): Command execution end time (local time)  
- `ExecutionTimeMs` (number): Command execution duration in milliseconds

---

### 1. unity.compile
- **Description**: After executing AssetDatabase.Refresh(), compile. Return the compilation results with detailed timing information.
- **Parameters**: 
  - `ForceRecompile` (boolean): Whether to perform forced recompilation (default: false)
- **Response**: 
  - `Success` (boolean): Whether compilation was successful
  - `ErrorCount` (number): Total number of errors
  - `WarningCount` (number): Total number of warnings
  - `CompletedAt` (string): Compilation completion time (ISO format)
  - `Errors` (array): Array of compilation errors (if any)
    - `Message` (string): Error message
    - `File` (string): File path where error occurred
    - `Line` (number): Line number where error occurred
  - `Warnings` (array): Array of compilation warnings (if any)
    - `Message` (string): Warning message
    - `File` (string): File path where warning occurred
    - `Line` (number): Line number where error occurred
  - `Message` (string): Optional message for additional information

### 2. unity.getLogs
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

### 3. unity.runTests
- **Description**: Executes Unity Test Runner and retrieves test results with comprehensive reporting
- **Parameters**: 
  - `FilterType` (enum): Type of test filter - "all", "fullclassname", "namespace", "testname", "assembly" (default: "all")
  - `FilterValue` (string): Filter value (specify when FilterType is other than all) (default: "")
    - `fullclassname`: Full class name (e.g.: io.github.hatayama.uMCP.CompileCommandTests)
    - `namespace`: Namespace (e.g.: io.github.hatayama.uMCP)
    - `testname`: Individual test name
    - `assembly`: Assembly name
  - `SaveXml` (boolean): Whether to save test results as XML file (default: false)
- **Response**: 
  - `Success` (boolean): Whether test execution was successful
  - `Message` (string): Test execution message
  - `CompletedAt` (string): Test execution completion timestamp (ISO format)
  - `TestCount` (number): Total number of tests executed
  - `PassedCount` (number): Number of passed tests
  - `FailedCount` (number): Number of failed tests
  - `SkippedCount` (number): Number of skipped tests
  - `XmlPath` (string): Path to XML result file (if SaveXml is true)
 
### 4. unity.ping
- **Description**: Ping test to Unity side
- **Parameters**: 
  - `Message` (string): Message to send to Unity side (default: "Hello from TypeScript MCP Server")
- **Response**: 
  - `Message` (string): Response message from Unity side
- **Note**:
  - Provides detailed execution timing for performance monitoring
  - Supports dynamic timeout configuration
  - Displays formatted response with connection information

### ⚡ Advanced Features

#### Type-Safe Parameter System
- All commands use strongly-typed parameter schemas with automatic validation
- Enum parameters provide predefined value options for better user experience
- Default values are automatically applied for optional parameters
- Comprehensive parameter descriptions guide proper usage

#### BaseCommandResponse System
- **Automatic Timing Measurement**: All commands automatically measure and report execution time
- **Consistent Response Format**: All responses include standardized timing information
- **Local Time Display**: Timestamps are converted to local time for better readability
- **Performance Monitoring**: Execution time helps identify performance bottlenecks

#### Dynamic Timeout Configuration
- **Per-Command Timeout**: Each command supports individual timeout configuration via `TimeoutSeconds` parameter
- **Intelligent Defaults**: Sensible default timeouts based on command complexity (5s for ping, 5min for tests)
- **Buffer Management**: TypeScript client adds 10-second buffer to ensure Unity-side timeout triggers first
- **Timeout Handling**: Graceful timeout responses with detailed error information

#### Real-Time Tool Discovery
- **Event-Driven Updates**: Unity command changes are automatically detected and propagated to LLM tools
- **Dynamic Tool Registration**: New custom commands appear in LLM tools without server restart
- **Domain Reload Recovery**: Automatic reconnection and tool synchronization after Unity compilation

## Usage
1. Select Window > uMCP. A dedicated window will open. Press the "Start Server" button.
<img width="400" alt="image" src="https://github.com/user-attachments/assets/0a1b5ed4-56a9-4209-b2e7-0acbca3cb9a9" />


If the display changes as shown below, it's successful.


<img width="400" alt="image" src="https://github.com/user-attachments/assets/9f5d8294-2cde-4d30-ab22-f527e6c3bf66" />



2. Next, select the target IDE in the LLM Tool Settings section. Press the "Auto Configure Settings" button to automatically connect to the IDE.

<img width="400" alt="image" src="https://github.com/user-attachments/assets/379fe674-dee7-4962-9d93-6f43fca13227" />


3. IDE connection verification
  - For example, in Cursor, check Tools & Integrations on the settings page and find unity-mcp-{port number}. Click the toggle to enable MCP. If yellow or red circles appear, restart Cursor.
<img width="657" alt="image" src="https://github.com/user-attachments/assets/14352ec0-c0a4-443d-98d5-35a6c86acd45" />

4. Manual configuration (usually not required)
If necessary, you can manually edit Cursor's configuration file (`.cursor/mcp.json`):

```json
{
  "mcpServers": {
    "uMcp-{port}": {
      "command": "node",
      "args": [
        "[Unity Package Path]/TypeScriptServer/dist/server.bundle.js"
      ],
      "env": {
        "UNITY_TCP_PORT": "{port}"
      }
    }
  }
}
```

**Path examples**:
- **Via Package Manager**: `"/Users/username/UnityProject/Library/PackageCache/io.github.hatayama.umpc@[hash]/TypeScriptServer/dist/server.bundle.js"`
> **Note**: When installed via Package Manager, the package is placed in `Library/PackageCache` with a hashed directory name. Using the "Auto Configure Cursor" button will automatically set the correct path.

5. Support for multiple Unity instances
  - Supports multiple Unity instances by changing port numbers

## Prerequisites

⚠️ **Important**: The following software is required
- **Unity 2022.3 or higher**
- **Node.js 18.0 or higher** ⭐ **Required** - Necessary for MCP Server execution
- Install node.js from [here](https://nodejs.org/en/download)


## Installation

### Via Unity Package Manager

1. Open the Unity Editor
2. Open Window > Package Manager
3. Click the "+" button
4. Select "Add package from git URL"
5. Enter the following URL:
```
https://github.com/hatayama/uMCP.git?path=/Packages/src
```

### Via OpenUPM (Recommended)

### Using Scoped registry with Unity Package Manager
1. Open the Project Settings window and navigate to the Package Manager page
2. Add the following entry to the Scoped Registries list:
```
Name: OpenUPM
URL: https://package.openupm.com
Scope(s): io.github.hatayama.umcp
```

3. Open the Package Manager window and navigate to the "hatayama" page in the My Registries section

### Unity connection errors
- Verify that Unity MCP Bridge is running (Window > Unity MCP)
- Check that the configured port is not being used by other applications

### Cursor configuration errors
- Verify that the path in `.cursor/mcp.json` is correct
- Check that JSON format is correct
- Check if it's recognized in Cursor's Tools & Integrations > MCP Tools. If "0 tool enable" or red circle is displayed, restart Cursor

## License
MIT License

[Currently only the above built-in functions are available, but we are considering a feature that allows freely adding commands outside the package in the future](https://github.com/hatayama/uMCP/issues/14)

### 🔧 Custom Command Development

The uMCP system supports **dynamic custom command registration** that allows developers to add their own commands without modifying the core package. There are **two ways** to register custom commands:

#### Method 1: Automatic Registration with [McpTool] Attribute (Recommended)

This is the **easiest and recommended method**. Commands are automatically discovered and registered when Unity compiles.

**Step 1: Create a Schema Class** (defines parameters):
```csharp
using System.ComponentModel;

public class MyCustomSchema : BaseCommandSchema
{
    [Description("Your parameter description")]
    public string MyParameter { get; set; } = "default_value";
    
    [Description("Select operation type")]
    public MyOperationType OperationType { get; set; } = MyOperationType.Process;
}

public enum MyOperationType
{
    Process,
    Validate,
    Export
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
    
    // Executed on the main thread
    protected override Task<MyCustomResponse> ExecuteAsync(MyCustomSchema parameters)
    {
        // Type-safe parameter access
        string param = parameters.MyParameter;
        MyOperationType operation = parameters.OperationType;
        
        // Your custom logic here
        string result = ProcessCustomLogic(param, operation);
        bool success = !string.IsNullOrEmpty(result);
        
        return Task.FromResult(new MyCustomResponse(result, success));
    }
    
    private string ProcessCustomLogic(string input, MyOperationType operation)
    {
        // Implement your custom logic
        return $"Processed '{input}' with operation '{operation}'";
    }
}
```

---

#### Method 2: Manual Registration with CustomCommandManager

This method gives you **full control** over when commands are registered/unregistered.

**Step 1-2: Create Schema and Response classes** (same as Method 1, but **without** `[McpTool]` attribute)

**Step 3: Create the Command Class** (without `[McpTool]` attribute):
```csharp
// No [McpTool] attribute for manual registration
public class MyManualCommand : AbstractUnityCommand<MyCustomSchema, MyCustomResponse>
{
    public override string CommandName => "myManualCommand";
    public override string Description => "Manually registered custom command";
    
    protected override Task<MyCustomResponse> ExecuteAsync(MyCustomSchema parameters)
    {
        // Implementation same as Method 1
        string result = ProcessCustomLogic(parameters.MyParameter, parameters.OperationType);
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
    // Register commands via Unity menu
    [MenuItem("MyProject/Register Custom Commands")]
    public static void RegisterMyCommands()
    {
        CustomCommandManager.RegisterCustomCommand(new MyManualCommand());
        Debug.Log("Custom commands registered!");
        
        // Optional: Manually notify LLM tools about changes
        CustomCommandManager.NotifyCommandChanges();
    }
    
    // Unregister commands via Unity menu  
    [MenuItem("MyProject/Unregister Custom Commands")]
    public static void UnregisterMyCommands()
    {
        CustomCommandManager.UnregisterCustomCommand("myManualCommand");
        Debug.Log("Custom commands unregistered!");
    }
    
    // Alternative: Automatic registration on Unity startup
    // [InitializeOnLoad]
    // static MyCommandRegistration()
    // {
    //     RegisterMyCommands();
    // }
}
```

#### 🔧 Debugging Custom Commands

```csharp
// View all registered commands
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

## License
MIT License
