import { z } from 'zod';
import { BaseTool } from './base-tool.js';
import { ToolResponse } from '../types/tool-types.js';

/**
 * Unityログ取得ツール
 */
export class LogsTool extends BaseTool {
  readonly name = 'unity-get-logs';
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
      },
      searchText: {
        type: 'string',
        description: 'ログメッセージ内で検索するテキスト（空の場合は全て取得）',
        default: ''
      },
      includeStackTrace: {
        type: 'boolean',
        description: 'スタックトレースを表示するかどうか',
        default: true
      }
    },
    required: []
  };

  protected validateArgs(args: unknown) {
    const schema = z.object({
      logType: z.enum(['Error', 'Warning', 'Log', 'All']).default('All'),
      maxCount: z.number().default(100),
      searchText: z.string().default(''),
      includeStackTrace: z.boolean().default(true)
    });
    
    const validatedArgs = schema.parse(args || {});
    
    return validatedArgs;
  }

  protected async execute(args: { logType: string; maxCount: number; searchText: string; includeStackTrace: boolean }): Promise<any> {
    // Unity側に接続（必要に応じて再接続）
    await this.context.unityClient.ensureConnected();

    // Unity側にスタックトレース情報も含めてログを取得
    const result = await this.context.unityClient.getLogs(args.logType, args.maxCount, args.searchText, args.includeStackTrace);
    
    return result;
  }

  protected formatResponse(result: any): ToolResponse {
    const logType = result.requestedLogType || 'All';
    const maxCount = result.requestedMaxCount || 100;
    const searchText = result.requestedSearchText || '';
    const includeStackTrace = result.requestedIncludeStackTrace !== undefined ? result.requestedIncludeStackTrace : true;
    
    let responseText = `Unity Console Logs (Filter: ${logType}, Max: ${maxCount}${searchText ? `, Search: "${searchText}"` : ''}, StackTrace: ${includeStackTrace ? 'ON' : 'OFF'}):\n\n`;
    
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