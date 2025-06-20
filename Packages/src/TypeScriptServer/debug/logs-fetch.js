import { UnityDebugClient } from './unity-debug-client.js';

function showHelp() {
    console.log('=== Unity Logs Test ===');
    console.log('');
    console.log('Usage:');
    console.log('  node test/test-logs.js [options]');
    console.log('');
    console.log('Options:');
    console.log('  --type, -t <type>    ログタイプを指定 (All, Error, Warning, Log)');
    console.log('  --count, -c <num>    取得する最大ログ数 (default: 10)');
    console.log('  --search, -s <text>  メッセージ内で検索するテキスト');
    console.log('  --no-stack           スタックトレースを非表示');
    console.log('  --help, -h           このヘルプを表示');
    console.log('');
    console.log('Examples:');
    console.log('  node test/test-logs.js                      # 全ログ10件取得（スタックトレース表示）');
    console.log('  node test/test-logs.js --type Error         # エラーログのみ取得');
    console.log('  node test/test-logs.js -t Warning -c 20     # 警告ログ20件取得');
    console.log('  node test/test-logs.js --search "エラー"      # "エラー"を含むログを検索');
    console.log('  node test/test-logs.js --no-stack           # スタックトレース非表示');
    console.log('  node test/test-logs.js -t Error --no-stack  # エラーログ（スタックトレースなし）');
    console.log('');
}

async function testGetLogs() {
    // コマンドライン引数の解析
    const args = process.argv.slice(2);
    
    // ヘルプ表示
    if (args.includes('--help') || args.includes('-h')) {
        showHelp();
        return;
    }
    
    // ログタイプの取得
    let logType = 'All';
    const typeIndex = args.findIndex(arg => arg === '--type' || arg === '-t');
    if (typeIndex !== -1 && args[typeIndex + 1]) {
        logType = args[typeIndex + 1];
    }
    
    // ログ数の取得
    let maxCount = 10;
    const countIndex = args.findIndex(arg => arg === '--count' || arg === '-c');
    if (countIndex !== -1 && args[countIndex + 1]) {
        maxCount = parseInt(args[countIndex + 1], 10) || 10;
    }
    
    // 検索テキストの取得
    let searchText = '';
    const searchIndex = args.findIndex(arg => arg === '--search' || arg === '-s');
    if (searchIndex !== -1 && args[searchIndex + 1]) {
        searchText = args[searchIndex + 1];
    }
    
    // スタックトレース表示設定の取得
    const includeStackTrace = !args.includes('--no-stack');
    
    console.log('=== Unity Logs Test ===');
    console.log(`Log Type: ${logType}`);
    console.log(`Max Count: ${maxCount}`);
    console.log(`Search Text: ${searchText || '(検索なし)'}`);
    console.log(`Include StackTrace: ${includeStackTrace ? 'ON' : 'OFF'}`);
    
    const client = new UnityDebugClient();
    
    try {
        console.log('\n1. Connecting to Unity...');
        await client.connect();
        console.log('✓ Connected successfully!');
        
        console.log(`\n2. Getting ${logType} logs (max ${maxCount}${searchText ? `, search: "${searchText}"` : ''}${includeStackTrace ? '' : ', no stack trace'})...`);
        const logs = await client.getLogs(logType, maxCount, searchText, includeStackTrace);
        console.log(`✓ Found ${logs.logs.length} logs (total: ${logs.totalCount})`);
        
        if (logs.logs.length > 0) {
            console.log(`\n--- ${logType.toUpperCase()} LOGS${searchText ? ` (SEARCH: "${searchText}")` : ''}${includeStackTrace ? '' : ' (NO STACK TRACE)'} ---`);
            logs.logs.forEach((log, index) => {
                console.log(`${index + 1}. [${log.timestamp}] ${log.type}: ${log.message}`);
                if (log.file && log.file !== './Runtime/Export/Debug/Debug.bindings.h') {
                    console.log(`   File: ${log.file}`);
                }
                if (includeStackTrace && log.stackTrace) {
                    const stackLines = log.stackTrace.split('\n').slice(0, 3);
                    stackLines.forEach(line => {
                        if (line.trim()) console.log(`   Stack: ${line.trim()}`);
                    });
                }
                console.log('');
            });
        } else {
            console.log(`No ${logType} logs found.`);
        }
        
    } catch (error) {
        console.error('✗ Get logs failed:', error.message);
    } finally {
        console.log('\n3. Disconnecting...');
        client.disconnect();
        console.log('✓ Disconnected');
    }
}

testGetLogs().catch(console.error); 