import { UnityClient } from './dist/unity-client.js';

async function testCompile() {
    console.log('=== Unity Compile Test ===');
    
    const client = new UnityClient();
    
    try {
        console.log('1. Connecting to Unity...');
        await client.connect();
        console.log('✓ Connected successfully!');
        
        console.log('\n2. Executing compile...');
        const compileResult = await client.compileProject(false);
        
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