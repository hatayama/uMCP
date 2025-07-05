import * as net from 'net';
import { UNITY_CONNECTION } from '../src/constants.js';
import {
    CompileResult,
    GetLogsResult,
    TestResult,
    PingResult,
    GetCommandDetailsResult,
    JsonRpcRequest,
    JsonRpcResponse,
    JsonRpcNotification
} from './types.js';

/**
 * Unity TCP/IP client (for direct communication debugging).
 * This client communicates directly via TCP/IP without depending on bundled files.
 */
export class UnityDebugClient {
    private socket: net.Socket | null = null;
    private _connected: boolean = false;
    private readonly port: number;
    private readonly host: string;

    constructor() {
        this.port = parseInt(process.env.UNITY_TCP_PORT || UNITY_CONNECTION.DEFAULT_PORT, 10);
        this.host = 'localhost';
    }

    get connected(): boolean {
        return this._connected && this.socket !== null && !this.socket.destroyed;
    }

    /**
     * Connect to the Unity side.
     */
    async connect(): Promise<void> {
        console.log(`Attempting to connect to Unity at ${this.host}:${this.port}...`);
        return new Promise((resolve, reject) => {
            this.socket = new net.Socket();
            
            this.socket.connect(this.port, this.host, () => {
                this._connected = true;
                console.log('✅ Connected to Unity successfully');
                resolve();
            });

            this.socket.on('error', (error) => {
                this._connected = false;
                console.error('❌ Connection error:', error.message);
                reject(new Error(`Unity connection failed: ${error.message}`));
            });

            this.socket.on('close', () => {
                this._connected = false;
                console.log('🔌 Disconnected from Unity');
            });

            // Add timeout
            setTimeout(() => {
                if (!this._connected) {
                    if (this.socket) {
                        this.socket.destroy();
                    }
                    reject(new Error('Connection timeout'));
                }
            }, 5000);
        });
    }

    /**
     * Send a ping to the Unity side.
     */
    async ping(message: string): Promise<PingResult> {
        if (!this.connected) {
            throw new Error('Unity MCP Bridge is not connected');
        }

        return new Promise((resolve, reject) => {
            const requestId = Date.now();
            const request: JsonRpcRequest = {
                jsonrpc: '2.0',
                id: requestId,
                method: 'ping',
                params: {
                    message: message
                }
            };

            this.socket!.write(JSON.stringify(request) + '\n');

            const timeout = setTimeout(() => {
                reject(new Error('Unity ping timeout'));
            }, 5000);

            this.socket!.once('data', (data) => {
                clearTimeout(timeout);
                try {
                    const response: JsonRpcResponse<PingResult> = JSON.parse(data.toString());
                    if (response.error) {
                        reject(new Error(`Unity ping error: ${response.error.message}`));
                    } else {
                        resolve(response.result || { message: 'Unity pong', timestamp: new Date().toISOString() });
                    }
                } catch (error) {
                    reject(new Error('Invalid ping response from Unity'));
                }
            });
        });
    }

    /**
     * Compile the Unity project.
     */
    async compileProject(forceRecompile: boolean = false): Promise<CompileResult> {
        if (!this.connected) {
            throw new Error('Unity MCP Bridge is not connected');
        }

        return new Promise((resolve, reject) => {
            const requestId = Date.now();
            const request: JsonRpcRequest = {
                jsonrpc: '2.0',
                id: requestId,
                method: 'compile',
                params: {
                    forceRecompile: forceRecompile
                }
            };

            this.socket!.write(JSON.stringify(request) + '\n');

            const timeout = setTimeout(() => {
                reject(new Error('Unity compile timeout'));
            }, 30000);

            const handleData = (data: Buffer) => {
                try {
                    const response: JsonRpcResponse<CompileResult> | JsonRpcNotification = JSON.parse(data.toString());
                    
                    // Skip notifications - we only want responses with matching request ID
                    if ('method' in response && response.method && response.method.startsWith('notifications/')) {
                        return; // Continue listening for the actual response
                    }
                    
                    // Check if this is the response to our request
                    if ('id' in response && response.id === requestId) {
                        clearTimeout(timeout);
                        this.socket!.off('data', handleData);
                        
                        if (response.error) {
                            reject(new Error(`Unity compile error: ${response.error.message}`));
                        } else {
                            resolve(response.result!);
                        }
                    }
                } catch (error) {
                    clearTimeout(timeout);
                    this.socket!.off('data', handleData);
                    reject(new Error('Invalid compile response from Unity'));
                }
            };

            this.socket!.on('data', handleData);
        });
    }

