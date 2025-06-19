# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## About this Project

uMCP is a Unity package that connects Unity Editor to AI assistants using the Model Context Protocol (MCP). It allows AI tools like Cursor to execute Unity operations such as compilation, log retrieval, and test running through a bridge architecture.

## Development Commands

### Unity Package Development
- **Unity Version**: 2020.3+ required
- **Testing**: Open Unity project at root level, install package via Package Manager from `Packages/src`
- **Package Testing**: Window > uMCP to open the main interface

### TypeScript MCP Server Development
```bash
# Navigate to TypeScript server directory
cd Packages/src/TypeScriptServer

# Initial setup (only needed once or after package.json changes)
npm install

# Build the bundled server
npm run build

# Development with auto-rebuild on file changes
npm run dev:watch

# Run development server (includes ping tool)
npm run dev

# Production server (ping tool disabled)
npm start
```

### Testing Commands
```bash
# Test Unity-TypeScript communication
node test/test-compile.js                    # Basic compile test
node test/test-compile.js --force           # Force recompile
node test/test-logs.js                      # Log retrieval test
node test/test-unity-connection.js          # Full connection test
node test/test-all-logs.js --stats         # Log statistics

# Direct Unity communication (when Unity MCP server is running on port 7400)
echo '{"jsonrpc":"2.0","id":1,"method":"ping","params":{"message":"test"}}' | nc localhost 7400
```

## Architecture Overview

### Two-Layer Bridge Architecture
The system uses a dual-bridge approach to connect AI assistants to Unity:

1. **TypeScript MCP Server** (`Packages/src/TypeScriptServer/`)
   - Implements MCP protocol for AI assistant communication
   - Provides tools: `unity.ping`, `action.compileUnity`, `context.getUnityLogs`
   - Communicates with Unity via TCP/IP JSON-RPC

2. **Unity MCP Bridge** (`Packages/src/Editor/`)
   - Unity Editor package that receives commands from TypeScript server
   - Executes Unity operations and returns results
   - Manages persistent state through ScriptableSingleton classes

### ScriptableSingleton-Based State Management

**Key Design Decision**: The codebase has migrated from SessionState to ScriptableSingleton for all persistent data to handle Unity's Domain Reload transparently. This architectural change eliminates the complexity of managing state across compilation cycles.

**ScriptableSingleton Classes:**
- `McpServerData` - Server state and configuration
- `McpCompileData` - Compilation request tracking  
- `McpCommunicationLogData` - Communication logs and pending requests
- `McpEditorWindowData` - UI state and preferences

### Command Pattern Implementation

Unity commands use a registry-based Command Pattern located in `Packages/src/Editor/Api/Commands/`:

- `IUnityCommand` - Base interface for all commands
- `UnityCommandRegistry` - Dynamic command registration and execution
- Individual command classes: `CompileCommand`, `GetLogsCommand`, `PingCommand`, etc.
- All commands support `CancellationToken` for async operations

### Critical Technical Constraints

**Domain Reload Behavior**: Unity's Domain Reload destroys static variables and interrupts TCP connections. The ScriptableSingleton architecture automatically handles server state restoration, but long-running operations (like compilation) may experience connection timeouts on the client side.

**TCP Communication Lifecycle**: During Unity compilation, Domain Reload will terminate existing TCP connections. The server automatically restarts based on ScriptableSingleton state, but clients need to handle connection timeouts appropriately.

## Code Organization

### Unity Editor Structure
```
Packages/src/Editor/
├── Api/            # JSON-RPC processing and command handlers
├── Server/         # TCP server and controller logic  
├── Data/           # ScriptableSingleton persistence classes
├── Config/         # IDE configuration management (Cursor, Claude Code)
├── UI/             # Editor windows and communication logs
├── Tools/          # Development utilities and log fetching
├── Build/          # TypeScript server building utilities
└── Utils/          # Logging and helper utilities
```

### TypeScript Server Structure
```
Packages/src/TypeScriptServer/
├── src/
│   ├── tools/      # MCP tool implementations
│   ├── types/      # TypeScript type definitions
│   ├── server.ts   # Main MCP server
│   └── unity-client.ts # Unity communication client
├── test/           # Test scripts for Unity communication
└── dist/           # Built artifacts (server.bundle.js)
```

## Key Implementation Details

### Bundle Distribution
The TypeScript server uses esbuild to create a single `server.bundle.js` file containing all dependencies. This eliminates the need for users to run `npm install` when using the package.

### IDE Integration
The package automatically configures MCP settings for supported IDEs:
- **Cursor**: Creates/updates `.cursor/mcp.json`
- **Claude Code**: Planned support for Claude Code configuration
- Configuration handles both local development and Package Manager installation paths

### Development vs Production Modes
- **Development**: `npm run dev` enables ping tool for testing
- **Production**: `npm start` runs minimal tool set for end users
- Environment variable `ENABLE_PING_TOOL=true` can override production behavior

## Domain-Specific Considerations

### Unity Package Manager Compatibility
The codebase handles both local development (Packages/src) and Package Manager installation (Library/PackageCache/io.github.hatayama.umcp@hash) through dynamic path resolution in `UnityMcpPathResolver`.

### Async/Await in Unity
All Unity command operations use async/await with proper `CancellationToken` support. The `MainThreadSwitcher` utility ensures Unity API calls execute on the main thread when needed.

### Compilation Integration
The compilation system integrates with Unity's `CompilationPipeline` and uses `CompileChecker` for async compilation tracking. Domain Reload during compilation is handled gracefully through the ScriptableSingleton persistence layer.

## Testing Strategy

### Automated Testing
- Unity: Edit Mode tests in `Assets/Tests/Editor/`
- TypeScript: Test scripts in `test/` directory for Unity communication validation

### Integration Testing
The test scripts provide comprehensive integration testing between TypeScript and Unity layers, validating the full communication pipeline including Domain Reload scenarios.

## Known Issues & Limitations

1. **Compilation Timeout**: Long-running compile operations may timeout on the client side due to Domain Reload breaking TCP connections, even though compilation completes successfully on the Unity side.

2. **ThreadAbortException**: Occasional thread abort exceptions occur during Unity Domain Reload, but these are handled gracefully and don't affect functionality.

3. **Background Operation Delays**: Unity's `EditorApplication.delayCall` executes more slowly when Unity is in the background, affecting auto-restart behavior.