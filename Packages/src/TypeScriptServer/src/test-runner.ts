#!/usr/bin/env node

import { McpConnectionValidator } from './tools/__tests__/mcp-connection.test.js';

/**
 * Standalone test runner for MCP connection validation
 * Implements fail-fast approach with contract-based design
 */
async function runMcpValidationTests(): Promise<void> {
    console.log('🚀 Starting MCP Connection Validation Suite');
    console.log('📋 Contract-based testing with fail-fast approach');
    console.log('=' .repeat(50));
    
    const validator = new McpConnectionValidator();
    
    try {
        // Run all validations - will throw on first failure
        validator.runAllValidations();
        
        console.log('=' .repeat(50));
        console.log('✅ All MCP connection tests PASSED');
        console.log('🎯 Server is ready for Cursor integration');
        
        process.exit(0);
    } catch (error) {
        console.log('=' .repeat(50));
        console.error('❌ MCP connection tests FAILED');
        console.error('🚨 Fail-fast triggered:', error);
        console.log('🛠️  Fix the issue before proceeding');
        
        process.exit(1);
    }
}

// Run if called directly
if (import.meta.url === `file://${process.argv[1]}`) {
    runMcpValidationTests().catch((error) => {
        console.error('Fatal error:', error);
        process.exit(1);
    });
} 