import { DirectUnityClient } from './unity-test-client.js';

/**
 * Domain Reload タイミング詳細調査
 * Unity側のログを確認しながらTCP通信の挙動を調査
 */
async function testDomainReloadTiming() {
    console.log('=== Domain Reload Timing Analysis ===');
    console.log('Unity Console で以下のログを確認してください:');
    console.log('- McpServerController.OnBeforeAssemblyReload');
    console.log('- McpBridgeServer の Dispose 関連ログ');
    console.log('- TCP接続関連のログ');
    console.log('');
    
    const client = new DirectUnityClient();
    
    try {
        console.log('1. Connecting to Unity...');
        await client.connect();
        console.log('✓ Connected - check Unity logs for connection message');
        
        // 少し待機してログを確認させる
        await new Promise(resolve => setTimeout(resolve, 1000));
        
        console.log('\n2. Starting compile (watch Unity Console for Domain Reload logs)...');
        console.log('Expected Unity log sequence:');
        console.log('  a) CompileCommand request received');
        console.log('  b) McpServerController.OnBeforeAssemblyReload');
        console.log('  c) Stopping Unity MCP Server...');
        console.log('  d) Domain Reload execution');
        console.log('  e) OnAfterAssemblyReload (server restart)');
        console.log('');
        
        const compileStart = Date.now();
        
        // コンパイル実行
        try {
            const compileResult = await client.compileProject(true);
            const totalTime = Date.now() - compileStart;
            
            console.log(`✓ Compile response received after ${totalTime}ms`);
            console.log(`  Success: ${compileResult.success}`);
            console.log(`  Completed at: ${compileResult.completedAt}`);
            
            console.log('\n--- PLEASE CHECK UNITY CONSOLE LOGS ---');
            console.log('重要な確認ポイント:');
            console.log('1. OnBeforeAssemblyReload が実行されたか？');
            console.log('2. "Stopping Unity MCP Server" ログが出力されたか？');
            console.log('3. mcpServer.Dispose() が呼ばれたか？');
            console.log('4. どのタイミングでレスポンスが送信されたか？');
            
        } catch (error) {
            console.log(`❌ Compile failed: ${error.message}`);
            console.log('This might indicate Domain Reload interrupted the connection');
        }
        
        // 接続テスト
        console.log('\n3. Testing post-compile connection...');
        try {
            const pingResult = await client.ping('Post-compile test');
            console.log(`✓ Post-compile ping successful: ${pingResult}`);
        } catch (pingError) {
            console.log(`❌ Post-compile ping failed: ${pingError.message}`);
            console.log('This confirms the connection was closed after Domain Reload');
        }
        
    } catch (error) {
        console.error(`❌ Test failed: ${error.message}`);
        
    } finally {
        console.log('\n4. Disconnecting...');
        client.disconnect();
        console.log('✓ Disconnected');
        
        console.log('\n=== UNITY LOG ANALYSIS NEEDED ===');
        console.log('Unity Console で以下を確認してください:');
        console.log('1. McpServerController.OnBeforeAssemblyReload の実行タイミング');
        console.log('2. Dispose() 処理の完了状況');
        console.log('3. HandleClient() の動作状況');
        console.log('4. TCP接続の実際の切断タイミング');
    }
}

console.log('Starting Domain Reload timing analysis...');
console.log('この実行中にUnity Consoleのログを注意深く確認してください');
console.log('');
testDomainReloadTiming().catch(console.error);