import { BaseTool } from '../base-tool';

// Explicitly import Jest types
import { describe, it, expect } from '@jest/globals';

type DummyContext = { unityClient: any };

class DummyTool extends BaseTool {
  readonly name = 'dummy';
  readonly description = 'dummy tool';
  readonly inputSchema = {};
  protected validateArgs(args: unknown) { return {}; }
  protected async execute(args: any): Promise<string> { return 'ok'; }
}

describe('BaseTool', () => {
  it('should format response as text', async () => {
    const tool = new DummyTool({ unityClient: {} });
    const res = await tool.handle({});
    expect(res.content[0].type).toBe('text');
    expect(res.content[0].text).toBe('ok');
  });

  it('should format error response if execute throws', async () => {
    class ErrorTool extends DummyTool {
      protected async execute(args: any): Promise<string> { throw new Error('fail!'); }
    }
    const tool = new ErrorTool({ unityClient: {} });
    const res = await tool.handle({});
    expect(res.content[0].text).toContain('Error in dummy: fail!');
  });
}); 