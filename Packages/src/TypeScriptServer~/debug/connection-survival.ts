import { UnityDebugClient } from './unity-debug-client.js';
import { PingResult } from './types.js';

function showHelp(): void {
    console.log('=== Unity Connection Survival Test ===');
    console.log('');
    console.log('Usage:');
    console.log('  tsx debug/connection-survival.ts [options]');
    console.log('');
    console.log('Options:');
    console.log('  --duration, -d <seconds>  Test duration in seconds (default: 60)');
    console.log('  --interval, -i <seconds>  Ping interval in seconds (default: 5)');
    console.log('  --help, -h                Show this help message');
    console.log('');
    console.log('Examples:');
    console.log('  tsx debug/connection-survival.ts              # Run for 60 seconds, ping every 5 seconds');
    console.log('  tsx debug/connection-survival.ts -d 120 -i 10 # Run for 120 seconds, ping every 10 seconds');
    console.log('');
}

async function testConnectionSurvival(): Promise<void> {
    const args = process.argv.slice(2);
    
    if (args.includes('--help') || args.includes('-h')) {
        showHelp();
        return;
    }
    
    let duration = 60; // seconds
    const durationIndex = args.findIndex(arg => arg === '--duration' || arg === '-d');
    if (durationIndex !== -1 && args[durationIndex + 1]) {
        duration = parseInt(args[durationIndex + 1], 10) || 60;
    }
    
    let interval = 5; // seconds
    const intervalIndex = args.findIndex(arg => arg === '--interval' || arg === '-i');
    if (intervalIndex !== -1 && args[intervalIndex + 1]) {
        interval = parseInt(args[intervalIndex + 1], 10) || 5;
    }
    
    console.log('=== Unity Connection Survival Test ===');
    console.log(`Duration: ${duration} seconds`);
    console.log(`Ping interval: ${interval} seconds`);
    
    const client = new UnityDebugClient();
    let pingCount = 0;
    let successCount = 0;
    let failureCount = 0;
    
    try {
        console.log('\n1. Connecting to Unity...');
        await client.connect();
        console.log('✓ Connected successfully!');
        
        console.log(`\n2. Starting survival test for ${duration} seconds...`);
        const startTime = Date.now();
        const endTime = startTime + (duration * 1000);
        
        while (Date.now() < endTime) {
            try {
                pingCount++;
                const pingMessage = `Survival test ping #${pingCount}`;
                const pingResult: PingResult = await client.ping(pingMessage);
                successCount++;
                
                const elapsed = Math.floor((Date.now() - startTime) / 1000);
                console.log(`[${elapsed}s] Ping #${pingCount}: ✓ Success - ${pingResult.message}`);
                
            } catch (error) {
                failureCount++;
                const elapsed = Math.floor((Date.now() - startTime) / 1000);
                console.log(`[${elapsed}s] Ping #${pingCount}: ✗ Failed - ${(error as Error).message}`);
            }
            
            // Wait for the next ping
            await new Promise<void>(resolve => setTimeout(resolve, interval * 1000));
        }
        
        console.log('\n=== SURVIVAL TEST RESULTS ===');
        console.log(`Total pings: ${pingCount}`);
        console.log(`Successful: ${successCount}`);
        console.log(`Failed: ${failureCount}`);
        console.log(`Success rate: ${((successCount / pingCount) * 100).toFixed(1)}%`);
        
        if (failureCount === 0) {
            console.log('\n🎉 Connection survival test passed!');
        } else {
            console.log('\n⚠️ Connection had some failures during test');
        }
        
    } catch (error) {
        console.error('✗ Connection survival test failed:', (error as Error).message);
    } finally {
        console.log('\n3. Disconnecting...');
        client.disconnect();
        console.log('✓ Disconnected');
    }
}

testConnectionSurvival().catch(console.error);