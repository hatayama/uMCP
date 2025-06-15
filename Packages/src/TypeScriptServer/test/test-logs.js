import { UnityClient } from './dist/unity-client.js';

async function testGetLogs() {
    console.log('=== Unity Logs Test ===');
    
    const client = new UnityClient();
    
    try {
        console.log('1. Connecting to Unity...');
        await client.connect();
        console.log('✓ Connected successfully!');
        
        // 全ログを取得
        console.log('\n2. Getting All logs (max 10)...');
        const allLogs = await client.getLogs('All', 10);
        console.log(`✓ Found ${allLogs.logs.length} logs (total: ${allLogs.totalCount})`);
        
        if (allLogs.logs.length > 0) {
            console.log('\n--- ALL LOGS ---');
            allLogs.logs.forEach((log, index) => {
                console.log(`${index + 1}. [${log.timestamp}] ${log.type}: ${log.message}`);
                if (log.file && log.file !== './Runtime/Export/Debug/Debug.bindings.h') {
                    console.log(`   File: ${log.file}`);
                }
            });
        }
        
        // エラーログのみ取得
        console.log('\n3. Getting Error logs only...');
        const errorLogs = await client.getLogs('Error', 5);
        console.log(`✓ Found ${errorLogs.logs.length} error logs`);
        
        if (errorLogs.logs.length > 0) {
            console.log('\n--- ERROR LOGS ---');
            errorLogs.logs.forEach((log, index) => {
                console.log(`${index + 1}. [${log.timestamp}] ${log.message}`);
                if (log.file && log.file !== './Runtime/Export/Debug/Debug.bindings.h') {
                    console.log(`   File: ${log.file}`);
                }
            });
        }
        
        // 警告ログのみ取得
        console.log('\n4. Getting Warning logs only...');
        const warningLogs = await client.getLogs('Warning', 5);
        console.log(`✓ Found ${warningLogs.logs.length} warning logs`);
        
        if (warningLogs.logs.length > 0) {
            console.log('\n--- WARNING LOGS ---');
            warningLogs.logs.forEach((log, index) => {
                console.log(`${index + 1}. [${log.timestamp}] ${log.message}`);
                if (log.file && log.file !== './Runtime/Export/Debug/Debug.bindings.h') {
                    console.log(`   File: ${log.file}`);
                }
            });
        }
        
    } catch (error) {
        console.error('✗ Get logs failed:', error.message);
    } finally {
        console.log('\n5. Disconnecting...');
        client.disconnect();
        console.log('✓ Disconnected');
    }
}

testGetLogs().catch(console.error); 