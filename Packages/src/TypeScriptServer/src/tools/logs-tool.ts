import { z } from 'zod';
import { BaseTool } from './base-tool.js';
import { ToolResponse } from '../types/tool-types.js';

/**
 * Unityログ取得ツール
 */
export class LogsTool extends BaseTool {
  readonly name = 'unity.getLogs';
  readonly description = 'Unityコンソールのログ情報を取得する';
  readonly inputSchema = {
    type: 'object',
    properties: {
      logType: {
        type: 'string',
        description: 'フィルタリングするログタイプ (Error, Warning, Log, All)',
        enum: ['Error', 'Warning', 'Log', 'All'],
        default: 'All'
      },
      maxCount: {
        type: 'number',
        description: '取得する最大ログ数',
        default: 100
      }
    }
  };

  protected validateArgs(args: unknown) {
    const schema = z.object({
      logType: z.enum(['Error', 'Warning', 'Log', 'All']).default('All'),
      maxCount: z.number().default(100)
    });
    return schema.parse(args || {});
  }

  protected async execute(args: { logType: string; maxCount: number }): Promise<any> {
    // Unity側に接続（必要に応じて再接続）
    await this.context.unityClient.ensureConnected();

    // Unity側からログを取得
    return await this.context.unityClient.getLogs(args.logType, args.maxCount);
  }

  protected formatResponse(result: any): ToolResponse {
    let responseText = `Unity Console Logs (Filter: ${result.logType}, Max: ${result.maxCount}):\n\n`;
    
    if (result.logs.length > 0) {
      responseText += result.logs.map((log: any) => {
        let logLine = `[${log.timestamp}] ${log.type}: ${log.message}`;
        if (log.stackTrace) {
          logLine += `\nStack Trace: ${log.stackTrace}`;
        }
        return logLine;
      }).join('\n\n');
    } else {
      responseText += 'No logs found.';
    }

    responseText += `\n\nTotal logs: ${result.logs.length} (of ${result.totalCount} total)`;

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
          text: `Unity Get Logs Failed!
Error: ${errorMessage}
Stack: ${stack}

Make sure Unity MCP Bridge is running and accessible on port ${process.env.UNITY_TCP_PORT || '7400'}.`
        }
      ]
    };
  }
} 