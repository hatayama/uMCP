/**
 * MCP Server Constants
 *
 * Centralized constants for the Unity MCP server implementation
 * 
 * Design document reference: Packages/src/Editor/ARCHITECTURE.md
 * 
 * Related files:
 * - constants.ts: General server constants
 * - server.ts: Uses MCP protocol version and capabilities
 * - tools/ping-tool.ts: Uses development tool constants
 * - tools/get-available-commands-tool.ts: Uses development tool constants
 * 
 * Key features:
 * - MCP protocol version and server name
 * - Capability flags (tools list changed notification)
 * - Development tool names and descriptions
 * - Client name defaults (empty string to rely on MCP protocol)
 */

// MCP Protocol Constants
export const MCP_PROTOCOL_VERSION = '2024-11-05';

// Server Information
export const MCP_SERVER_NAME = 'umcp-server';

// Capabilities
export const TOOLS_LIST_CHANGED_CAPABILITY = true;

// Development Tools
export const DEV_TOOL_PING_NAME = 'mcp-ping';
export const DEV_TOOL_PING_DESCRIPTION = 'TypeScript side health check (dev only)';
export const DEV_TOOL_PING_DEFAULT_MESSAGE = 'Hello Unity MCP!';

export const DEV_TOOL_COMMANDS_NAME = 'get-unity-commands';
export const DEV_TOOL_COMMANDS_DESCRIPTION = 'Get Unity commands list (dev only)';

// Client Name Constants
export const DEFAULT_CLIENT_NAME = ''; // Empty string to avoid showing default names
