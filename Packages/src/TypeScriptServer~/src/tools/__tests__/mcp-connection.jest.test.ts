import { McpConnectionValidator } from './mcp-connection.test.js';

// Jest test cases
describe('MCP Connection Tests', () => {
  let validator: McpConnectionValidator;

  beforeEach(() => {
    validator = new McpConnectionValidator();
  });

  test('should create server successfully', () => {
    expect(() => validator.createServer()).not.toThrow();
  });

  test('should register handlers successfully', () => {
    validator.createServer();
    expect(() => validator.registerHandlers()).not.toThrow();
  });

  test('should send notifications successfully', () => {
    validator.createServer();
    validator.registerHandlers();
    expect(() => validator.testNotification()).not.toThrow();
  });

  test('should validate JSON RPC compliance', () => {
    expect(() => validator.validateJsonRpcCompliance()).not.toThrow();
  });

  test('should pass all validations', () => {
    expect(() => validator.runAllValidations()).not.toThrow();
  });

  test('should fail fast on invalid server config', () => {
    // Test contract violation
    const invalidValidator = new McpConnectionValidator();
    expect(() => invalidValidator.registerHandlers()).toThrow(
      'Contract violation: Server must be created before registering handlers',
    );
  });
});
