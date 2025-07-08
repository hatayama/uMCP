import { z } from 'zod';

/**
 * Tool Type Definitions - Core interfaces for tool system
 *
 * Design document reference: Packages/src/TypeScriptServer~/ARCHITECTURE.md
 *
 * Related classes:
 * - BaseTool: Base class that implements ToolHandler interface
 * - DynamicUnityCommandTool: Specific tool implementation
 * - UnityToolManager: Manages tools that use these types
 * - ToolRegistry: Manages tool registration using these interfaces
 *
 * Key features:
 * - ToolHandler interface for all tools
 * - ToolResponse type for MCP-compliant responses
 * - ToolContext for Unity client access
 * - ToolDefinition for tool metadata
 */

/**
 * Base interface for tool handlers
 */
export interface ToolHandler {
  readonly name: string;
  readonly description: string;
  readonly inputSchema: object;
  handle(args: unknown): Promise<ToolResponse>;
}

/**
 * Tool response type (aligned with MCP server return values)
 */
export interface ToolResponse {
  content: Array<{
    type: string;
    text: string;
  }>;
  isError?: boolean;
}

/**
 * Tool execution context
 */
export interface ToolContext {
  unityClient: any; // UnityClient type will be defined later
}

/**
 * Tool definition type
 */
export interface ToolDefinition {
  name: string;
  description: string;
  inputSchema: object;
}
