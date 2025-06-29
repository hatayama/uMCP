import { UnityDebugClient } from './unity-debug-client.js';
import { CompileResult } from './types.js';

function showHelp(): void {
    console.log('=== Unity Domain Reload Timing Test ===');
    console.log('');
    console.log('Usage:');
    console.log('  tsx debug/domain-reload-timing.ts [options]');
    console.log('');
    console.log('Options:');
    console.log('  --cycles, -c <num>  Number of compile cycles to run (default: 3)');
    console.log('  --verbose, -v       Show verbose timing information');
    console.log('  --help, -h          Show this help message');
    console.log('');
    console.log('Examples:');
    console.log('  tsx debug/domain-reload-timing.ts         # Run 3 compile cycles');
    console.log('  tsx debug/domain-reload-timing.ts -c 5    # Run 5 compile cycles');
    console.log('  tsx debug/domain-reload-timing.ts -v      # Verbose timing output');
    console.log('');
}

interface TimingResult {
    cycle: number;
    compileTime: number;
    success: boolean;
    errorCount: number;
    warningCount: number;
}

async function testDomainReloadTiming(): Promise<void> {
    const args = process.argv.slice(2);
    
    if (args.includes('--help') || args.includes('-h')) {
        showHelp();
        return;
    }
    
    let cycles = 3;
    const cyclesIndex = args.findIndex(arg => arg === '--cycles' || arg === '-c');
    if (cyclesIndex !== -1 && args[cyclesIndex + 1]) {
        cycles = parseInt(args[cyclesIndex + 1], 10) || 3;
    }
    
    const verbose = args.includes('--verbose') || args.includes('-v');
    
    console.log('=== Unity Domain Reload Timing Test ===');
    console.log(`Cycles: ${cycles}`);
    console.log(`Verbose: ${verbose ? 'ON' : 'OFF'}`);
    
    const client = new UnityDebugClient();
    const results: TimingResult[] = [];
    
    try {
        console.log('\n1. Connecting to Unity...');
        await client.connect();
        console.log('✓ Connected successfully!');
        
        console.log(`\n2. Running ${cycles} compile cycles to measure domain reload timing...`);
        
        for (let i = 1; i <= cycles; i++) {
            console.log(`\n--- Cycle ${i}/${cycles} ---`);
            
            const startTime = Date.now();
            
            try {
                const compileResult: CompileResult = await client.compileProject(true); // Force recompile
                const endTime = Date.now();
                const compileTime = endTime - startTime;
                
                const result: TimingResult = {
                    cycle: i,
                    compileTime,
                    success: compileResult.Success,
                    errorCount: compileResult.ErrorCount,
                    warningCount: compileResult.WarningCount
                };
                
                results.push(result);
                
                console.log(`✓ Cycle ${i} completed in ${compileTime}ms`);
                console.log(`  Success: ${result.success}, Errors: ${result.errorCount}, Warnings: ${result.warningCount}`);
                
                if (verbose) {
                    console.log(`  Completed at: ${compileResult.CompletedAt}`);
                }
                
            } catch (error) {
                const endTime = Date.now();
                const compileTime = endTime - startTime;
                
                const result: TimingResult = {
                    cycle: i,
                    compileTime,
                    success: false,
                    errorCount: -1,
                    warningCount: -1
                };
                
                results.push(result);
                console.log(`✗ Cycle ${i} failed after ${compileTime}ms: ${(error as Error).message}`);
            }
            
            // Wait a bit between cycles
            if (i < cycles) {
                console.log('  Waiting 2 seconds before next cycle...');
                await new Promise<void>(resolve => setTimeout(resolve, 2000));
            }
        }
        
        console.log('\n=== TIMING ANALYSIS ===');
        
        const successfulResults = results.filter(r => r.success);
        if (successfulResults.length > 0) {
            const times = successfulResults.map(r => r.compileTime);
            const avgTime = times.reduce((a, b) => a + b, 0) / times.length;
            const minTime = Math.min(...times);
            const maxTime = Math.max(...times);
            
            console.log(`Successful compiles: ${successfulResults.length}/${cycles}`);
            console.log(`Average compile time: ${avgTime.toFixed(0)}ms`);
            console.log(`Min compile time: ${minTime}ms`);
            console.log(`Max compile time: ${maxTime}ms`);
            
            if (verbose) {
                console.log('\n--- DETAILED RESULTS ---');
                results.forEach(result => {
                    const status = result.success ? '✓' : '✗';
                    console.log(`Cycle ${result.cycle}: ${status} ${result.compileTime}ms (E:${result.errorCount}, W:${result.warningCount})`);
                });
            }
        } else {
            console.log('No successful compiles to analyze.');
        }
        
    } catch (error) {
        console.error('✗ Domain reload timing test failed:', (error as Error).message);
    } finally {
        console.log('\n3. Disconnecting...');
        client.disconnect();
        console.log('✓ Disconnected');
    }
}

testDomainReloadTiming().catch(console.error);