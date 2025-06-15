import { z } from 'zod';
import { BaseTool } from './base-tool.js';
import { ToolResponse } from '../types/tool-types.js';

/**
 * Unityコンパイルツール
 */
export class CompileTool extends BaseTool {
  readonly name = 'action.compileUnity';
  readonly description = 'Unityプロジェクトのコンパイルを実行し、エラー情報を取得する';
  readonly inputSchema = {
    type: 'object',
    properties: {
      forceRecompile: {
        type: 'boolean',
        description: '強制再コンパイルを行うかどうか',
        default: false
      }
    }
  };

  protected validateArgs(args: unknown) {
    const schema = z.object({
      forceRecompile: z.boolean().default(false)
    });
    return schema.parse(args || {});
  }

  protected async execute(args: { forceRecompile: boolean }): Promise<any> {
    // Unity側に接続（必要に応じて）
    if (!this.context.unityClient.connected) {
      await this.context.unityClient.connect();
    }

    // Unity側でコンパイルを実行
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

Make sure Unity MCP Bridge is running and accessible on port ${process.env.UNITY_TCP_PORT || '7400'}.`
        }
      ]
    };
  }
} 