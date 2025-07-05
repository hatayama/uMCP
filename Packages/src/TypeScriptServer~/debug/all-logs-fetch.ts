import { UnityDebugClient } from './unity-debug-client.js';
import { GetLogsResult, LogEntry } from './types.js';

interface LogCounts {
    Error: number;
    Warning: number;
    Log: number;
    Other: number;
}

function showHelp(): void {
    console.log('=== Unity All Logs Test ===');
    console.log('');
    console.log('Usage:');
    console.log('  tsx debug/all-logs-fetch.ts [options]');
    console.log('');
    console.log('Options:');
    console.log('  --count, -c <num>    Max number of logs to retrieve (default: 100)');
    console.log('  --search <text>      Text to search for within messages');
    console.log('  --stats, -s          Display statistics only');
    console.log('  --help, -h           Show this help message');
    console.log('');
    console.log('Examples:');
    console.log('  tsx debug/all-logs-fetch.ts                # Get 100 logs + stats');
    console.log('  tsx debug/all-logs-fetch.ts -c 200         # Get 200 logs');
    console.log('  tsx debug/all-logs-fetch.ts --stats        # Display stats only');
    console.log('  tsx debug/all-logs-fetch.ts --search "Error" # Search for logs containing "Error"');
    console.log('');
}

async function testAllLogs(): Promise<void> {
    // Parse command line arguments.
    const args = process.argv.slice(2);
    
    // Show help.
    if (args.includes('--help') || args.includes('-h')) {
        showHelp();
        return;
    }
    
    // Get the number of logs.
    let maxCount = 100;
    const countIndex = args.findIndex(arg => arg === '--count' || arg === '-c');
    if (countIndex !== -1 && args[countIndex + 1]) {
        maxCount = parseInt(args[countIndex + 1], 10) || 100;
    }
    
    // Get the search text.
    let searchText = '';
    const searchIndex = args.findIndex(arg => arg === '--search');
    if (searchIndex !== -1 && args[searchIndex + 1]) {
        searchText = args[searchIndex + 1];
    }
    
    // Flag for displaying stats only.
    const statsOnly = args.includes('--stats') || args.includes('-s');
    
    console.log('=== Unity All Logs Test ===');
    console.log(`Max Count: ${maxCount}`);
    console.log(`Search Text: ${searchText || '(none)'}`);
    console.log(`Stats Only: ${statsOnly ? 'ON' : 'OFF'}`);
    
    const client = new UnityDebugClient();
    
    try {
        console.log('\n1. Connecting to Unity...');
        await client.connect();
        console.log('✓ Connected successfully!');
        
        console.log(`\n2. Getting All logs (max ${maxCount}${searchText ? `, search: "${searchText}"` : ''})...`);
        const allLogs: GetLogsResult = await client.getLogs('All', maxCount, searchText);
        console.log(`✓ Found ${allLogs.logs.length} logs (total: ${allLogs.totalCount})`);
        
        // Aggregate by log type.
        const logCounts: LogCounts = {
            Error: 0,
            Warning: 0,
            Log: 0,
            Other: 0
        };
        
        allLogs.logs.forEach((log: LogEntry) => {
            const logType = log.logType as keyof LogCounts;
            if (logCounts[logType] !== undefined) {
                logCounts[logType]++;
            } else {
                logCounts.Other++;
            }
        });
        
        console.log('\n--- LOG STATISTICS ---');
        console.log(`Total logs in Unity: ${allLogs.totalCount}`);
        console.log(`Retrieved logs: ${allLogs.logs.length}${searchText ? ` (filtered by: "${searchText}")` : ''}`);
        console.log(`Error logs: ${logCounts.Error}`);
        console.log(`Warning logs: ${logCounts.Warning}`);
        console.log(`Info logs: ${logCounts.Log}`);
        console.log(`Other logs: ${logCounts.Other}`);
        
        if (!statsOnly) {
            // Display error logs.
            if (logCounts.Error > 0) {
                console.log('\n--- RECENT ERROR LOGS ---');
                const errorLogs = allLogs.logs.filter(log => log.logType === 'Error');
                errorLogs.slice(0, 5).forEach((log, index) => {
                    console.log(`${index + 1}. [${log.timestamp}] ${log.message}`);
                    console.log('');
                });
            }
            
            // Display warning logs.
            if (logCounts.Warning > 0) {
                console.log('\n--- RECENT WARNING LOGS ---');
                const warningLogs = allLogs.logs.filter(log => log.logType === 'Warning');
                warningLogs.slice(0, 5).forEach((log, index) => {
                    console.log(`${index + 1}. [${log.timestamp}] ${log.message}`);
                    console.log('');
                });
            }
            
            // Display info logs.
            if (logCounts.Log > 0) {
                console.log('\n--- RECENT INFO LOGS ---');
                const infoLogs = allLogs.logs.filter(log => log.logType === 'Log');
                infoLogs.slice(0, 3).forEach((log, index) => {
                    console.log(`${index + 1}. [${log.timestamp}] ${log.message}`);
                    console.log('');
                });
            }
        }
        
    } catch (error) {
        console.error('✗ Get all logs failed:', (error as Error).message);
    } finally {
        console.log('\n3. Disconnecting...');
        client.disconnect();
        console.log('✓ Disconnected');
    }
}

testAllLogs().catch(console.error);