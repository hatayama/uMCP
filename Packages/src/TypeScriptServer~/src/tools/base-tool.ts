import { z } from 'zod';
import { ToolHandler, ToolResponse, ToolContext } from '../types/tool-types.js';

/**
 * Base class for tools
 * Provides common processing and template method pattern
 *
 * Design document reference: Packages/src/Editor/ARCHITECTURE.md
 *
 * Related classes:
 * - DynamicUnityCommandTool: Extends this class for Unity command tools
 * - UnityMcpServer: Uses tools through this interface
 * - ToolHandler: Interface this class implements
 */
export abstract class BaseTool implements ToolHandler {
  abstract readonly name: string;
  abstract readonly description: string;
  abstract readonly inputSchema: object;

  protected context: ToolContext;

  constructor(context: ToolContext) {
    this.context = context;
  }

  /**
   * Main method for tool execution
   */
  async handle(args: unknown): Promise<ToolResponse> {
    try {
      const validatedArgs = this.validateArgs(args);
      const result = await this.execute(validatedArgs);
      return this.formatResponse(result);
    } catch (error) {
      return this.formatErrorResponse(error);
    }
  }

  /**
   * Argument validation (implemented in subclass)
   */
  protected abstract validateArgs(args: unknown): any;

  /**
   * Actual processing (implemented in subclass)
   */
  protected abstract execute(args: any): Promise<any>;

  /**
   * Format success response (can be overridden in subclass)
   */
  protected formatResponse(result: any): ToolResponse {
    return {
      content: [
        {
          type: 'text',
          text: typeof result === 'string' ? result : JSON.stringify(result, null, 2),
        },
      ],
    };
  }

  /**
   * Format error response
   */
  protected formatErrorResponse(error: unknown): ToolResponse {
    const errorMessage = error instanceof Error ? error.message : 'Unknown error';
    return {
      content: [
        {
          type: 'text',
          text: `Error in ${this.name}: ${errorMessage}`,
        },
      ],
    };
  }
}
