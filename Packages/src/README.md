[日本語](/Packages/src/README_ja.md)

[![Unity](https://img.shields.io/badge/Unity-2022.3+-red.svg)](https://unity3d.com/)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE.md)
[![Ask DeepWiki](https://deepwiki.com/badge.svg)](https://deepwiki.com/hatayama/uMCP)  
![ClaudeCode](https://img.shields.io/badge/Claude_Code-555?logo=claude)
![Cursor](https://img.shields.io/badge/Cursor-111?logo=Cursor)
![GitHubCopilot](https://img.shields.io/badge/GitHub_Copilot-111?logo=githubcopilot)
![Windsurf](https://img.shields.io/badge/Windsurf-111?logo=Windsurf)
![WSL2](https://img.shields.io/badge/WSL2-28b?logo=WSL2)


<h1 align="center">
    <img width="500" alt="uMCP" src="https://github.com/user-attachments/assets/0b7c4fcf-af5f-4025-b0d3-e596897d41b7" />  
</h1>    

Control Unity Editor from various LLM tools.

# Concept

During AI-assisted coding with Unity, tasks like compilation and log retrieval typically require human intervention. uMCP was designed to minimize such manual operations as much as possible.
With uMCP, AI can run autonomously for extended periods without relying on human operations.

# Tool Window
<img width="350" alt="image" src="https://github.com/user-attachments/assets/5863b58b-7b48-48ae-9a40-c874ddc11488" />

- Manages and monitors server status
- Provides visibility into LLM tool connection status
- Enables easy connection to tools via the LLM tool settings button

# Key Features
#### 1. compile - Execute Compilation
Performs compilation after AssetDatabase.Refresh(). Can detect errors and warnings that built-in linters cannot find.  
You can choose between incremental compilation and forced full compilation.
```
→ Execute compile
→ Analyze error content
→ Automatically fix relevant files
→ Verify with compile again
```

#### 2. get-logs - Retrieve Logs Same as Unity Console
Filter by LogType or search target string. You can also choose whether to include stacktrace.
This allows you to retrieve logs while keeping the context small.
```
→ get-logs (LogType: Error, SearchText: "NullReference")
→ Identify cause from stacktrace
→ Fix relevant code
```

#### 3. run-tests - Execute TestRunner (PlayMode, EditMode supported)
Executes Unity Test Runner and retrieves test results. You can set conditions with FilterType and FilterValue.
- FilterType: all (all tests), fullclassname (full class name), etc.
- FilterValue: Value according to filter type (class name, namespace, etc.)  
Test results can be output as xml. The output path is returned so AI can read it.  
This is also a strategy to avoid consuming context.
```
→ run-tests (FilterType: fullclassname, FilterValue: "PlayerControllerTests")
→ Check failed tests
→ Fix implementation and pass tests
```
> [!WARNING]
> During PlayMode test execution, Domain Reload is forcibly turned OFF. Note that static variables will not be reset.

#### 4. clear-console - Log Cleanup
Clear logs that become noise during log searches.
```
→ clear-console
→ Start new debug session
```

#### 5. unity-search - Project Search with UnitySearch
You can use [UnitySearch](https://docs.unity3d.com/Manual/search-overview.html).
```
→ unity-search (SearchQuery: "*.prefab")
→ List prefabs matching specific conditions
→ Identify problematic prefabs
```

#### 6. get-provider-details - Check UnitySearch Search Providers
Retrieve search providers offered by UnitySearch.
```
→ get-provider-details
→ Understand each provider's capabilities
→ Choose optimal search method
```

#### 7. get-menu-items - Retrieve Menu Items
Retrieve menu items defined with [MenuItem("xxx")] attribute. Can filter by string specification.
```
→ Check available menu items
```

#### 8. execute-menu-item - Execute Menu Items
Execute menu items defined with [MenuItem("xxx")] attribute.
```
→ Have AI generate test program
→ execute-menu-item (MenuItemPath: "Tools/xxx")
→ Execute generated test program
→ Check results with get-logs
```

#### 9. find-game-objects - Search Scene Objects
Retrieve objects and examine component parameters.
```
→ find-game-objects (RequiredComponents: ["Camera"])
→ Investigate Camera component parameters
```

#### 10. get-hierarchy - Analyze Scene Structure
Retrieve information about the currently active Hierarchy. Works at runtime as well.
```
→ get-hierarchy
→ Understand parent-child relationships between GameObjects
→ Discover and fix structural issues
```

> [!NOTE]
> By combining these commands, AI can complete complex tasks without human intervention.
> They are particularly powerful for repetitive tasks like error fixing and test execution.

## Security Settings

> [!WARNING]
> **Features Disabled by Default**
> 
> The following features are disabled by default because they can execute arbitrary code freely:
> - `execute-menu-item`: Executing menu items
> - `run-tests`: Test execution
> 
> To use these features, you need to enable the corresponding settings in the Security Settings of the uMCP window:
> - **Allow Test Execution**: Enables the `run-tests` command
> - **Allow Menu Item Execution**: Enables the `execute-menu-item` command
> Only enable these features in trusted environments.

For detailed features, please see [FEATURES.md](FEATURES.md).

## Usage
1. Select Window > uMCP. A dedicated window will open, so press the "Start Server" button.  
<img width="335" alt="image" src="https://github.com/user-attachments/assets/4cfd7f26-7739-442d-bad9-b3f6d113a0d7" />

3. Next, select the target IDE in the LLM Tool Settings section. Press the yellow "Configure {LLM Tool Name}" button to automatically connect to the IDE.  
<img width="335" alt="image" src="https://github.com/user-attachments/assets/25f1f4f9-e3c8-40a5-a2f3-903f9ed5f45b" />

4. IDE Connection Verification
  - For example, with Cursor, check the Tools & Integrations in the settings page and find uMCP. Click the toggle to enable MCP. If a red circle appears, restart Cursor.  
<img width="545" alt="image" src="https://github.com/user-attachments/assets/ed54d051-b78a-4bb4-bb2f-7ab23ebc1840" />


4. Manual Setup (Usually Unnecessary)
> [!NOTE]
> Usually automatic setup is sufficient, but if needed, you can manually edit Cursor's configuration file (`.cursor/mcp.json`):

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

**Path Examples**:
- **Via Package Manager**: `"/Users/username/UnityProject/Library/PackageCache/io.github.hatayama.umcp@[hash]/TypeScriptServer~/dist/server.bundle.js"`
> [!NOTE]
> When installed via Package Manager, the package is placed in `Library/PackageCache` with a hashed directory name. Using the "Auto Configure Cursor" button will automatically set the correct path.

5. Multiple Unity Instance Support
> [!NOTE]
> Multiple Unity instances can be supported by changing port numbers. uMCP automatically assigns unused ports when starting up.

## Prerequisites

> [!WARNING]
> The following software is required:
> - **Unity 2022.3 or later**
> - **Node.js 18.0 or later** - Required for MCP server execution
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

### Using Scoped registry in Unity Package Manager
1. Open Project Settings window and go to Package Manager page
2. Add the following entry to the Scoped Registries list:
```
Name: OpenUPM
URL: https://package.openupm.com
Scope(s): io.github.hatayama.umcp
```

3. Open Package Manager window and select OpenUPM in the My Registries section. uMCP will be displayed.

## Custom Command Development
You can easily add project-specific commands without modifying the core package.

**Step 1: Create Schema Class** (define parameters):
```csharp
using System.ComponentModel;

public class MyCustomSchema : BaseCommandSchema
{
    [Description("Parameter description")]
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

**Step 2: Create Response Class** (define return data):
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
[McpTool]  // ← Auto-registered with this attribute
public class MyCustomCommand : AbstractUnityCommand<MyCustomSchema, MyCustomResponse>
{
    public override string CommandName => "myCustomCommand";
    public override string Description => "Description of my custom command";
    
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

Please also refer to [Custom Command Samples](/Assets/Editor/CustomCommandSamples).

## Automatic MCP Execution in Cursor
By default, Cursor requires user permission when executing MCP.
To disable this, go to Cursor Settings > Chat > MCP Tools Protection and turn it Off.
Note that this cannot be controlled per MCP type or tool, so all MCPs will no longer require permission. This is a security tradeoff, so please configure it with that in mind.

## WSL2 Support for Using Claude Code on Windows
Enable WSL2 mirror mode. Add the following to `C:/Users/[username]/.wslconfig`:
```
[wsl2]
networkingMode=mirrored
```
Then execute the following commands to apply the settings:
```bash
wsl --shutdown
wsl
```

## License
MIT License
