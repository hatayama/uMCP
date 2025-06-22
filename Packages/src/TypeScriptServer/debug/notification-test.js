import { UnityClient } from '../dist/unity-client.js';

async function testNotifications() {
    console.log('=== Unity Notification Test ===');
    
    const unityClient = new UnityClient();
    
    try {
        // Connect to Unity
        console.log('1. Connecting to Unity...');
        await unityClient.connect();
        console.log('✓ Connected successfully!');
        
        // Register notification handler
        console.log('2. Registering notification handler...');
        unityClient.onNotification('commandsChanged', (params) => {
            console.log('🎉 Received commandsChanged notification!', params);
        });
        console.log('✓ Notification handler registered');
        
        // Test ping to ensure connection is working
        console.log('3. Testing ping...');
        const pingResult = await unityClient.ping('notification test');
        console.log('✓ Ping successful:', pingResult);
        
        // Keep listening for notifications
        console.log('4. Listening for notifications... (waiting 30 seconds)');
        console.log('   Please trigger a compile in Unity to test notification');
        
        await new Promise(resolve => setTimeout(resolve, 30000));
        
        console.log('5. Test completed');
        
    } catch (error) {
        console.error('✗ Test failed:', error.message);
    } finally {
        unityClient.disconnect();
    }
}

testNotifications(); 