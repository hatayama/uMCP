import { z } from 'zod';
import { BaseTool } from './base-tool.js';
import { ToolResponse } from '../types/tool-types.js';
import { TOOL_NAMES, TEST_CONFIG } from '../constants.js';

/**
 * Unity Test Runner実行ツール
 */
export class RunTestsTool extends BaseTool {
  readonly name = TOOL_NAMES.RUN_TESTS;
  readonly description = 'Unity Test Runnerを実行してテスト結果を取得する';
  readonly inputSchema = {
    type: 'object',
    properties: {
      filterType: {
        type: 'string',
        description: 'テストフィルターの種類',
        enum: TEST_CONFIG.FILTER_TYPES,
        default: TEST_CONFIG.DEFAULT_FILTER_TYPE
      },
      filterValue: {
        type: 'string',
        description: 'フィルター値（filterTypeがall以外の場合に指定）\n' +
                    '• fullclassname: フルクラス名 (例: io.github.hatayama.uMCP.CompileCommandTests)\n' +
                    '• namespace: ネームスペース (例: io.github.hatayama.uMCP)\n' +
                    '• testname: 個別テスト名\n' +
                    '• assembly: アセンブリ名',
        default: TEST_CONFIG.DEFAULT_FILTER_VALUE
      },
      saveXml: {
        type: 'boolean',
        description: 'テスト結果をXMLファイルとして保存するかどうか',
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
      // Unity側に接続（必要に応じて再接続）
      await this.context.unityClient.ensureConnected();

      const response = await this.context.unityClient.runTests(
        args.filterType,
        args.filterValue,
        args.saveXml
      );

      // successの値に関係なく、テスト結果があれば詳細情報を表示
      let result = response.success ? `✅ テスト実行完了\n` : `⚠️ テスト実行完了（失敗あり）\n`;
      result += `📊 結果: ${response.message}\n`;
      
      if (response.testResults) {
        const testResults = response.testResults;
        result += `\n📈 詳細統計:\n`;
        result += `  • 成功: ${testResults.PassedCount}件\n`;
        result += `  • 失敗: ${testResults.FailedCount}件\n`;
        result += `  • スキップ: ${testResults.SkippedCount}件\n`;
        result += `  • 合計: ${testResults.TotalCount}件\n`;
        result += `  • 実行時間: ${testResults.Duration.toFixed(1)}秒\n`;
        
        // 失敗したテストの詳細を表示
        if (testResults.FailedTests && testResults.FailedTests.length > 0) {
          result += `\n❌ 失敗したテスト:\n`;
          testResults.FailedTests.forEach((failedTest: any, index: number) => {
            result += `  ${index + 1}. ${failedTest.TestName}\n`;
            result += `     フルネーム: ${failedTest.FullName}\n`;
            if (failedTest.Message) {
              result += `     エラー: ${failedTest.Message}\n`;
            }
            if (failedTest.StackTrace) {
              // スタックトレースは長いので最初の数行だけ表示
              const stackLines = failedTest.StackTrace.split('\n').slice(0, 3);
              result += `     スタックトレース: ${stackLines.join('\n     ')}\n`;
            }
            result += `     実行時間: ${failedTest.Duration.toFixed(3)}秒\n\n`;
          });
        }
      }
      
      if (response.xmlPath) {
        result += `\n📄 XMLファイル保存: ${response.xmlPath}\n`;
      }
      
      result += `\n⏰ 完了時刻: ${response.completedAt}`;
      
      // エラーがある場合のみエラー情報を追加
      if (!response.success && response.error && !response.testResults) {
        result += `\n\n❌ エラー詳細:\n${response.error}`;
      }
      
      return result;
    } catch (error) {
      return `❌ テスト実行エラー: ${error instanceof Error ? error.message : String(error)}`;
    }
  }
} 