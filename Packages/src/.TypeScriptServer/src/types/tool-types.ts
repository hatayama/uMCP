import { z } from 'zod';

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
