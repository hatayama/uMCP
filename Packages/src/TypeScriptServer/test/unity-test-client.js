import * as net from 'net';

/**
 * Unity TCP/IP クライアント（テスト用直接通信版）
 * bundleされたファイルに依存せずに直接TCP/IP通信を行う
 */
export class DirectUnityClient {
    constructor() {
        this.port = parseInt(process.env.UNITY_TCP_PORT || '7400', 10);
        this.host = 'localhost';
        this.socket = null;
        this.connected = false;
    }

    /**
     * Unity側に接続
     */
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

    /**
     * Unity側にpingを送信
     */
    async ping(message) {
        if (!this.connected) {
            throw new Error('Unity MCP Bridge is not connected');
        }

        return new Promise((resolve, reject) => {
            const requestId = Date.now();
            const request = JSON.stringify({
                jsonrpc: '2.0',
                id: requestId,
                method: 'ping',
                params: {
                    message: message
                }
            });

            this.socket.write(request + '\n');

            const timeout = setTimeout(() => {
                reject(new Error('Unity ping timeout'));
            }, 5000);

            this.socket.once('data', (data) => {
                clearTimeout(timeout);
                try {
                    const response = JSON.parse(data.toString());
                    if (response.error) {
                        reject(new Error(`Unity ping error: ${response.error.message}`));
                    } else {
                        resolve(response.result || 'Unity pong');
                    }
                } catch (error) {
                    reject(new Error('Invalid ping response from Unity'));
                }
            });
        });
    }

    /**
     * Unityプロジェクトをコンパイル
     */
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

    /**
     * Unityコンソールのログを取得
     */
    async getLogs(logType = 'All', maxCount = 100) {
        if (!this.connected) {
            throw new Error('Unity MCP Bridge is not connected');
        }

        return new Promise((resolve, reject) => {
            const requestId = Date.now();
            const request = JSON.stringify({
                jsonrpc: '2.0',
                id: requestId,
                method: 'getLogs',
                params: {
                    logType: logType,
                    maxCount: maxCount
                }
            });

            this.socket.write(request + '\n');

            const timeout = setTimeout(() => {
                reject(new Error('Unity getLogs timeout'));
            }, 10000);

            this.socket.once('data', (data) => {
                clearTimeout(timeout);
                try {
                    const response = JSON.parse(data.toString());
                    if (response.error) {
                        reject(new Error(`Unity getLogs error: ${response.error.message}`));
                    } else {
                        resolve(response.result);
                    }
                } catch (error) {
                    reject(new Error('Invalid getLogs response from Unity'));
                }
            });
        });
    }

    /**
     * Unity Test Runnerを実行
     */
    async runTests(filterType = 'all', filterValue = '', saveXml = false) {
        if (!this.connected) {
            throw new Error('Unity MCP Bridge is not connected');
        }

        return new Promise((resolve, reject) => {
            const requestId = Date.now();
            const request = JSON.stringify({
                jsonrpc: '2.0',
                id: requestId,
                method: 'runtests',
                params: {
                    filterType: filterType,
                    filterValue: filterValue,
                    saveXml: saveXml
                }
            });

            this.socket.write(request + '\n');

            const timeout = setTimeout(() => {
                reject(new Error('Unity runTests timeout'));
            }, 60000); // 60秒タイムアウト

            this.socket.once('data', (data) => {
                clearTimeout(timeout);
                try {
                    const response = JSON.parse(data.toString());
                    if (response.error) {
                        reject(new Error(`Unity runTests error: ${response.error.message}`));
                    } else {
                        resolve(response.result);
                    }
                } catch (error) {
                    reject(new Error('Invalid runTests response from Unity'));
                }
            });
        });
    }

    /**
     * 接続を切断
     */
    disconnect() {
        if (this.socket) {
            this.socket.destroy();
            this.socket = null;
        }
        this.connected = false;
    }
}