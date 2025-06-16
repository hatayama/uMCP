import { z } from 'zod';
import { BaseTool } from './base-tool.js';
import { ToolResponse } from '../types/tool-types.js';

/**
 * Unity Test Runner実行ツール
 */
export class RunTestsTool extends BaseTool {
  readonly name = 'action_runTests';
  readonly description = 'Unity Test Runnerを実行してテスト結果を取得する';
  readonly inputSchema = {
    type: 'object',
    properties: {
      filterType: {
        type: 'string',
        description: 'テストフィルターの種類',
        enum: ['all', 'fullclassname', 'namespace', 'testname', 'assembly'],
        default: 'all'
      },
      filterValue: {
        type: 'string',
        description: 'フィルター値（filterTypeがall以外の場合に指定）\n' +
                    '• fullclassname: フルクラス名 (例: io.github.hatayama.uMCP.CompileCommandTests)\n' +
                    '• namespace: ネームスペース (例: io.github.hatayama.uMCP)\n' +
                    '• testname: 個別テスト名\n' +
                    '• assembly: アセンブリ名',
        default: ''
      },
      saveXml: {
        type: 'boolean',
        description: 'テスト結果をXMLファイルとして保存するかどうか',
        default: false
      }
    }
  };

  protected validateArgs(args: unknown) {
    const schema = z.object({
      filterType: z.enum(['all', 'fullclassname', 'namespace', 'testname', 'assembly']).default('all'),
      filterValue: z.string().default(''),
      saveXml: z.boolean().default(false)
    });
    return schema.parse(args || {});
  }

  protected async execute(args: { filterType: string; filterValue: string; saveXml: boolean }): Promise<string> {
    try {
      const response = await this.context.unityClient.sendCommand('runtests', {
        filterType: args.filterType,
        filterValue: args.filterValue,
        saveXml: args.saveXml
      });

      if (response.success) {
        let result = `✅ テスト実行完了\n`;
        result += `📊 結果: ${response.message}\n`;
        
        if (response.testResults) {
          const testResults = response.testResults;
          result += `\n📈 詳細統計:\n`;
          result += `  • 成功: ${testResults.PassedCount}件\n`;
          result += `  • 失敗: ${testResults.FailedCount}件\n`;
          result += `  • スキップ: ${testResults.SkippedCount}件\n`;
          result += `  • 合計: ${testResults.TotalCount}件\n`;
          result += `  • 実行時間: ${testResults.Duration.toFixed(1)}秒\n`;
        }
        
        if (response.xmlPath) {
          result += `\n📄 XMLファイル保存: ${response.xmlPath}\n`;
        }
        
        result += `\n⏰ 完了時刻: ${response.completedAt}`;
        
        return result;
      } else {
        return `❌ テスト実行失敗: ${response.message}\n${response.error || ''}`;
      }
    } catch (error) {
      return `❌ テスト実行エラー: ${error instanceof Error ? error.message : String(error)}`;
    }
  }
} 