import { UnityDebugClient } from './unity-debug-client.js';
import { PingResult, CompileResult, GetLogsResult } from './types.js';

function showHelp(): void {
    console.log('=== Unity Connection Test ===');
    console.log('');
    console.log('Usage:');
    console.log('  tsx debug/connection-check.ts [options]');
    console.log('');
    console.log('Options:');
    console.log('  --verbose, -v      Show verbose output.');
    console.log('  --quick, -q        Run ping test only.');
    console.log('  --help, -h         Show this help message.');
    console.log('');
    console.log('Examples:');
    console.log('  tsx debug/connection-check.ts           # Run all tests.');
    console.log('  tsx debug/connection-check.ts --quick   # Run ping test only.');
    console.log('  tsx debug/connection-check.ts -v        # Show verbose output.');
    console.log('');
}

async function testUnityConnection(): Promise<void> {
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
        const pingResponse: PingResult = await client.ping('Hello from connection test!');
        console.log('✓ Ping response:', verbose ? JSON.stringify(pingResponse, null, 2) : pingResponse);
        
        if (quickTest) {
            console.log('\n✓ Quick test completed successfully!');
            return;
        }
        
        console.log('\n3. Testing compile...');
        const compileResponse: CompileResult = await client.compileProject(false);
        console.log('✓ Compile completed!');
        console.log(`  Success: ${compileResponse.Success}`);
        console.log(`  Errors: ${compileResponse.ErrorCount}`);
        console.log(`  Warnings: ${compileResponse.WarningCount}`);
        if (verbose) {
            console.log('  Full response:', JSON.stringify(compileResponse, null, 2));
        }
        
        console.log('\n4. Testing getLogs...');
        const logsResponse: GetLogsResult = await client.getLogs('All', 5);
        console.log('✓ Logs retrieved!');
        console.log(`  Found: ${logsResponse.logs.length} logs (total: ${logsResponse.totalCount})`);
        if (verbose) {
            console.log('  Recent logs:');
            logsResponse.logs.slice(0, 3).forEach((log, index) => {
                console.log(`    ${index + 1}. [${log.logType}] ${log.message}`);
            });
        }
        
        console.log('\n✓ All connection tests passed!');
        
    } catch (error) {
        console.error('✗ Connection test failed:', (error as Error).message);
        process.exit(1);
    } finally {
        console.log('\n5. Disconnecting...');
        client.disconnect();
        console.log('✓ Disconnected');
    }
}

testUnityConnection().catch(console.error);