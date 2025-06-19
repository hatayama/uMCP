import { UnityDebugClient } from './unity-debug-client.js';

function showHelp() {
    console.log('=== Unity Compile Test ===');
    console.log('');
    console.log('Usage:');
    console.log('  node debug/compile-check.js [options]');
    console.log('');
    console.log('Options:');
    console.log('  --force, -f    強制再コンパイルを実行');
    console.log('  --help, -h     このヘルプを表示');
    console.log('');
    console.log('Examples:');
    console.log('  node debug/compile-check.js           # 通常コンパイル');
    console.log('  node debug/compile-check.js --force   # 強制再コンパイル');
    console.log('  node debug/compile-check.js -f        # 強制再コンパイル（短縮形）');
    console.log('');
}

async function testCompile() {
    // コマンドライン引数から強制コンパイルフラグを取得
    const args = process.argv.slice(2);
    
    // ヘルプ表示
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