import { UnityDebugClient } from './unity-debug-client.js';
import { PingResult } from './types.js';

async function testNotifications(): Promise<void> {
    console.log('=== Unity Notification Test ===');
    
    const client = new UnityDebugClient();
    
    try {
        // Connect to Unity
        console.log('1. Connecting to Unity...');
        await client.connect();
        console.log('✓ Connected successfully!');
        
        // Set up notification listener
        console.log('2. Setting up notification listener...');
        const socket = (client as any).socket;
        if (socket) {
            socket.on('data', (data: Buffer) => {
                const messages = data.toString().split('\n').filter(msg => msg.trim());
                
                for (const message of messages) {
                    try {
                        const parsed = JSON.parse(message);
                        
                        // Check if it's a notification (no id property)
                        if (!parsed.id && parsed.method) {
                            console.log('🎉 Received notification:', {
                                method: parsed.method,
                                params: parsed.params,
                                timestamp: new Date().toISOString()
                            });
                        }
                    } catch (error) {
                        // Ignore parsing errors for non-JSON messages
                    }
                }
            });
        }
        console.log('✓ Notification listener set up');
        
        // Test ping to ensure connection is working
        console.log('3. Testing ping...');
        const pingResult: PingResult = await client.ping('notification test');
        console.log('✓ Ping successful:', pingResult);
        
        // Keep listening for notifications
        console.log('4. Listening for notifications... (waiting 30 seconds)');
        console.log('   Please trigger a compile in Unity to test notification');
        
        await new Promise<void>(resolve => setTimeout(resolve, 30000));
        
        console.log('✓ Notification test completed');
        
    } catch (error) {
        console.error('✗ Notification test failed:', (error as Error).message);
    } finally {
        console.log('5. Disconnecting...');
        client.disconnect();
        console.log('✓ Disconnected');
    }
}

testNotifications().catch(console.error);