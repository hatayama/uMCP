import { z } from 'zod';
import { BaseTool } from './base-tool.js';
import { ToolResponse } from '../types/tool-types.js';
import { TOOL_NAMES, LOG_CONFIG, UNITY_CONNECTION } from '../constants.js';

/**
 * Unity log retrieval tool
 */
export class LogsTool extends BaseTool {
  readonly name = TOOL_NAMES.GET_LOGS;
  readonly description = 'Retrieve log information from Unity console';
  readonly inputSchema = {
    type: 'object',
    properties: {
      logType: {
        type: 'string',
        description: 'Log type to filter (Error, Warning, Log, All)',
        enum: LOG_CONFIG.TYPES,
        default: LOG_CONFIG.DEFAULT_TYPE
      },
      maxCount: {
        type: 'number',
        description: 'Maximum number of logs to retrieve',
        default: LOG_CONFIG.DEFAULT_MAX_COUNT
      },
      searchText: {
        type: 'string',
        description: 'Text to search within log messages (retrieve all if empty)',
        default: LOG_CONFIG.DEFAULT_SEARCH_TEXT
      },
      includeStackTrace: {
        type: 'boolean',
        description: 'Whether to display stack trace',
        default: LOG_CONFIG.DEFAULT_INCLUDE_STACK_TRACE
      }
    },
    required: []
  };

  protected validateArgs(args: unknown) {
    const schema = z.object({
      logType: z.enum(LOG_CONFIG.TYPES).default(LOG_CONFIG.DEFAULT_TYPE),
      maxCount: z.number().default(LOG_CONFIG.DEFAULT_MAX_COUNT),
      searchText: z.string().default(LOG_CONFIG.DEFAULT_SEARCH_TEXT),
      includeStackTrace: z.boolean().default(LOG_CONFIG.DEFAULT_INCLUDE_STACK_TRACE)
    });
    
    const validatedArgs = schema.parse(args || {});
    
    return validatedArgs;
  }

  protected async execute(args: { logType: string; maxCount: number; searchText: string; includeStackTrace: boolean }): Promise<any> {
    // Connect to Unity (reconnect if necessary)
    await this.context.unityClient.ensureConnected();

    // Retrieve logs from Unity including stack trace information
    const result = await this.context.unityClient.getLogs(args.logType, args.maxCount, args.searchText, args.includeStackTrace);
    
    return result;
  }

  protected formatResponse(result: any): ToolResponse {
    const logType = result.requestedLogType || LOG_CONFIG.DEFAULT_TYPE;
    const maxCount = result.requestedMaxCount || LOG_CONFIG.DEFAULT_MAX_COUNT;
    const searchText = result.requestedSearchText || '';
    const includeStackTrace = result.requestedIncludeStackTrace !== undefined ? result.requestedIncludeStackTrace : LOG_CONFIG.DEFAULT_INCLUDE_STACK_TRACE;
    
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

Make sure Unity MCP Bridge is running and accessible on port ${process.env.UNITY_TCP_PORT || UNITY_CONNECTION.DEFAULT_PORT}.`
        }
      ]
    };
  }
} 