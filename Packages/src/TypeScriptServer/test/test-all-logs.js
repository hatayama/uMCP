import { DirectUnityClient } from './unity-test-client.js';

function showHelp() {
    console.log('=== Unity All Logs Test ===');
    console.log('');
    console.log('Usage:');
    console.log('  node test/test-all-logs.js [options]');
    console.log('');
    console.log('Options:');
    console.log('  --count, -c <num>  取得する最大ログ数 (default: 100)');
    console.log('  --stats, -s        統計情報のみ表示');
    console.log('  --help, -h         このヘルプを表示');
    console.log('');
    console.log('Examples:');
    console.log('  node test/test-all-logs.js              # 全ログ100件取得+統計');
    console.log('  node test/test-all-logs.js -c 200       # 全ログ200件取得');
    console.log('  node test/test-all-logs.js --stats      # 統計情報のみ表示');
    console.log('');
}

async function testAllLogs() {
    // コマンドライン引数の解析
    const args = process.argv.slice(2);
    
    // ヘルプ表示
    if (args.includes('--help') || args.includes('-h')) {
        showHelp();
        return;
    }
    
    // ログ数の取得
    let maxCount = 100;
    const countIndex = args.findIndex(arg => arg === '--count' || arg === '-c');
    if (countIndex !== -1 && args[countIndex + 1]) {
        maxCount = parseInt(args[countIndex + 1], 10) || 100;
    }
    
    // 統計のみ表示フラグ
    const statsOnly = args.includes('--stats') || args.includes('-s');
    
    console.log('=== Unity All Logs Test ===');
    console.log(`Max Count: ${maxCount}`);
    console.log(`Stats Only: ${statsOnly ? 'ON' : 'OFF'}`);
    
    const client = new DirectUnityClient();
    
    try {
        console.log('\n1. Connecting to Unity...');
        await client.connect();
        console.log('✓ Connected successfully!');
        
        console.log(`\n2. Getting All logs (max ${maxCount})...`);
        const allLogs = await client.getLogs('All', maxCount);
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
        console.log(`Total logs in Unity: ${allLogs.totalCount}`);
        console.log(`Retrieved logs: ${allLogs.logs.length}`);
        console.log(`Error logs: ${logCounts.Error}`);
        console.log(`Warning logs: ${logCounts.Warning}`);
        console.log(`Info logs: ${logCounts.Log}`);
        console.log(`Other logs: ${logCounts.Other}`);
        
        if (!statsOnly) {
            // エラーログを表示
            if (logCounts.Error > 0) {
                console.log('\n--- RECENT ERROR LOGS ---');
                const errorLogs = allLogs.logs.filter(log => log.type === 'Error');
                errorLogs.slice(0, 5).forEach((log, index) => {
                    console.log(`${index + 1}. [${log.timestamp}] ${log.message}`);
                    if (log.file && log.file !== './Runtime/Export/Debug/Debug.bindings.h') {
                        console.log(`   File: ${log.file}`);
                    }
                    console.log('');
                });
            }
            
            // 警告ログを表示
            if (logCounts.Warning > 0) {
                console.log('\n--- RECENT WARNING LOGS ---');
                const warningLogs = allLogs.logs.filter(log => log.type === 'Warning');
                warningLogs.slice(0, 5).forEach((log, index) => {
                    console.log(`${index + 1}. [${log.timestamp}] ${log.message}`);
                    if (log.file && log.file !== './Runtime/Export/Debug/Debug.bindings.h') {
                        console.log(`   File: ${log.file}`);
                    }
                    console.log('');
                });
            }
            
            // 情報ログを表示
            if (logCounts.Log > 0) {
                console.log('\n--- RECENT INFO LOGS ---');
                const infoLogs = allLogs.logs.filter(log => log.type === 'Log');
                infoLogs.slice(0, 3).forEach((log, index) => {
                    console.log(`${index + 1}. [${log.timestamp}] ${log.message}`);
                    console.log('');
                });
            }
        }
        
    } catch (error) {
        console.error('✗ Get all logs failed:', error.message);
    } finally {
        console.log('\n3. Disconnecting...');
        client.disconnect();
        console.log('✓ Disconnected');
    }
}

testAllLogs().catch(console.error); 