import { UnityClient } from './dist/unity-client.js';

async function testAllLogs() {
    console.log('=== Unity All Logs Test ===');
    
    const client = new UnityClient();
    
    try {
        console.log('1. Connecting to Unity...');
        await client.connect();
        console.log('✓ Connected successfully!');
        
        // 全ログを大量に取得（最大100件）
        console.log('\n2. Getting All logs (max 100)...');
        const allLogs = await client.getLogs('All', 100);
        console.log(`✓ Found ${allLogs.logs.length} logs (total: ${allLogs.totalCount})`);
        
        // ログタイプ別の集計
        const logCounts = {
            Error: 0,
            Warning: 0,
            Log: 0,
            Other: 0
        };
        
        allLogs.logs.forEach(log => {
            if (logCounts[log.type] !== undefined) {
                logCounts[log.type]++;
            } else {
                logCounts.Other++;
            }
        });
        
        console.log('\n--- LOG STATISTICS ---');
        console.log(`Total logs: ${allLogs.totalCount}`);
        console.log(`Error logs: ${logCounts.Error}`);
        console.log(`Warning logs: ${logCounts.Warning}`);
        console.log(`Info logs: ${logCounts.Log}`);
        console.log(`Other logs: ${logCounts.Other}`);
        
        // 最新の警告ログを表示
        console.log('\n--- RECENT WARNING LOGS ---');
        const warningLogs = allLogs.logs.filter(log => log.type === 'Warning');
        warningLogs.slice(0, 10).forEach((log, index) => {
            console.log(`${index + 1}. [${log.timestamp}] ${log.message}`);
            if (log.file && log.file !== './Runtime/Export/Debug/Debug.bindings.h') {
                console.log(`   File: ${log.file}`);
            }
        });
        
        // 最新の情報ログを表示
        console.log('\n--- RECENT INFO LOGS ---');
        const infoLogs = allLogs.logs.filter(log => log.type === 'Log');
        infoLogs.slice(0, 10).forEach((log, index) => {
            console.log(`${index + 1}. [${log.timestamp}] ${log.message}`);
        });
        
    } catch (error) {
        console.error('✗ Get all logs failed:', error.message);
    } finally {
        console.log('\n3. Disconnecting...');
        client.disconnect();
        console.log('✓ Disconnected');
    }
}

testAllLogs().catch(console.error); 