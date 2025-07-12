#!/usr/bin/env node

/**
 * Simple MCP Client for testing uLoopMCP Resources functionality
 * 
 * This test client simulates MCP protocol communication to verify:
 * - resources/list endpoint
 * - resources/read endpoint
 * - TypeScript server resource handling
 */

import { spawn } from 'child_process';
import { promises as fs } from 'fs';

class SimpleMcpClient {
  constructor() {
    this.serverProcess = null;
    this.requestId = 1;
  }

  async startServer() {
    console.log('Starting MCP server...');
    
    this.serverProcess = spawn('node', ['dist/server.bundle.js'], {
      cwd: '/Users/a12115/work/oss/uLoopMCP/Packages/src/TypeScriptServer~',
      stdio: ['pipe', 'pipe', 'pipe']
    });

    this.serverProcess.stderr.on('data', (data) => {
      console.error('Server stderr:', data.toString());
    });

    // Wait a bit for server to start
    await new Promise(resolve => setTimeout(resolve, 1000));
  }

  async sendRequest(method, params = {}) {
    if (!this.serverProcess) {
      throw new Error('Server not started');
    }

    const request = {
      jsonrpc: '2.0',
      id: this.requestId++,
      method,
      params
    };

    console.log('Sending request:', JSON.stringify(request, null, 2));

    return new Promise((resolve, reject) => {
      let responseData = '';
      
      const timeout = setTimeout(() => {
        reject(new Error('Request timeout'));
      }, 5000);

      this.serverProcess.stdout.on('data', (data) => {
        responseData += data.toString();
        
        // Try to parse response
        try {
          const lines = responseData.split('\n').filter(line => line.trim());
          
          for (const line of lines) {
            if (line.trim()) {
              const response = JSON.parse(line);
              if (response.id === request.id) {
                clearTimeout(timeout);
                console.log('Received response:', JSON.stringify(response, null, 2));
                resolve(response);
                return;
              }
            }
          }
        } catch (e) {
          // Continue collecting data
        }
      });

      this.serverProcess.stdin.write(JSON.stringify(request) + '\n');
    });
  }

  async initialize() {
    return await this.sendRequest('initialize', {
      protocolVersion: '2024-11-05',
      capabilities: {
        tools: { listChanged: true },
        resources: { subscribe: true, listChanged: true }
      },
      clientInfo: {
        name: 'test-mcp-client',
        version: '1.0.0'
      }
    });
  }

  async listResources() {
    return await this.sendRequest('resources/list', {});
  }

  async readResource(uri) {
    return await this.sendRequest('resources/read', { uri });
  }

  async cleanup() {
    if (this.serverProcess) {
      this.serverProcess.kill();
    }
  }
}

async function runTest() {
  const client = new SimpleMcpClient();
  
  try {
    console.log('=== MCP Resources Test ===\n');
    
    // Start server
    await client.startServer();
    
    // Initialize
    console.log('1. Initializing...');
    const initResponse = await client.initialize();
    console.log('Init successful:', initResponse.result ? 'Yes' : 'No');
    console.log('Resources capability:', initResponse.result?.capabilities?.resources);
    console.log('');
    
    // List resources
    console.log('2. Listing resources...');
    const listResponse = await client.listResources();
    if (listResponse.result) {
      console.log(`Found ${listResponse.result.resources?.length || 0} resources:`);
      listResponse.result.resources?.forEach(resource => {
        console.log(`  - ${resource.uri}: ${resource.name}`);
      });
    }
    console.log('');
    
    // Read each resource
    if (listResponse.result?.resources) {
      for (const resource of listResponse.result.resources) {
        console.log(`3. Reading resource: ${resource.uri}`);
        try {
          const readResponse = await client.readResource(resource.uri);
          if (readResponse.result?.contents?.[0]) {
            const content = readResponse.result.contents[0];
            const textLength = content.text?.length || 0;
            console.log(`  Content length: ${textLength} characters`);
            console.log(`  MIME type: ${content.mimeType}`);
            
            // Show first 200 chars of content
            if (content.text && textLength > 0) {
              const preview = content.text.substring(0, 200);
              console.log(`  Preview: ${preview}${textLength > 200 ? '...' : ''}`);
            }
          }
        } catch (error) {
          console.error(`  Error reading ${resource.uri}:`, error.message);
        }
        console.log('');
      }
    }
    
    console.log('=== Test Complete ===');
    
  } catch (error) {
    console.error('Test failed:', error);
  } finally {
    await client.cleanup();
  }
}

// Run the test
runTest().catch(console.error);