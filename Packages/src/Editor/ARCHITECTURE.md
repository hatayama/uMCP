# uMCP Unity Editor-Side Architecture

## 1. Overview

This document details the architecture of the C# code within the `Packages/src/Editor` directory. This code runs inside the Unity Editor and serves as the bridge between the Unity environment and the external TypeScript-based MCP (Model-Context-Protocol) server.

Its primary responsibilities are:
1.  **Running a TCP Server (`McpBridgeServer`)**: Listens for connections from the TypeScript server to receive commands.
2.  **Executing Unity Operations**: Processes received commands to perform actions within the Unity Editor, such as compiling the project, running tests, or retrieving logs.
3.  **Providing a User Interface (`McpEditorWindow`)**: Offers a GUI within the Unity Editor for developers to manage and monitor the MCP server.
4.  **Managing Configuration**: Handles the setup of `mcp.json` files required by LLM tools like Cursor, Claude, and VSCode.

## 2. Core Architectural Principles

The architecture is built upon several key design principles to ensure robustness, extensibility, and maintainability.

### 2.1. Command Pattern
The system is centered around the **Command Pattern**. Each action that can be triggered by an LLM tool is encapsulated in its own command class.

- **`IUnityCommand`**: The common interface for all commands.
- **`AbstractUnityCommand<TSchema, TResponse>`**: A generic abstract base class that provides type-safe handling of parameters and responses.
- **`UnityCommandRegistry`**: A central registry that discovers and holds all available commands.
- **`UnityApiHandler` / `UnityCommandExecutor`**: These classes receive a command name and parameters, look up the command in the registry, and execute it.

This pattern makes the system highly extensible. To add a new feature, a developer simply needs to create a new class that implements `IUnityCommand` and decorate it with the `[McpTool]` attribute. The system will automatically discover and expose it.

### 2.2. Schema-Driven and Type-Safe Communication
To avoid manual and error-prone JSON parsing, the system uses a schema-driven approach for commands.

- **`*Schema.cs` files** (e.g., `CompileSchema.cs`, `GetLogsSchema.cs`): These classes define the expected parameters for a command using simple C# properties. Attributes like `[Description]` and default values are used to automatically generate a JSON Schema for the client.
- **`*Response.cs` files** (e.g., `CompileResponse.cs`): These define the structure of the data returned to the client.
- **`CommandParameterSchemaGenerator.cs`**: This utility uses reflection on the `*Schema.cs` files to generate the parameter schema dynamically, ensuring the C# code is the single source of truth.

This design eliminates inconsistencies between the server and client and provides strong type safety within the C# code.

### 2.3. SOLID Principles
- **Single Responsibility Principle (SRP)**: Each class has a well-defined responsibility.
    - `McpBridgeServer`: Handles raw TCP communication.
    - `McpServerController`: Manages the server's lifecycle and state across domain reloads.
    - `McpConfigRepository`: Handles file I/O for configuration.
    - `McpConfigService`: Implements the business logic for configuration.
    - `JsonRpcProcessor`: Deals exclusively with parsing and formatting JSON-RPC 2.0 messages.
- **Open/Closed Principle (OCP)**: The system is open for extension but closed for modification. The Command Pattern is the prime example; new commands can be added without altering the core execution logic.

### 2.4. Resilience to Domain Reloads
A significant challenge in the Unity Editor is the "domain reload," which resets the application's state. The architecture handles this gracefully:
- **`McpServerController`**: Uses `[InitializeOnLoad]` to hook into Editor lifecycle events.
- **`AssemblyReloadEvents`**: Before a reload, `OnBeforeAssemblyReload` is used to save the server's running state (port, status) into `SessionState`.
- **`SessionState`**: A Unity Editor feature that persists simple data across domain reloads.
- After a reload, `OnAfterAssemblyReload` reads the `SessionState` and automatically restarts the server if it was previously running, ensuring a seamless experience for the connected client.

## 3. Key Components (Directory Breakdown)

### `/Server`
This directory contains the core networking and lifecycle management components.
- **`McpBridgeServer.cs`**: The low-level TCP server. It listens on a specified port, accepts client connections, and handles the reading/writing of JSON data over the network stream. It operates on a background thread.
- **`McpServerController.cs`**: The high-level, static manager for the server. It controls the lifecycle (Start, Stop, Restart) of the `McpBridgeServer` instance. It is the central point for managing state across domain reloads.
- **`McpServerConfig.cs`**: A static class holding constants for server configuration (e.g., default port, buffer sizes).

