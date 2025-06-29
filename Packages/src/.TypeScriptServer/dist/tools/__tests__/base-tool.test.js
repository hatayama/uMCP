import { BaseTool } from '../base-tool';
// Explicitly import Jest types
import { describe, it, expect } from '@jest/globals';
class DummyTool extends BaseTool {
    name = 'dummy';
    description = 'dummy tool';
    inputSchema = {};
    validateArgs(args) { return {}; }
    async execute(args) { return 'ok'; }
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
            async execute(args) { throw new Error('fail!'); }
        }
        const tool = new ErrorTool({ unityClient: {} });
        const res = await tool.handle({});
        expect(res.content[0].text).toContain('Error in dummy: fail!');
    });
});
//# sourceMappingURL=base-tool.test.js.map