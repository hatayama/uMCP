import { UnityDebugClient } from './unity-debug-client.js';

function showHelp() {
    console.log('=== Unity Logs Test ===');
    console.log('');
    console.log('Usage:');
    console.log('  node test/test-logs.js [options]');
    console.log('');
    console.log('Options:');
    console.log('  --type, -t <type>    Specify log type (All, Error, Warning, Log)');
    console.log('  --count, -c <num>    Max number of logs to retrieve (default: 10)');
    console.log('  --search, -s <text>  Text to search for in messages');
    console.log('  --no-stack           Hide stack traces');
    console.log('  --help, -h           Show this help message');
    console.log('');
    console.log('Examples:');
    console.log('  node test/test-logs.js                      # Get 10 logs (with stack trace)');
    console.log('  node test/test-logs.js --type Error         # Get only Error logs');
    console.log('  node test/test-logs.js -t Warning -c 20     # Get 20 Warning logs');
    console.log('  node test/test-logs.js --search "Error"     # Search for logs containing "Error"');
    console.log('  node test/test-logs.js --no-stack           # Hide stack traces');
    console.log('  node test/test-logs.js -t Error --no-stack  # Get Error logs (no stack trace)');
    console.log('');
}

async function testGetLogs() {
    // Parse command line arguments.
    const args = process.argv.slice(2);
    
    // Show help.
    if (args.includes('--help') || args.includes('-h')) {
        showHelp();
        return;
    }
    
    // Get log type.
    let logType = 'All';
    const typeIndex = args.findIndex(arg => arg === '--type' || arg === '-t');
    if (typeIndex !== -1 && args[typeIndex + 1]) {
        logType = args[typeIndex + 1];
    }
    
    // Get number of logs.
    let maxCount = 10;
    const countIndex = args.findIndex(arg => arg === '--count' || arg === '-c');
    if (countIndex !== -1 && args[countIndex + 1]) {
        maxCount = parseInt(args[countIndex + 1], 10) || 10;
    }
    
    // Get search text.
    let searchText = '';
    const searchIndex = args.findIndex(arg => arg === '--search' || arg === '-s');
    if (searchIndex !== -1 && args[searchIndex + 1]) {
        searchText = args[searchIndex + 1];
    }
    
    // Get stack trace display setting.
    const includeStackTrace = !args.includes('--no-stack');
    
    console.log('=== Unity Logs Test ===');
    console.log(`Log Type: ${logType}`);
    console.log(`Max Count: ${maxCount}`);
    console.log(`Search Text: ${searchText || '(none)'}`);
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