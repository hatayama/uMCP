#!/usr/bin/env node

import { spawn } from 'child_process';
import { fileURLToPath } from 'url';
import { dirname, join } from 'path';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

const serverPath = join(__dirname, 'Packages/src/TypeScriptServer/dist/server.bundle.js');

console.log('🧪 カスタムコマンドをテスト中...');

const server = spawn('node', [serverPath], {
  stdio: ['pipe', 'pipe', 'pipe']
});

server.stderr.on('data', (data) => {
  // サーバーログは非表示
});

server.stdout.on('data', (data) => {
  const response = data.toString().trim();
  if (!response) return;
  
  try {
    const parsed = JSON.parse(response);
    
    if (parsed.id === 1) {
      console.log('✅ 初期化成功');
      // unity-helloworldを実行
      const testMessage = {
        jsonrpc: "2.0",
        id: 2,
        method: "tools/call",
        params: {
          name: "unity-helloworld",
          arguments: {}
        }
      };
      console.log('🔧 unity-helloworldを実行中...');
      server.stdin.write(JSON.stringify(testMessage) + '\n');
    }
    
    if (parsed.id === 2) {
      console.log('\n🎊 unity-helloworld結果:');
      console.log(JSON.stringify(parsed.result, null, 2));
      
      // unity-getprojectinfoを実行
      const projectInfoMessage = {
        jsonrpc: "2.0",
        id: 3,
        method: "tools/call",
        params: {
          name: "unity-getprojectinfo",
          arguments: {}
        }
      };
      console.log('\n🔧 unity-getprojectinfoを実行中...');
      server.stdin.write(JSON.stringify(projectInfoMessage) + '\n');
    }
    
    if (parsed.id === 3) {
      console.log('\n🎊 unity-getprojectinfo結果:');
      console.log(JSON.stringify(parsed.result, null, 2));
      
      setTimeout(() => {
        server.kill();
        process.exit(0);
      }, 1000);
    }
    
  } catch (error) {
    console.log('📄 生データ:', response);
  }
});

// 初期化メッセージを送信
const initMessage = {
  jsonrpc: "2.0",
  id: 1,
  method: "initialize",
  params: {
    protocolVersion: "2024-11-05",
    capabilities: { tools: {} },
    clientInfo: { name: "test-client", version: "1.0.0" }
  }
};

server.stdin.write(JSON.stringify(initMessage) + '\n');

setTimeout(() => {
  console.log('⏰ タイムアウト');
  server.kill();
  process.exit(1);
}, 15000); 