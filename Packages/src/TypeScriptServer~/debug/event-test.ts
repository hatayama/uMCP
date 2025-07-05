import { UnityDebugClient } from './unity-debug-client.js';
import { PingResult, CompileResult } from './types.js';

interface NotificationEvent {
    method: string;
    params: any;
    timestamp: string;
}

interface ResponseEvent {
    id: number;
    method: string;
    success: boolean;
}

async function testEventNotifications(): Promise<void> {
    const client = new UnityDebugClient();
    
    try {
        console.log('🚀 Starting event notification test...');
        
        // Connect to Unity
        await client.connect();
        
        // Set up notification listener
        // Note: Accessing private socket property for testing purposes
        const socket = (client as any).socket;
        if (socket) {
            socket.on('data', (data: Buffer) => {
                const messages = data.toString().split('\n').filter(msg => msg.trim());
                
                for (const message of messages) {
                    try {
                        const parsed = JSON.parse(message);
                        
                        // Check if it's a notification (no id property)
                        if (!parsed.id && parsed.method) {
                            const notification: NotificationEvent = {
                                method: parsed.method,
                                params: parsed.params,
                                timestamp: new Date().toISOString()
                            };
                            console.log('🔔 Received notification:', notification);
                        } else if (parsed.id) {
                            const response: ResponseEvent = {
                                id: parsed.id,
                                method: parsed.method || 'unknown',
                                success: !parsed.error
                            };
                            console.log('📨 Received response:', response);
                        }
                    } catch (error) {
                        console.log('📝 Raw message:', message);
                    }
                }
            });
        }
        
        // Send ping to test connection
        console.log('📡 Sending ping...');
        const pingResult: PingResult = await client.ping('Event test connection');
        console.log('✅ Ping successful:', pingResult);
        
        // Trigger compile to generate event
        console.log('🔨 Triggering compile to test event notification...');
        const compileResult: CompileResult = await client.compileProject(true);
        console.log('✅ Compile completed:', compileResult);
        
        // Wait for potential notifications
        console.log('⏳ Waiting for notifications for 5 seconds...');
        await new Promise<void>(resolve => setTimeout(resolve, 5000));
        
        console.log('✅ Event test completed');
        
    } catch (error) {
        console.error('❌ Test failed:', (error as Error).message);
    } finally {
        client.disconnect();
    }
}

// Run the test
testEventNotifications().catch(console.error);