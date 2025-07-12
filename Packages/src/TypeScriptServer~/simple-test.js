#!/usr/bin/env node

import { spawn } from 'child_process';

async function testMcpResources() {
  console.log('Testing MCP Resources...');
  
  const server = spawn('node', ['dist/server.bundle.js'], {
    stdio: ['pipe', 'pipe', 'pipe'],
    cwd: '/Users/a12115/work/oss/uLoopMCP/Packages/src/TypeScriptServer~'
  });

  let responseBuffer = '';
  
  server.stdout.on('data', (data) => {
    responseBuffer += data.toString();
    console.log('Response:', data.toString().trim());
  });

  server.stderr.on('data', (data) => {
    console.error('Error:', data.toString());
  });

  // Initialize first
  const initRequest = {
    jsonrpc: '2.0',
    id: 1,
    method: 'initialize',
    params: {
      protocolVersion: '2024-11-05',
      capabilities: {
        resources: { subscribe: true, listChanged: true }
      },
      clientInfo: {
        name: 'test-client',
        version: '1.0.0'
      }
    }
  };

  console.log('Sending initialize...');
  server.stdin.write(JSON.stringify(initRequest) + '\n');

  // Wait for init response
  await new Promise(resolve => setTimeout(resolve, 2000));

  // Test resources/list
  const listRequest = {
    jsonrpc: '2.0',
    id: 2,
    method: 'resources/list',
    params: {}
  };

  console.log('Sending resources/list...');
  server.stdin.write(JSON.stringify(listRequest) + '\n');

  // Wait for response
  await new Promise(resolve => setTimeout(resolve, 3000));

  server.kill();
}

testMcpResources().catch(console.error);