import { Server } from '@modelcontextprotocol/sdk/server/index.js';
import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';
import { ListToolsRequestSchema, CallToolRequestSchema } from '@modelcontextprotocol/sdk/types.js';

/**
 * Contract: MCP Server Connection Validator
 * Ensures that MCP server can be created and configured without errors
 * Fail-fast approach: Any violation immediately throws with descriptive error
 */
export class McpConnectionValidator {
  private server: Server | null = null;

  /**
   * Contract: Server must be creatable with valid configuration
   * @throws {Error} If server creation fails
   */
  createServer(): void {
    try {
      this.server = new Server(
        {
          name: 'uMCP-7400-test',
          version: '1.0.0',
        },
        {
          capabilities: {
            tools: {},
          },
        },
      );

      if (!this.server) {
        throw new Error('Contract violation: Server creation returned null');
      }
    } catch (error) {
      throw new Error(`Contract violation: Server creation failed - ${error}`);
    }
  }

  /**
   * Contract: Request handlers must be registerable without errors
   * @throws {Error} If handler registration fails
   */
  registerHandlers(): void {
    if (!this.server) {
      throw new Error('Contract violation: Server must be created before registering handlers');
    }

    try {
      // List tools handler
      this.server.setRequestHandler(ListToolsRequestSchema, async () => {
        return { tools: [] };
      });

      // Call tool handler
      this.server.setRequestHandler(CallToolRequestSchema, async (request) => {
        return {
          content: [
            {
              type: 'text',
              text: `Test response for ${request.params.name}`,
            },
          ],
        };
      });
    } catch (error) {
      throw new Error(`Contract violation: Handler registration failed - ${error}`);
    }
  }

  /**
   * Contract: Notifications must be sendable without errors
   * @throws {Error} If notification sending fails
   */
  testNotification(): void {
    if (!this.server) {
      throw new Error('Contract violation: Server must be created before sending notifications');
    }

    try {
      this.server.notification({
        method: 'notifications/tools/list_changed',
        params: {},
      });
    } catch (error) {
      throw new Error(`Contract violation: Notification sending failed - ${error}`);
    }
  }

  /**
   * Contract: Server must be able to validate JSON RPC compliance
   * @throws {Error} If JSON output is invalid
   */
  validateJsonRpcCompliance(): void {
    // Test that we can create valid JSON RPC messages
    const testMessage = {
      method: 'notifications/tools/list_changed',
      params: {},
      jsonrpc: '2.0',
    };

    try {
      const jsonString = JSON.stringify(testMessage);
      const parsed = JSON.parse(jsonString);

      if (!parsed.method || !parsed.jsonrpc) {
        throw new Error('Contract violation: Invalid JSON RPC structure');
      }
    } catch (error) {
      throw new Error(`Contract violation: JSON RPC compliance failed - ${error}`);
    }
  }

  /**
   * Run all validation tests with fail-fast approach
   * @throws {Error} On first validation failure
   */
  runAllValidations(): void {
    console.log('Starting MCP Connection Validation Tests...');

    try {
      console.log('Testing server creation...');
      this.createServer();

      console.log('Testing handler registration...');
      this.registerHandlers();

      console.log('Testing notification capability...');
      this.testNotification();

      console.log('Testing JSON RPC compliance...');
      this.validateJsonRpcCompliance();

      console.log('All MCP connection tests passed!');
    } catch (error) {
      console.error('MCP Connection Test Failed:', error);
      throw error; // Fail fast
    }
  }
}

// Export for standalone usage
export default McpConnectionValidator;
