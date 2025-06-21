import { z } from 'zod';
import { BaseTool } from './base-tool.js';
import { ToolResponse } from '../types/tool-types.js';
import { TOOL_NAMES, COMPILE_CONFIG, UNITY_CONNECTION } from '../constants.js';

/**
 * Unity compilation tool
 */
export class CompileTool extends BaseTool {
  readonly name = TOOL_NAMES.COMPILE;
  readonly description = 'After executing AssetDatabase.Refresh(), compile. Return the compilation results.';
  readonly inputSchema = {
    type: 'object',
    properties: {
      forceRecompile: {
        type: 'boolean',
        description: 'Whether to perform forced recompilation',
        default: COMPILE_CONFIG.DEFAULT_FORCE_RECOMPILE
      }
    }
  };

  protected validateArgs(args: unknown) {
    const schema = z.object({
      forceRecompile: z.boolean().default(COMPILE_CONFIG.DEFAULT_FORCE_RECOMPILE)
    });
    return schema.parse(args || {});
  }

  protected async execute(args: { forceRecompile: boolean }): Promise<any> {
    // Connect to Unity (reconnect if necessary)
    await this.context.unityClient.ensureConnected();

    // Execute compilation on Unity side
    return await this.context.unityClient.compileProject(args.forceRecompile);
  }

  protected formatResponse(result: any): ToolResponse {
    let responseText = `Unity Compile ${result.forceRecompile ? '(Force Recompile)' : ''} Result:
Success: ${result.success}
Errors: ${result.errorCount || 0}
Warnings: ${result.warningCount || 0}
Completed: ${result.completedAt}`;

    if (result.errors && result.errors.length > 0) {
      responseText += `\n\nErrors:`;
      result.errors.forEach((error: any, index: number) => {
        responseText += `\n${index + 1}. ${error.file}(${error.line},${error.column}): ${error.message}`;
      });
    }

    if (result.warnings && result.warnings.length > 0) {
      responseText += `\n\nWarnings:`;
      result.warnings.forEach((warning: any, index: number) => {
        responseText += `\n${index + 1}. ${warning.file}(${warning.line},${warning.column}): ${warning.message}`;
      });
    }

    return {
      content: [
        {
          type: 'text',
          text: responseText
        }
      ]
    };
  }

  protected formatErrorResponse(error: unknown): ToolResponse {
    const errorMessage = error instanceof Error ? error.message : 'Unknown error';
    const stack = error instanceof Error ? error.stack : 'No stack trace';
    
    return {
      content: [
        {
          type: 'text',
          text: `Unity Compile Failed!
Error: ${errorMessage}
Stack: ${stack}

Make sure Unity MCP Bridge is running and accessible on port ${process.env.UNITY_TCP_PORT || UNITY_CONNECTION.DEFAULT_PORT}.`
        }
      ]
    };
  }
} 