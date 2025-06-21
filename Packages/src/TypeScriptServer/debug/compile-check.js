import { UnityDebugClient } from './unity-debug-client.js';

function showHelp() {
    console.log('=== Unity Compile Test ===');
    console.log('');
    console.log('Usage:');
    console.log('  node debug/compile-check.js [options]');
    console.log('');
    console.log('Options:');
    console.log('  --force, -f    Force a recompile.');
    console.log('  --help, -h     Show this help message.');
    console.log('');
    console.log('Examples:');
    console.log('  node debug/compile-check.js           # Normal compile');
    console.log('  node debug/compile-check.js --force   # Force recompile');
    console.log('  node debug/compile-check.js -f        # Force recompile (short form)');
    console.log('');
}

async function testCompile() {
    // Get force recompile flag from command line arguments.
    const args = process.argv.slice(2);
    
    // Show help.
    if (args.includes('--help') || args.includes('-h')) {
        showHelp();
        return;
    }
    
    const forceRecompileFlag = args.includes('--force') || args.includes('-f');
    
    console.log('=== Unity Compile Test ===');
    console.log(`Force Recompile: ${forceRecompileFlag ? 'ON' : 'OFF'}`);
    
    const client = new UnityDebugClient();
    
    try {
        console.log('\n1. Connecting to Unity...');
        await client.connect();
        console.log('✓ Connected successfully!');
        
        console.log(`\n2. Executing compile${forceRecompileFlag ? ' (Force Recompile)' : ''}...`);
        const compileResult = await client.compileProject(forceRecompileFlag);
        
        console.log('✓ Compile completed!');
        console.log(`Success: ${compileResult.success}`);
        console.log(`Errors: ${compileResult.errorCount}`);
        console.log(`Warnings: ${compileResult.warningCount}`);
        console.log(`Completed at: ${compileResult.completedAt}`);
        
        if (compileResult.errors && compileResult.errors.length > 0) {
            console.log('\n--- ERRORS ---');
            compileResult.errors.forEach((error, index) => {
                console.log(`${index + 1}. ${error.file}(${error.line},${error.column}): ${error.message}`);
            });
        }
        
        if (compileResult.warnings && compileResult.warnings.length > 0) {
            console.log('\n--- WARNINGS ---');
            compileResult.warnings.forEach((warning, index) => {
                console.log(`${index + 1}. ${warning.file}(${warning.line},${warning.column}): ${warning.message}`);
            });
        }
        
    } catch (error) {
        console.error('✗ Compile failed:', error.message);
    } finally {
        console.log('\n3. Disconnecting...');
        client.disconnect();
        console.log('✓ Disconnected');
    }
}

testCompile().catch(console.error); 