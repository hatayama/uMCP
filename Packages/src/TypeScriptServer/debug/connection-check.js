import { UnityDebugClient } from './unity-debug-client.js';

function showHelp() {
    console.log('=== Unity Connection Test ===');
    console.log('');
    console.log('Usage:');
    console.log('  node debug/connection-check.js [options]');
    console.log('');
    console.log('Options:');
    console.log('  --verbose, -v      Show verbose output.');
    console.log('  --quick, -q        Run ping test only.');
    console.log('  --help, -h         Show this help message.');
    console.log('');
    console.log('Examples:');
    console.log('  node debug/connection-check.js           # Run all tests.');
    console.log('  node debug/connection-check.js --quick   # Run ping test only.');
    console.log('  node debug/connection-check.js -v        # Show verbose output.');
    console.log('');
}

async function testUnityConnection() {
    // Parse command line arguments.
    const args = process.argv.slice(2);
    
    // Show help.
    if (args.includes('--help') || args.includes('-h')) {
        showHelp();
        return;
    }
    
    const verbose = args.includes('--verbose') || args.includes('-v');
    const quickTest = args.includes('--quick') || args.includes('-q');
    
    console.log('=== Unity Connection Test ===');
    console.log(`Verbose: ${verbose ? 'ON' : 'OFF'}`);
    console.log(`Quick Test: ${quickTest ? 'ON' : 'OFF'}`);
    
    const client = new UnityDebugClient();
    
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