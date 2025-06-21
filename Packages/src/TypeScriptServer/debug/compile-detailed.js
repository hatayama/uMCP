import { UnityDebugClient } from './unity-debug-client.js';

/**
 * Detailed test for the compile request.
 * Investigates response reception timing and behavior before/after Domain Reload.
 */
async function testCompileDetailed() {
    console.log('=== Unity Compile Response Analysis ===');
    console.log(`Test started at: ${new Date().toISOString()}`);
    
    const client = new UnityDebugClient();
    
    try {
        console.log('\n1. Connecting to Unity...');
        const connectStart = Date.now();
        await client.connect();
        const connectEnd = Date.now();
        console.log(`✓ Connected successfully! (${connectEnd - connectStart}ms)`);
        
        console.log('\n2. Sending compile request...');
        const requestStart = Date.now();
        console.log(`Request sent at: ${new Date(requestStart).toISOString()}`);
        
        // Get the force recompile flag from command line arguments.
        const args = process.argv.slice(2);
        const forceRecompile = args.includes('--force') || args.includes('-f');
        
        console.log(`Force Recompile: ${forceRecompile ? 'ON' : 'OFF'}`);
        
        // Set a long timeout to wait for the response.
        const compilePromise = client.compileProject(forceRecompile);
        
        // Periodically check the status.
        const statusInterval = setInterval(() => {
            const elapsed = Date.now() - requestStart;
            console.log(`⏱️  Waiting for response... (${elapsed}ms elapsed)`);
        }, 1000);
        
        let compileResult;
        try {
            compileResult = await compilePromise;
            clearInterval(statusInterval);
            
            const responseEnd = Date.now();
            const totalTime = responseEnd - requestStart;
            
            console.log(`\n✓ Response received! (Total time: ${totalTime}ms)`);
            console.log(`Response received at: ${new Date(responseEnd).toISOString()}`);
            
            console.log('\n--- RESPONSE DETAILS ---');
            console.log(`Success: ${compileResult.success}`);
            console.log(`Errors: ${compileResult.errorCount}`);
            console.log(`Warnings: ${compileResult.warningCount}`);
            console.log(`Completed at: ${compileResult.completedAt}`);
            console.log(`Full response:`, JSON.stringify(compileResult, null, 2));
            
            // Check consistency of the response content.
            if (compileResult.completedAt) {
                const unityTime = new Date(compileResult.completedAt);
                const requestTime = new Date(requestStart);
                const responseTime = new Date(responseEnd);
                
                console.log('\n--- TIMING ANALYSIS ---');
                console.log(`Request sent: ${requestTime.toISOString()}`);
                console.log(`Unity completed: ${unityTime.toISOString()}`);
                console.log(`Response received: ${responseTime.toISOString()}`);
                
                const unityProcessTime = unityTime.getTime() - requestTime.getTime();
                const networkDelay = responseTime.getTime() - unityTime.getTime();
                
                console.log(`Unity processing time: ${unityProcessTime}ms`);
                console.log(`Network/transmission delay: ${networkDelay}ms`);
                
                if (networkDelay < 0) {
                    console.log('⚠️  WARNING: Negative network delay - possible clock synchronization issue');
                }
                
                if (unityProcessTime > 5000) {
                    console.log('🔄 Long processing time detected - Domain Reload likely occurred');
                } else {
                    console.log('⚡ Fast processing - may have completed before Domain Reload');
                }
            }
            
        } catch (timeoutError) {
            clearInterval(statusInterval);
            const timeoutTime = Date.now() - requestStart;
            console.log(`\n❌ Request failed after ${timeoutTime}ms`);
            console.log(`Error: ${timeoutError.message}`);
            
            if (timeoutError.message.includes('timeout')) {
                console.log('🔄 Timeout suggests Domain Reload interrupted the connection');
            }
            throw timeoutError;
        }
        
    } catch (error) {
        console.error('\n❌ Test failed:', error.message);
        
        // Analyze the type of error.
        if (error.message.includes('connection')) {
            console.log('🔌 Connection-related error - server may have stopped');
        } else if (error.message.includes('timeout')) {
            console.log('⏰ Timeout error - request took too long');
        } else {
            console.log('❓ Unknown error type');
        }
        
    } finally {
        console.log('\n3. Disconnecting...');
        client.disconnect();
        console.log('✓ Disconnected');
        console.log(`\nTest completed at: ${new Date().toISOString()}`);
    }
}

console.log('Starting detailed compile response analysis...');
testCompileDetailed().catch(console.error);