    /**
     * Get logs from the Unity console.
     */
    async getLogs(
        logType: string = 'All', 
        maxCount: number = 100, 
        searchText: string = '', 
        includeStackTrace: boolean = true
    ): Promise<GetLogsResult> {
        if (!this.connected) {
            throw new Error('Unity MCP Bridge is not connected');
        }

        return new Promise((resolve, reject) => {
            const requestId = Date.now();
            const request: JsonRpcRequest = {
                jsonrpc: '2.0',
                id: requestId,
                method: 'getLogs',
                params: {
                    logType: logType,
                    maxCount: maxCount,
                    searchText: searchText,
                    includeStackTrace: includeStackTrace
                }
            };

            this.socket!.write(JSON.stringify(request) + '\n');

            const timeout = setTimeout(() => {
                reject(new Error('Unity getLogs timeout'));
            }, 10000);

            this.socket!.once('data', (data) => {
                clearTimeout(timeout);
                try {
                    const response: JsonRpcResponse<GetLogsResult> = JSON.parse(data.toString());
                    if (response.error) {
                        reject(new Error(`Unity getLogs error: ${response.error.message}`));
                    } else {
                        resolve(response.result!);
                    }
                } catch (error) {
                    reject(new Error('Invalid getLogs response from Unity'));
                }
            });
        });
    }

    /**
     * Run the Unity Test Runner.
     */
    async runTests(
        filterType: string = 'all', 
        filterValue: string = '', 
        saveXml: boolean = false
    ): Promise<TestResult> {
        if (!this.connected) {
            throw new Error('Unity MCP Bridge is not connected');
        }

        return new Promise((resolve, reject) => {
            const requestId = Date.now();
            const request: JsonRpcRequest = {
                jsonrpc: '2.0',
                id: requestId,
                method: 'runtests',
                params: {
                    filterType: filterType,
                    filterValue: filterValue,
                    saveXml: saveXml
                }
            };

            this.socket!.write(JSON.stringify(request) + '\n');

            const timeout = setTimeout(() => {
                reject(new Error('Unity runTests timeout'));
            }, 60000); // 60 second timeout

            this.socket!.once('data', (data) => {
                clearTimeout(timeout);
                try {
                    const response: JsonRpcResponse<TestResult> = JSON.parse(data.toString());
                    if (response.error) {
                        reject(new Error(`Unity runTests error: ${response.error.message}`));
                    } else {
                        resolve(response.result!);
                    }
                } catch (error) {
                    reject(new Error('Invalid runTests response from Unity'));
                }
            });
        });
    }

    async getCommandDetails(): Promise<GetCommandDetailsResult> {
        if (!this.connected) {
            throw new Error('Not connected to Unity');
        }
        
        console.log('📤 Sending getCommandDetails request...');
        const request: JsonRpcRequest = {
            jsonrpc: '2.0',
            id: Date.now(),
            method: "getCommandDetails",
            params: {}
        };

        console.log('📋 Request:', JSON.stringify(request, null, 2));
        const response = await this.sendRequest<GetCommandDetailsResult>(request);
        console.log('📥 Raw response:', JSON.stringify(response, null, 2));
        
        if (response.error) {
            throw new Error(`Failed to get command details: ${response.error.message}`);
        }

        return response.result || { commands: [] };
    }

    async sendRequest<T>(request: JsonRpcRequest): Promise<JsonRpcResponse<T>> {
        return new Promise((resolve, reject) => {
            const timeout = setTimeout(() => {
                console.error('⏰ Request timeout');
                reject(new Error('Request timeout'));
            }, 10000);

            this.socket!.write(JSON.stringify(request) + '\n');
            console.log('📡 Request sent to Unity');

            this.socket!.once('data', (data) => {
                clearTimeout(timeout);
                console.log('📨 Received data from Unity');
                try {
                    const response: JsonRpcResponse<T> = JSON.parse(data.toString());
                    resolve(response);
                } catch (error) {
                    console.error('❌ Failed to parse response:', error);
                    console.error('Raw data:', data.toString());
                    reject(new Error('Invalid response from Unity'));
                }
            });
        });
    }

    /**
     * Disconnect the connection.
     */
    disconnect(): void {
        if (this.socket) {
            this.socket.destroy();
            this.socket = null;
        }
        this._connected = false;
    }
}