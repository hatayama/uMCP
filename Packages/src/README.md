[![Unity](https://img.shields.io/badge/Unity-2022.3+-red.svg)](https://unity3d.com/)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE.md)

# uMCP

Connects Unity Editor to LLM tools using Model Context Protocol.  
It has an automatic connection function to `Cursor` and `Claude Code`.  
This enables you to call the following functions:  

## ✨ Features

### 1. unity.compile
- **Description**: After executing AssetDatabase.Refresh(), compile. Return the compilation results.
- **Parameters**: 
  - `forceRecompile` (boolean): Whether to force recompilation (default: false)
- **Response**: 
  - `success` (boolean): Whether compilation was successful
  - `errorCount` (number): Total number of errors
  - `warningCount` (number): Total number of warnings
  - `completedAt` (string): Compilation completion time (ISO format)
  - `errors` (array): Array of compilation errors (if any)
    - `message` (string): Error message
    - `file` (string): File path where error occurred
    - `line` (number): Line number where error occurred
    - `column` (number): Column number where error occurred
    - `type` (string): Error type
  - `warnings` (array): Array of compilation warnings (if any)
    - `message` (string): Warning message
    - `file` (string): File path where warning occurred
    - `line` (number): Line number where warning occurred
    - `column` (number): Column number where warning occurred
    - `type` (string): Warning type

### 2. unity.getLogs
- **Description**: Retrieves log information from Unity console
- **Parameters**: 
  - `logType` (string): Log type to filter (Error, Warning, Log, All) (default: "All")
  - `maxCount` (number): Maximum number of logs to retrieve (default: 100)
  - `searchText` (string): Text to search within log messages (retrieve all if empty) (default: "")
  - `includeStackTrace` (boolean): Whether to display stack traces (default: true)
- **Response**: 
  - `logs` (array): Array of log entries
    - `type` (string): Log type (Error, Warning, Log)
    - `message` (string): Log message
    - `stackTrace` (string): Stack trace (if includeStackTrace is true)
    - `file` (string): File name where log occurred
    - `line` (number): Line number (currently always 0)
    - `timestamp` (string): Log timestamp
  - `totalCount` (number): Total number of retrieved logs
  - `requestedLogType` (string): Requested log type
  - `requestedMaxCount` (number): Requested maximum log count
  - `requestedSearchText` (string): Requested search text
  - `requestedIncludeStackTrace` (boolean): Requested stack trace display setting

### 3. unity.runTests
- **Description**: Executes Unity Test Runner and retrieves test results
- **Parameters**: 
  - `filterType` (string): Type of test filter (all, fullclassname, namespace, testname, assembly) (default: "all")
  - `filterValue` (string): Filter value (specify when filterType is other than all) (default: "")
    - `fullclassname`: Full class name (e.g.: io.github.hatayama.uMCP.CompileCommandTests)
    - `namespace`: Namespace (e.g.: io.github.hatayama.uMCP)
    - `testname`: Individual test name
    - `assembly`: Assembly name
  - `saveXml` (boolean): Whether to save test results as XML file (default: false)
- **Response**: 
  - `success` (boolean): Whether test execution was successful
  - `message` (string): Execution result message
  - `testResults` (object): Test result details
    - `passedCount` (number): Number of successful tests
    - `failedCount` (number): Number of failed tests
    - `skippedCount` (number): Number of skipped tests
    - `totalCount` (number): Total number of executed tests
    - `duration` (number): Test execution time (seconds)
    - `failedTests` (array): Details of failed tests
      - `testName` (string): Test name
      - `fullName` (string): Full name of test
      - `message` (string): Error message
      - `stackTrace` (string): Stack trace
      - `duration` (number): Test execution time (seconds)
  - `xmlPath` (string): XML file path (if saveXml is true)
  - `filterType` (string): Used filter type
  - `filterValue` (string): Used filter value
  - `saveXml` (boolean): XML file save setting
  - `completedAt` (string): Test completion time
 
### 4. unity.ping
- **Description**: Ping test to Unity side (TCP/IP communication verification)
- **Parameters**: 
  - `message` (string): Message to send to Unity side (default: "Hello from TypeScript MCP Server")
- **Response**: 
  - Response message from Unity side (string format)
- **Note**:
  - Current implementation does not determine communication success/failure or measure response time
  - Only the response string from Unity side is returned

[Currently only the above built-in functions are available, but we are considering a feature that allows freely adding commands outside the package in the future](https://github.com/hatayama/uMCP/issues/14)

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
    "unity-mcp-{設定したport}": {
      "command": "node",
      "args": [
        "[Unity Package Path]/TypeScriptServer/dist/server.bundle.js"
      ],
      "env": {
        "UNITY_TCP_PORT": "{設定したport}"
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


## インストール

### Unity Package Manager

1. Unity Editorを開く
2. Window > Package Manager を開く
3. "+" ボタンをクリック
4. "Add package from git URL" を選択
5. 以下のURLを入力：
```
https://github.com/hatayama/uMCP.git?path=/Packages/src
```

### OpenUPM経由 (推奨)

### Using Scoped registry with Unity Package Manager
1. Open the Project Settings window and navigate to the Package Manager page
2. Add the following entry to the Scoped Registries list:
```
Name：OpenUPM
URL: https://package.openupm.com
Scope(s)：io.github.hatayama.umcp
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

## Author
Masamichi Hatayama
