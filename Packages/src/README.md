[日本語](/Packages/src/README_ja.md)

[![Unity](https://img.shields.io/badge/Unity-2022.3+-red.svg)](https://unity3d.com/)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE.md)
[![Ask DeepWiki](https://deepwiki.com/badge.svg)](https://deepwiki.com/hatayama/uMCP)  
![ClaudeCode](https://img.shields.io/badge/Claude_Code-555?logo=claude)
![Cursor](https://img.shields.io/badge/Cursor-111?logo=Cursor)
![GitHubCopilot](https://img.shields.io/badge/GitHub_Copilot-111?logo=githubcopilot)
![Windsurf](https://img.shields.io/badge/Windsurf-111?logo=Windsurf)
![WSL2](https://img.shields.io/badge/WSL2-28b?logo=WSL2)

# uMCP

Control Unity Editor from various LLM tools.

# Concept

When AI is coding, humans need to handle Unity compilation and log retrieval. uMCP was created with the concept of minimizing this as much as possible.
With uMCP, AI can run autonomously for extended periods without relying on human operations.

### Key Features
#### 1. compile - Execute Compilation
Compiles after executing AssetDatabase.Refresh(). Can find errors and warnings that built-in linters cannot detect.  
Choose between differential compilation and forced full compilation.
```
→ Execute compile
→ Analyze error content
→ Auto-fix relevant files
→ Verify with compile again
```

#### 2. get-logs - Retrieve Console Logs
Retrieves the same log content as Unity's Console. Can filter by LogType and search text, with optional stack trace inclusion.
This allows retrieving logs while keeping context small.
```
→ get-logs (LogType: Error, SearchText: "NullReference")
→ Identify cause from stack trace
→ Fix relevant code
```

#### 3. run-tests - Execute TestRunner (PlayMode, EditMode supported)
Executes Unity Test Runner and retrieves test results. Configure conditions with FilterType and FilterValue.
- FilterType: all (all tests), fullclassname (full class name), etc.
- FilterValue: Value according to filter type (class name, namespace, etc.)  
Can output test results as XML. Returns the output path for AI to read.  
This is also designed to minimize context consumption.
```
→ run-tests (FilterType: fullclassname, FilterValue: "PlayerControllerTests")
→ Check failed tests
→ Fix implementation to pass tests
```
> [!WARNING]
> When executing PlayMode tests, Domain Reload is forcibly turned OFF. Note that static variables will not be reset.

#### 4. clear-console - Clear Logs
Can clear logs that become noise during log searches.
```
→ clear-console
→ Start new debug session
```

#### 5. unity-search - Project Search with UnitySearch
Use [UnitySearch](https://docs.unity3d.com/2022.3/Manual/search-overview.html).
```
→ unity-search (SearchQuery: "*.prefab")
→ List Prefabs matching specific conditions
→ Identify problematic Prefabs
```

#### 6. get-provider-details - Check UnitySearch Providers
Retrieves search providers offered by UnitySearch.
```
→ get-provider-details
→ Understand each provider's capabilities
→ Select optimal search method
```

#### 7. get-menu-items - Retrieve Menu Items
Retrieves menu items defined with [MenuItem("xxx")] attribute. Can filter by string.
```
→ Check available menu items
```

#### 8. execute-menu-item - Execute Menu Items
Executes menu items defined with [MenuItem("xxx")] attribute.
```
→ Have AI generate test programs
→ execute-menu-item (MenuItemPath: "Tools/xxx")
→ Execute generated test programs
→ Check results with get-logs
```

#### 9. find-game-objects - Search Scene Objects
Retrieves objects and examines component parameters.
```
→ find-game-objects (RequiredComponents: ["Camera"])
→ Investigate Camera component parameters
```

#### 10. get-hierarchy - Analyze Scene Structure
Retrieves currently active Hierarchy information. Works at runtime too.
```
→ get-hierarchy
→ Understand parent-child relationships between GameObjects
→ Discover and fix structural issues
```

> [!NOTE]
> By combining these commands, AI can complete complex tasks without human intervention.
> Particularly effective for repetitive tasks like error fixing and test execution.
 
For detailed features, see [FEATURES.md](FEATURES.md).

## Usage
1. Select Window > uMCP. A dedicated window will open, so press the "Start Server" button.  
<img width="335" alt="image" src="https://github.com/user-attachments/assets/4cfd7f26-7739-442d-bad9-b3f6d113a0d7" />

3. Next, select the target IDE in the LLM Tool Settings section. Press the yellow "Configure {LLM Tool Name}" button to automatically connect to the IDE.  
<img width="335" alt="image" src="https://github.com/user-attachments/assets/25f1f4f9-e3c8-40a5-a2f3-903f9ed5f45b" />

4. IDE Connection Verification
  - For example, in Cursor, check Tools & Integrations on the settings page and find uMCP. Click the toggle to enable MCP. If red circles appear, restart Cursor.  
<img width="545" alt="image" src="https://github.com/user-attachments/assets/ed54d051-b78a-4bb4-bb2f-7ab23ebc1840" />


4. Manual Configuration (Usually Not Required)
> [!NOTE]
> Auto-configuration is usually sufficient, but if needed, you can manually edit Cursor's configuration file (`.cursor/mcp.json`):

```json
{
  "mcpServers": {
    "uMcp": {
      "command": "node",
      "args": [
        "[Unity Package Path]/TypeScriptServer~/dist/server.bundle.js"
      ],
      "env": {
        "UNITY_TCP_PORT": "{port}"
      }
    }
  }
}
```

**Path examples**:
- **Via Package Manager**: `"/Users/username/UnityProject/Library/PackageCache/io.github.hatayama.umcp@[hash]/TypeScriptServer~/dist/server.bundle.js"`
> [!NOTE]
> When installed via Package Manager, the package is placed in `Library/PackageCache` with a hashed directory name. Using the "Auto Configure Cursor" button will automatically set the correct path.

5. Support for Multiple Unity Instances
> [!NOTE]
> Supports multiple Unity instances by changing port numbers. Assign different port numbers to each instance.

## Prerequisites

> [!WARNING]
> The following software is required:
> - **Unity 2022.3 or higher**
> - **Node.js 18.0 or higher** - Required for MCP server execution
> - Install Node.js from [here](https://nodejs.org/en/download)

## Installation

### Via Unity Package Manager

1. Open Unity Editor
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

3. Open the Package Manager window and navigate to the hutayama page in the My Registries section to go to Project Settings

### Unity Connection Errors
> [!CAUTION]
> If connection errors occur:
> - Ensure Unity MCP Bridge is running (Window > Unity MCP)
> - Verify the configured port is not being used by other applications

### Cursor Configuration Errors
> [!WARNING]
> Please check the following:
> - Verify the path in `.cursor/mcp.json` is correct
> - Ensure JSON format is valid
> - Check if it's recognized in Cursor's Tools & Integrations > MCP Tools. If "0 tool enable" or red circles appear, restart Cursor


## Custom Command Development
You can easily add project-specific commands without modifying the core package.

**Step 1: Create Schema Class** (Define parameters):
```csharp
using System.ComponentModel;

public class MyCustomSchema : BaseCommandSchema
{
    [Description("Parameter description")]
    public string MyParameter { get; set; } = "default_value";
    
    [Description("Enum parameter example")]
    public MyEnum EnumParameter { get; set; } = MyEnum.Option1;
}

public enum MyEnum
{
    Option1,
    Option2,
    Option3
}
```

**Step 2: Create Response Class** (Define return data):
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

**Step 3: Create Command Class**:
```csharp
[McpTool]  // ← This attribute enables automatic registration
public class MyCustomCommand : AbstractUnityCommand<MyCustomSchema, MyCustomResponse>
{
    public override string CommandName => "myCustomCommand";
    public override string Description => "Description of my custom command";
    
    // Executes on main thread
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

See [Custom Command Samples](/Assets/Editor/CustomCommandSamples) for reference.

## License
MIT License