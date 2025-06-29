import { UnityDebugClient } from './unity-debug-client.js';
import { CompileResult } from './types.js';

function showHelp(): void {
    console.log('=== Unity Detailed Compile Test ===');
    console.log('');
    console.log('Usage:');
    console.log('  tsx debug/compile-detailed.ts [options]');
    console.log('');
    console.log('Options:');
    console.log('  --force, -f      Force recompile');
    console.log('  --verbose, -v    Show detailed output');
    console.log('  --help, -h       Show this help message');
    console.log('');
    console.log('Examples:');
    console.log('  tsx debug/compile-detailed.ts           # Normal compile with details');
    console.log('  tsx debug/compile-detailed.ts --force   # Force recompile with details');
    console.log('  tsx debug/compile-detailed.ts -v        # Verbose output');
    console.log('');
}

async function testDetailedCompile(): Promise<void> {
    const args = process.argv.slice(2);
    
    if (args.includes('--help') || args.includes('-h')) {
        showHelp();
        return;
    }
    
    const forceRecompile = args.includes('--force') || args.includes('-f');
    const verbose = args.includes('--verbose') || args.includes('-v');
    
    console.log('=== Unity Detailed Compile Test ===');
    console.log(`Force Recompile: ${forceRecompile ? 'ON' : 'OFF'}`);
    console.log(`Verbose: ${verbose ? 'ON' : 'OFF'}`);
    
    const client = new UnityDebugClient();
    
    try {
        console.log('\n1. Connecting to Unity...');
        await client.connect();
        console.log('✓ Connected successfully!');
        
        console.log('\n2. Starting compilation...');
        const startTime = Date.now();
        
        const compileResult: CompileResult = await client.compileProject(forceRecompile);
        
        const endTime = Date.now();
        const duration = endTime - startTime;
        
        console.log('\n=== COMPILATION RESULTS ===');
        console.log(`✓ Compile completed in ${duration}ms`);
        console.log(`Success: ${compileResult.Success}`);
        console.log(`Errors: ${compileResult.ErrorCount}`);
        console.log(`Warnings: ${compileResult.WarningCount}`);
        console.log(`Completed at: ${compileResult.CompletedAt}`);
        
        if (verbose) {
            console.log('\n--- FULL RESPONSE ---');
            console.log(JSON.stringify(compileResult, null, 2));
        }
        
        if (compileResult.Errors && compileResult.Errors.length > 0) {
            console.log('\n--- COMPILATION ERRORS ---');
            compileResult.Errors.forEach((error, index) => {
                console.log(`${index + 1}. ${error.file}(${error.line},${error.column}): ${error.message}`);
            });
        }
        
        if (compileResult.Warnings && compileResult.Warnings.length > 0) {
            console.log('\n--- COMPILATION WARNINGS ---');
            compileResult.Warnings.forEach((warning, index) => {
                console.log(`${index + 1}. ${warning.file}(${warning.line},${warning.column}): ${warning.message}`);
            });
        }
        
        if (compileResult.Success) {
            console.log('\n🎉 Compilation successful!');
        } else {
            console.log('\n❌ Compilation failed!');
        }
        
    } catch (error) {
        console.error('✗ Compile test failed:', (error as Error).message);
    } finally {
        console.log('\n3. Disconnecting...');
        client.disconnect();
        console.log('✓ Disconnected');
    }
}

testDetailedCompile().catch(console.error);