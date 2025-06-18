import * as net from 'net';

/**
 * Unity TCP/IP クライアント（直接通信版）
 */
class DirectUnityClient {
    constructor() {
        this.port = parseInt(process.env.UNITY_TCP_PORT || '7400', 10);
        this.host = 'localhost';
        this.socket = null;
        this.connected = false;
    }

    async connect() {
        return new Promise((resolve, reject) => {
            this.socket = new net.Socket();
            
            this.socket.connect(this.port, this.host, () => {
                this.connected = true;
                resolve();
            });

            this.socket.on('error', (error) => {
                this.connected = false;
                reject(new Error(`Unity connection failed: ${error.message}`));
            });

            this.socket.on('close', () => {
                this.connected = false;
            });
        });
    }

    async compileProject(forceRecompile = false) {
        if (!this.connected) {
            throw new Error('Unity MCP Bridge is not connected');
        }

        return new Promise((resolve, reject) => {
            const requestId = Date.now();
            const request = JSON.stringify({
                jsonrpc: '2.0',
                id: requestId,
                method: 'compile',
                params: {
                    forceRecompile: forceRecompile
                }
            });

            this.socket.write(request + '\n');

            const timeout = setTimeout(() => {
                reject(new Error('Unity compile timeout'));
            }, 30000);

            this.socket.once('data', (data) => {
                clearTimeout(timeout);
                try {
                    const response = JSON.parse(data.toString());
                    if (response.error) {
                        reject(new Error(`Unity compile error: ${response.error.message}`));
                    } else {
                        resolve(response.result);
                    }
                } catch (error) {
                    reject(new Error('Invalid compile response from Unity'));
                }
            });
        });
    }

    disconnect() {
        if (this.socket) {
            this.socket.destroy();
            this.socket = null;
        }
        this.connected = false;
    }
}

function showHelp() {
    console.log('=== Unity Compile Test ===');
    console.log('');
    console.log('Usage:');
    console.log('  node test/test-compile.js [options]');
    console.log('');
    console.log('Options:');
    console.log('  --force, -f    強制再コンパイルを実行');
    console.log('  --help, -h     このヘルプを表示');
    console.log('');
    console.log('Examples:');
    console.log('  node test/test-compile.js           # 通常コンパイル');
    console.log('  node test/test-compile.js --force   # 強制再コンパイル');
    console.log('  node test/test-compile.js -f        # 強制再コンパイル（短縮形）');
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
    
    const client = new DirectUnityClient();
    
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