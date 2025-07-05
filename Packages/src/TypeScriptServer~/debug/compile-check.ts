import { UnityDebugClient } from './unity-debug-client.js';
import { CompileResult } from './types.js';

function showHelp(): void {
    console.log('=== Unity Compile Test ===');
    console.log('');
    console.log('Usage:');
    console.log('  tsx debug/compile-check.ts [options]');
    console.log('');
    console.log('Options:');
    console.log('  --force, -f    Force a recompile.');
    console.log('  --help, -h     Show this help message.');
    console.log('');
    console.log('Examples:');
    console.log('  tsx debug/compile-check.ts           # Normal compile');
    console.log('  tsx debug/compile-check.ts --force   # Force recompile');
    console.log('  tsx debug/compile-check.ts -f        # Force recompile (short form)');
    console.log('');
}

async function testCompile(): Promise<void> {
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
        const compileResult: CompileResult = await client.compileProject(forceRecompileFlag);
        
        if (compileResult) {
            console.log('✓ Compile completed!');
            console.log(`Success: ${compileResult.Success}`);
            console.log(`Errors: ${compileResult.ErrorCount}`);
            console.log(`Warnings: ${compileResult.WarningCount}`);
            console.log(`Completed at: ${compileResult.CompletedAt}`);
            
            if (compileResult.Errors && compileResult.Errors.length > 0) {
                console.log('\n--- ERRORS ---');
                compileResult.Errors.forEach((error, index) => {
                    console.log(`${index + 1}. ${error.file}(${error.line},${error.column}): ${error.message}`);
                });
            }
            
            if (compileResult.Warnings && compileResult.Warnings.length > 0) {
                console.log('\n--- WARNINGS ---');
                compileResult.Warnings.forEach((warning, index) => {
                    console.log(`${index + 1}. ${warning.file}(${warning.line},${warning.column}): ${warning.message}`);
                });
            }
        } else {
            console.log('✗ Compile completed but no result returned');
        }
        
    } catch (error) {
        console.error('✗ Compile failed:', (error as Error).message);
    } finally {
        console.log('\n3. Disconnecting...');
        client.disconnect();
        console.log('✓ Disconnected');
    }
}

testCompile().catch(console.error);