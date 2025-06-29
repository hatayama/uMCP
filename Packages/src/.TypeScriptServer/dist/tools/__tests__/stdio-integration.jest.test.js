import { spawn } from 'child_process';
import { join } from 'path';
/**
 * Integration test for actual stdio MCP communication
 * This test catches issues like console.log pollution that unit tests miss
 */
describe('MCP Stdio Integration Tests', () => {
    let serverProcess;
    const serverPath = join(__dirname, '../../../dist/server.bundle.js');
    afterEach(() => {
        if (serverProcess) {
            serverProcess.kill();
        }
    });
    test('should not pollute stdout with console.log statements', (done) => {
        serverProcess = spawn('node', [serverPath], {
            stdio: ['pipe', 'pipe', 'pipe']
        });
        let stdoutData = '';
        let hasReceivedValidJson = false;
        serverProcess.stdout?.on('data', (data) => {
            stdoutData += data.toString();
            // Check if output contains console.log pollution
            const lines = stdoutData.split('\n');
            for (const line of lines) {
                if (line.trim() === '')
                    continue;
                try {
                    // Every line should be valid JSON
                    JSON.parse(line);
                    hasReceivedValidJson = true;
                }
                catch (error) {
                    // If we get invalid JSON, it's likely console.log pollution
                    if (line.includes('[Simple MCP]') || line.includes('===')) {
                        done(new Error(`Console.log pollution detected: ${line}`));
                        return;
                    }
                }
            }
        });
        serverProcess.stderr?.on('data', (data) => {
            const errorOutput = data.toString();
            if (errorOutput.includes('[Simple MCP]')) {
                done(new Error(`Console.error pollution detected: ${errorOutput}`));
                return;
            }
        });
        // Send initialize request
        const initRequest = {
            jsonrpc: '2.0',
            id: 1,
            method: 'initialize',
            params: {
                protocolVersion: '2024-11-05',
                capabilities: {},
                clientInfo: {
                    name: 'test-client',
                    version: '1.0.0'
                }
            }
        };
        serverProcess.stdin?.write(JSON.stringify(initRequest) + '\n');
        // Give server time to respond
        setTimeout(() => {
            if (hasReceivedValidJson) {
                done();
            }
            else {
                done(new Error('No valid JSON response received from server'));
            }
        }, 2000);
    }, 10000);
    test('should handle tools/list request without stdout pollution', (done) => {
        serverProcess = spawn('node', [serverPath], {
            stdio: ['pipe', 'pipe', 'pipe']
        });
        let responseCount = 0;
        let hasToolsResponse = false;
        serverProcess.stdout?.on('data', (data) => {
            const lines = data.toString().split('\n');
            for (const line of lines) {
                if (line.trim() === '')
                    continue;
                try {
                    const response = JSON.parse(line);
                    responseCount++;
                    if (response.result && response.result.tools) {
                        hasToolsResponse = true;
                    }
                }
                catch (error) {
                    done(new Error(`Invalid JSON in response: ${line}`));
                    return;
                }
            }
        });
        // Initialize first
        const initRequest = {
            jsonrpc: '2.0',
            id: 1,
            method: 'initialize',
            params: {
                protocolVersion: '2024-11-05',
                capabilities: {},
                clientInfo: { name: 'test-client', version: '1.0.0' }
            }
        };
        serverProcess.stdin?.write(JSON.stringify(initRequest) + '\n');
        // Then request tools list
        setTimeout(() => {
            const toolsRequest = {
                jsonrpc: '2.0',
                id: 2,
                method: 'tools/list',
                params: {}
            };
            serverProcess.stdin?.write(JSON.stringify(toolsRequest) + '\n');
        }, 500);
        // Check results
        setTimeout(() => {
            if (hasToolsResponse) {
                done();
            }
            else {
                done(new Error('Did not receive valid tools/list response'));
            }
        }, 3000);
    }, 10000);
});
//# sourceMappingURL=stdio-integration.jest.test.js.map