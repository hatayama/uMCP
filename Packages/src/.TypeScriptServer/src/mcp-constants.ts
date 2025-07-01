/**
 * MCP Server Constants
 *
 * Centralized constants for the Unity MCP server implementation
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
