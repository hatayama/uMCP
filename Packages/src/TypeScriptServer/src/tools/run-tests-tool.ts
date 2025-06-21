import { z } from 'zod';
import { BaseTool } from './base-tool.js';
import { ToolResponse } from '../types/tool-types.js';
import { TOOL_NAMES, TEST_CONFIG } from '../constants.js';

/**
 * Unity Test Runner execution tool
 */
export class RunTestsTool extends BaseTool {
  readonly name = TOOL_NAMES.RUN_TESTS;
  readonly description = 'Execute Unity Test Runner and retrieve test results';
  readonly inputSchema = {
    type: 'object',
    properties: {
      filterType: {
        type: 'string',
        description: 'Type of test filter',
        enum: TEST_CONFIG.FILTER_TYPES,
        default: TEST_CONFIG.DEFAULT_FILTER_TYPE
      },
      filterValue: {
        type: 'string',
        description: 'Filter value (specify when filterType is not all)\n' +
                    '• fullclassname: Full class name (e.g.: io.github.hatayama.uMCP.CompileCommandTests)\n' +
                    '• namespace: Namespace (e.g.: io.github.hatayama.uMCP)\n' +
                    '• testname: Individual test name\n' +
                    '• assembly: Assembly name',
        default: TEST_CONFIG.DEFAULT_FILTER_VALUE
      },
      saveXml: {
        type: 'boolean',
        description: 'Whether to save test results as XML file',
        default: TEST_CONFIG.DEFAULT_SAVE_XML
      }
    }
  };

  protected validateArgs(args: unknown) {
    const schema = z.object({
      filterType: z.enum(TEST_CONFIG.FILTER_TYPES).default(TEST_CONFIG.DEFAULT_FILTER_TYPE),
      filterValue: z.string().default(TEST_CONFIG.DEFAULT_FILTER_VALUE),
      saveXml: z.boolean().default(TEST_CONFIG.DEFAULT_SAVE_XML)
    });
    return schema.parse(args || {});
  }

  protected async execute(args: { filterType: string; filterValue: string; saveXml: boolean }): Promise<string> {
    try {
      // Connect to Unity (reconnect if necessary)
      await this.context.unityClient.ensureConnected();

      const response = await this.context.unityClient.runTests(
        args.filterType,
        args.filterValue,
        args.saveXml
      );

      // Display detailed information if test results exist, regardless of success value
      let result = response.success ? `✅ Test execution completed\n` : `⚠️ Test execution completed (with failures)\n`;
      result += `Result: ${response.message}\n`;
      
      if (response.testResults) {
        const testResults = response.testResults;
        result += `\nDetailed Statistics:\n`;
        result += `  • Passed: ${testResults.PassedCount} tests\n`;
        result += `  • Failed: ${testResults.FailedCount} tests\n`;
        result += `  • Skipped: ${testResults.SkippedCount} tests\n`;
        result += `  • Total: ${testResults.TotalCount} tests\n`;
        result += `  • Execution time: ${testResults.Duration.toFixed(1)} seconds\n`;
        
        // Display details of failed tests
        if (testResults.FailedTests && testResults.FailedTests.length > 0) {
          result += `\n❌ Failed Tests:\n`;
          testResults.FailedTests.forEach((failedTest: any, index: number) => {
            result += `  ${index + 1}. ${failedTest.TestName}\n`;
            result += `     Full name: ${failedTest.FullName}\n`;
            if (failedTest.Message) {
              result += `     Error: ${failedTest.Message}\n`;
            }
            if (failedTest.StackTrace) {
              // Display only the first few lines of stack trace as it can be long
              const stackLines = failedTest.StackTrace.split('\n').slice(0, 3);
              result += `     Stack trace: ${stackLines.join('\n     ')}\n`;
            }
            result += `     Execution time: ${failedTest.Duration.toFixed(3)} seconds\n\n`;
          });
        }
      }
      
      if (response.xmlPath) {
        result += `\nXML file saved: ${response.xmlPath}\n`;
      }
      
      result += `\n⏰ Completed at: ${response.completedAt}`;
      
      // Add error information only when there are errors
      if (!response.success && response.error && !response.testResults) {
        result += `\n\n❌ Error Details:\n${response.error}`;
      }
      
      return result;
    } catch (error) {
      return `❌ Test execution error: ${error instanceof Error ? error.message : String(error)}`;
    }
  }
} 