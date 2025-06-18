import { DirectUnityClient } from './unity-test-client.js';

function showHelp() {
    console.log('=== Unity Connection Test ===');
    console.log('');
    console.log('Usage:');
    console.log('  node test/test-unity-connection.js [options]');
    console.log('');
    console.log('Options:');
    console.log('  --verbose, -v      詳細出力を表示');
    console.log('  --quick, -q        ping テストのみ実行');
    console.log('  --help, -h         このヘルプを表示');
    console.log('');
    console.log('Examples:');
    console.log('  node test/test-unity-connection.js           # 全機能テスト');
    console.log('  node test/test-unity-connection.js --quick   # pingテストのみ');
    console.log('  node test/test-unity-connection.js -v        # 詳細出力');
    console.log('');
}

async function testUnityConnection() {
    // コマンドライン引数の解析
    const args = process.argv.slice(2);
    
    // ヘルプ表示
    if (args.includes('--help') || args.includes('-h')) {
        showHelp();
        return;
    }
    
    const verbose = args.includes('--verbose') || args.includes('-v');
    const quickTest = args.includes('--quick') || args.includes('-q');
    
    console.log('=== Unity Connection Test ===');
    console.log(`Verbose: ${verbose ? 'ON' : 'OFF'}`);
    console.log(`Quick Test: ${quickTest ? 'ON' : 'OFF'}`);
    
    const client = new DirectUnityClient();
    
    try {
        console.log('\n1. Connecting to Unity...');
        await client.connect();
        console.log('✓ Connected successfully!');
        
        console.log('\n2. Testing ping...');
        const pingResponse = await client.ping('Hello from connection test!');
        console.log('✓ Ping response:', verbose ? JSON.stringify(pingResponse, null, 2) : pingResponse);
        
        if (quickTest) {
            console.log('\n✓ Quick test completed successfully!');
            return;
        }
        
        console.log('\n3. Testing compile...');
        const compileResponse = await client.compileProject(false);
        console.log('✓ Compile completed!');
        console.log(`  Success: ${compileResponse.success}`);
        console.log(`  Errors: ${compileResponse.errorCount}`);
        console.log(`  Warnings: ${compileResponse.warningCount}`);
        if (verbose) {
            console.log('  Full response:', JSON.stringify(compileResponse, null, 2));
        }
        
        console.log('\n4. Testing getLogs...');
        const logsResponse = await client.getLogs('All', 5);
        console.log('✓ Logs retrieved!');
        console.log(`  Found: ${logsResponse.logs.length} logs (total: ${logsResponse.totalCount})`);
        if (verbose) {
            console.log('  Recent logs:');
            logsResponse.logs.slice(0, 3).forEach((log, index) => {
                console.log(`    ${index + 1}. [${log.type}] ${log.message}`);
            });
        }
        
        console.log('\n✓ All connection tests passed!');
        
    } catch (error) {
        console.error('✗ Connection test failed:', error.message);
        process.exit(1);
    } finally {
        console.log('\n5. Disconnecting...');
        client.disconnect();
        console.log('✓ Disconnected');
    }
}

testUnityConnection().catch(console.error); 