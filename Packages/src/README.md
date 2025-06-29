[日本語](./Packages/src/README_ja.md)

[![Unity](https://img.shields.io/badge/Unity-2022.3+-red.svg)](https://unity3d.com/)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE.md)
[![Ask DeepWiki](https://deepwiki.com/badge.svg)](https://deepwiki.com/hatayama/uMCP)

# uMCP

Connect Unity Editor to LLM tools using Model Context Protocol.

# Concept

When AI is coding, humans need to handle Unity compilation and log retrieval tasks. uMCP was created with the concept of minimizing this human intervention as much as possible.
With uMCP, AI can run autonomously for extended periods without relying on human operations.

## ✨ Features

uMCP provides 8 Unity MCP commands offering comprehensive functionality including compilation, log retrieval, test execution, Unity Search, and MenuItems operations.

### Key Features
Currently, the following 8 commands are available.

**Core Unity Commands**:
- Refresh & Compilation (compile) - Returns compilation results
- Log retrieval (getlogs) - Features filtering by log type, text search, and count limits
- Test execution (runtests) - Exports test results to XML and returns the file path
- Console clearing (clearconsole)

**Unity Search & Discovery**:
- Unity Search API execution (unitysearch)
- Unity Search provider details retrieval (getproviderdetails)
- MenuItems retrieval (getmenuitems)
- MenuItems execution (executemenuitem) - Convenient for AI to create and execute test code

**📖 For detailed feature specifications, see [FEATURES.md](FEATURES.md)**

**Advanced Features**:
- Type-safe parameters
- Automatic timing measurement
- Dynamic timeout
- Real-time tool discovery
- File export system

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
        "[Unity Package Path]/.TypeScriptServer/dist/server.bundle.js"
      ],
      "env": {
        "UNITY_TCP_PORT": "{port}"
      }
    }
  }
}
```

**Path examples**:
- **Via Package Manager**: `"/Users/username/UnityProject/Library/PackageCache/io.github.hatayama.umcp@[hash]/.TypeScriptServer/dist/server.bundle.js"`
> **Note**: When installed via Package Manager, the package is placed in `Library/PackageCache` with a hashed directory name. Using the "Auto Configure Settings" button will automatically set the correct path.

5. Support for multiple Unity instances
  - Supports multiple Unity instances by changing port numbers

## Prerequisites

⚠️ **Important**: The following software is required
- **Unity 2022.3 or higher**
- **Node.js 18.0 or higher** ⭐ **Required** - Necessary for MCP Server execution
- Install Node.js from [here](https://nodejs.org/en/download)

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

3. Open the Package Manager window and navigate to the hutayama page in the My Registries section to go to Project Settings

## Troubleshooting

### Unity Connection Errors
- Ensure Unity MCP Bridge is running (Window > Unity MCP)
- Verify the configured port is not being used by other applications

### Cursor Configuration Errors
- Verify the path in `.cursor/mcp.json` is correct
- Ensure JSON format is valid
- Check if it's recognized in Cursor's Tools & Integrations > MCP Tools. If "0 tool enable" or red circles appear, restart Cursor

## 🔧 Custom Command Development

uMCP supports dynamic custom command registration. Developers can add their own commands without modifying the core package.

**📖 For detailed custom command development, see [FEATURES.md](FEATURES.md#custom-command-development)**

### Development Methods
- **Method 1**: Automatic registration with [McpTool] attribute (Recommended)
- **Method 2**: Manual registration with CustomCommandManager
- **Debugging**: Command registration verification features

## License
MIT License
