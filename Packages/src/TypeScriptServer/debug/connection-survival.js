import { UnityDebugClient } from './unity-debug-client.js';

/**
 * TCP connection survival test.
 * Investigates the connection state before and after a Domain Reload.
 */
async function testConnectionSurvival() {
    console.log('=== TCP Connection Survival Test ===');
    
    const client = new UnityDebugClient();
    
    try {
        console.log('\n1. Establishing connection...');
        await client.connect();
        console.log('✓ Connected');
        
        console.log('\n2. Sending ping to verify connection...');
        const pingResult = await client.ping('Pre-compile ping');
        console.log(`✓ Ping response: ${pingResult}`);
        
        console.log('\n3. Starting compile (this will trigger Domain Reload)...');
        const compileStart = Date.now();
        
        // Long-running compile request (forced compilation)
        const compilePromise = client.compileProject(true);
        
        // Periodically check the connection status.
        let connectionChecks = 0;
        const checkInterval = setInterval(async () => {
            connectionChecks++;
            const elapsed = Date.now() - compileStart;
            console.log(`⏱️  [${elapsed}ms] Checking connection status... (Check #${connectionChecks})`);
            
            try {
                // Check connection status with another lightweight request.
                // Note: Multiple requests on the same socket are not normally possible, this is for testing purposes.
                console.log(`   Connection alive: ${client.socket && !client.socket.destroyed}`);
                console.log(`   Socket readable: ${client.socket?.readable}`);
                console.log(`   Socket writable: ${client.socket?.writable}`);
            } catch (err) {
                console.log(`   Connection check failed: ${err.message}`);
            }
        }, 1000);
        
        try {
            const compileResult = await compilePromise;
            clearInterval(checkInterval);
            
            const totalTime = Date.now() - compileStart;
            console.log(`\n✓ Compile completed after ${totalTime}ms`);
            console.log(`   Success: ${compileResult.success}`);
            
            console.log('\n4. Post-compile connection verification...');
            
            // Test if the connection is still alive after compilation.
            try {
                const postPingResult = await client.ping('Post-compile ping');
                console.log(`✓ Post-compile ping successful: ${postPingResult}`);
                console.log('🎉 TCP connection survived Domain Reload!');
            } catch (pingError) {
                console.log(`❌ Post-compile ping failed: ${pingError.message}`);
                console.log('💀 TCP connection did not survive Domain Reload');
            }
            
        } catch (compileError) {
            clearInterval(checkInterval);
            console.log(`❌ Compile failed: ${compileError.message}`);
        }
        
    } catch (error) {
        console.error(`❌ Test failed: ${error.message}`);
        
    } finally {
        console.log('\n5. Disconnecting...');
        client.disconnect();
        console.log('✓ Disconnected');
    }
}

console.log('Testing TCP connection survival during Domain Reload...');
testConnectionSurvival().catch(console.error);