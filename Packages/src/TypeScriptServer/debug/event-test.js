import { UnityDebugClient } from './unity-debug-client.js';

async function testEventNotifications() {
    const client = new UnityDebugClient();
    
    try {
        console.log('🚀 Starting event notification test...');
        
        // Connect to Unity
        await client.connect();
        
        // Set up notification listener
        client.socket.on('data', (data) => {
            const messages = data.toString().split('\n').filter(msg => msg.trim());
            
            for (const message of messages) {
                try {
                    const parsed = JSON.parse(message);
                    
                    // Check if it's a notification (no id property)
                    if (!parsed.id && parsed.method) {
                        console.log('🔔 Received notification:', {
                            method: parsed.method,
                            params: parsed.params,
                            timestamp: new Date().toISOString()
                        });
                    } else if (parsed.id) {
                        console.log('📨 Received response:', {
                            id: parsed.id,
                            method: parsed.method || 'unknown',
                            success: !parsed.error
                        });
                    }
                } catch (error) {
                    console.log('📝 Raw message:', message);
                }
            }
        });
        
        // Send ping to test connection
        console.log('📡 Sending ping...');
        const pingResult = await client.ping('Event test connection');
        console.log('✅ Ping successful:', pingResult);
        
        // Trigger compile to generate event
        console.log('🔨 Triggering compile to test event notification...');
        const compileResult = await client.compileProject(true);
        console.log('✅ Compile completed:', compileResult);
        
        // Wait for potential notifications
        console.log('⏳ Waiting for notifications for 5 seconds...');
        await new Promise(resolve => setTimeout(resolve, 5000));
        
        console.log('✅ Event test completed');
        
    } catch (error) {
        console.error('❌ Test failed:', error.message);
    } finally {
        client.disconnect();
    }
}

// Run the test
testEventNotifications().catch(console.error); 