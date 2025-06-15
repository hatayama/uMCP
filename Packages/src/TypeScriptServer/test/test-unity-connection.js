import { UnityClient } from './dist/unity-client.js';

async function testUnityConnection() {
    console.log('=== Unity Connection Test ===');
    
    const client = new UnityClient();
    
    try {
        console.log('1. Connecting to Unity...');
        await client.connect();
        console.log('✓ Connected successfully!');
        
        console.log('\n2. Testing ping...');
        const pingResponse = await client.ping('Hello from direct test!');
        console.log('✓ Ping response:', pingResponse);
        
        console.log('\n3. Testing compile...');
        const compileResponse = await client.compileProject(false);
        console.log('✓ Compile response:', JSON.stringify(compileResponse, null, 2));
        
        console.log('\n4. Testing getLogs...');
        const logsResponse = await client.getLogs('All', 5);
        console.log('✓ Logs response:', JSON.stringify(logsResponse, null, 2));
        
    } catch (error) {
        console.error('✗ Error:', error.message);
    } finally {
        console.log('\n5. Disconnecting...');
        client.disconnect();
        console.log('✓ Disconnected');
    }
}

testUnityConnection().catch(console.error); 