### `/Api`
This is the heart of the command processing logic.
- **`/Commands`**: Contains the implementation of all supported commands.
    - **`/Core`**: The foundational classes for the command system.
        - **`IUnityCommand.cs`**: Defines the contract for all commands, including `CommandName`, `Description`, `ParameterSchema`, and the `ExecuteAsync` method.
        - **`AbstractUnityCommand.cs`**: The generic base class that simplifies command creation by handling the boilerplate of parameter deserialization and response creation.
        - **`UnityCommandRegistry.cs`**: Discovers all classes with the `[McpTool]` attribute and registers them in a dictionary, mapping a command name to its implementation.
        - **`McpToolAttribute.cs`**: A simple attribute used to mark a class for automatic registration as a command.
    - **Command-specific folders** (e.g., `/Compile`, `/RunTests`): Each folder contains the `*Command.cs`, `*Schema.cs`, and `*Response.cs` files for a single command, keeping the implementation clean and organized.
- **`JsonRpcProcessor.cs`**: Responsible for parsing incoming JSON strings into `JsonRpcRequest` objects and serializing response objects back into JSON strings, adhering to the JSON-RPC 2.0 specification.
- **`UnityApiHandler.cs`**: The entry point for API calls. It receives the method name and parameters from the `JsonRpcProcessor` and uses the `UnityCommandRegistry` to execute the appropriate command.

### `/UI`
Contains the code for the user-facing Editor Window.
- **`McpEditorWindow.cs`**: The main GUI window (`Window > uMCP`). It allows the user to see the server status, start/stop the server, change the port, and configure settings for different LLM tools. It interacts primarily with `McpServerController` and `McpConfigService`.
- **`McpCommunicationLog.cs`**: Manages the in-memory and `SessionState`-backed log of requests and responses displayed in the "Developer Tools" section of the window.

### `/Config`
Manages the creation and modification of `mcp.json` configuration files.
- **`UnityMcpPathResolver.cs`**: A utility to find the correct path for configuration files for different editors (Cursor, VSCode, etc.).
- **`McpConfigRepository.cs`**: Handles the direct reading and writing of the `mcp.json` file.
- **`McpConfigService.cs`**: Contains the logic for auto-configuring the `mcp.json` file with the correct command, arguments, and environment variables based on the user's settings in the `McpEditorWindow`.

### `/Tools`
Contains higher-level utilities that wrap core Unity Editor functionality.
- **`/ConsoleUtility` & `/ConsoleLogFetcher`**: A set of classes, primarily `ConsoleLogRetriever`, that use reflection to access Unity's internal console log entries. This allows the `getlogs` command to retrieve logs with specific types and filters.
- **`/TestRunner`**: Contains the logic for executing Unity tests.
    - **`PlayModeTestExecuter.cs`**: A key class that handles the complexity of running PlayMode tests, which involves disabling domain reloads (`DomainReloadDisableScope`) to ensure the `async` task can complete successfully.
    - **`NUnitXmlResultExporter.cs`**: Formats test results into NUnit-compatible XML files.
- **`/Util`**: General-purpose utilities.
    - **`CompileController.cs`**: Wraps the `CompilationPipeline` API to provide a simple `async` interface for compiling the project.

### `/Utils`
Contains low-level, general-purpose helper classes.
- **`MainThreadSwitcher.cs`**: A crucial utility that provides an `awaitable` object to switch execution from a background thread (like the TCP server's) back to Unity's main thread. This is essential because most Unity APIs can only be called from the main thread.
- **`EditorDelay.cs`**: A custom, `async/await`-compatible implementation of a frame-based delay, useful for waiting a few frames for the Editor to reach a stable state, especially after domain reloads.
- **`McpLogger.cs`**: A simple, unified logging wrapper to prefix all package-related logs with `[uMCP]`.

## 4. Key Workflows

### Command Execution Flow
1.  The `McpBridgeServer` receives a JSON string from the TypeScript client.
2.  The string is passed to `JsonRpcProcessor.ProcessRequest`.
3.  `JsonRpcProcessor` deserializes the JSON into a `JsonRpcRequest` object.
4.  It calls `UnityApiHandler.ExecuteCommandAsync` with the command name and parameters (`JToken`).
5.  `UnityApiHandler` uses `UnityCommandRegistry` to find the `IUnityCommand` instance matching the command name.
6.  The `ExecuteAsync(JToken)` method of the command instance is invoked.
7.  The `AbstractUnityCommand` base class deserializes the `JToken` into a strongly-typed `*Schema` object.
8.  It then calls the command's specific, overridden `ExecuteAsync(*Schema parameters)` method.
9.  The command performs its logic (e.g., calling `CompileController` or `PlayModeTestExecuter`).
10. The command returns a strongly-typed `*Response` object.
11. The response bubbles back up to `JsonRpcProcessor`, which serializes it into a JSON string and sends it back to the client via `McpBridgeServer`.
