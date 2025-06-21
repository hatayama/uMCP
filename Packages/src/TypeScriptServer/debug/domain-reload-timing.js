import { UnityDebugClient } from './unity-debug-client.js';

/**
 * Detailed analysis of Domain Reload timing.
 * Investigates TCP communication behavior while checking Unity-side logs.
 */
async function testDomainReloadTiming() {
    console.log('=== Domain Reload Timing Analysis ===');
    console.log('Please check the following logs in the Unity Console:');
    console.log('- McpServerController.OnBeforeAssemblyReload');
    console.log('- Dispose related logs for McpBridgeServer');
    console.log('- TCP connection related logs');
    console.log('');
    
    const client = new UnityDebugClient();
    
    try {
        console.log('1. Connecting to Unity...');
        await client.connect();
        console.log('✓ Connected - check Unity logs for connection message');
        
        // Wait a bit to allow for log checking.
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
        
        // Execute compilation.
        try {
            const compileResult = await client.compileProject(true);
            const totalTime = Date.now() - compileStart;
            
            console.log(`✓ Compile response received after ${totalTime}ms`);
            console.log(`  Success: ${compileResult.success}`);
            console.log(`  Completed at: ${compileResult.completedAt}`);
            
            console.log('\n--- PLEASE CHECK UNITY CONSOLE LOGS ---');
            console.log('Important points to check:');
            console.log('1. Was OnBeforeAssemblyReload executed?');
            console.log('2. Was the "Stopping Unity MCP Server" log output?');
            console.log('3. Was mcpServer.Dispose() called?');
            console.log('4. At what point was the response sent?');
            
        } catch (error) {
            console.log(`❌ Compile failed: ${error.message}`);
            console.log('This might indicate Domain Reload interrupted the connection');
        }
        
        // Connection test.
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
        console.log('Please check the following in the Unity Console:');
        console.log('1. Execution timing of McpServerController.OnBeforeAssemblyReload');
        console.log('2. Completion status of the Dispose() process');
        console.log('3. Operating status of HandleClient()');
        console.log('4. Actual disconnection timing of the TCP connection');
    }
}

console.log('Starting Domain Reload timing analysis...');
console.log('Please carefully check the Unity Console logs during this execution');
console.log('');
testDomainReloadTiming().catch(console.error);