import * as net from 'net';
import { 
  UNITY_CONNECTION, 
  JSONRPC, 
  TIMEOUTS, 
  LOG_CONFIG, 
  TEST_CONFIG, 
  ERROR_MESSAGES 
} from './constants.js';

/**
 * TCP/IP client for communication with Unity
 */
export class UnityClient {
  private socket: net.Socket | null = null;
  private _connected: boolean = false;
  private readonly port: number;
  private readonly host: string = UNITY_CONNECTION.DEFAULT_HOST;

  constructor() {
    // Get port number from environment variable UNITY_TCP_PORT, default is 7400
    this.port = parseInt(process.env.UNITY_TCP_PORT || UNITY_CONNECTION.DEFAULT_PORT, 10);
  }

  get connected(): boolean {
    return this._connected && this.socket !== null && !this.socket.destroyed;
  }

  /**
   * Actually test Unity's connection status
   * Check actual communication possibility, not just socket status
   */
  async testConnection(): Promise<boolean> {
    if (!this._connected || this.socket === null || this.socket.destroyed) {
      return false;
    }

    try {
      // Send a simple ping to test if actual communication is possible
      await this.ping(UNITY_CONNECTION.CONNECTION_TEST_MESSAGE);
      return true;
    } catch {
      // Set connection to disconnected state if communication fails
      this._connected = false;
      return false;
    }
  }

  /**
   * Connect to Unity (reconnect if necessary)
   */
  async ensureConnected(): Promise<void> {
    // Return as is if already connected and actual communication is possible
    if (await this.testConnection()) {
      return;
    }

    // Reconnect if connection is lost
    this.disconnect();
    await this.connect();
  }

  /**
   * Connect to Unity
   */
  async connect(): Promise<void> {
    return new Promise((resolve, reject) => {
      this.socket = new net.Socket();
      
      this.socket.connect(this.port, this.host, () => {
        this._connected = true;
        resolve();
      });

      this.socket.on('error', (error) => {
        this._connected = false;
        reject(new Error(`Unity connection failed: ${error.message}`));
      });

      this.socket.on('close', () => {
        this._connected = false;
      });
    });
  }

  /**
   * Send ping to Unity
   */
  async ping(message: string): Promise<string> {
    if (!this.connected) {
      throw new Error('Not connected to Unity');
    }

    return new Promise((resolve, reject) => {
      const requestId = Date.now();
      const request = JSON.stringify({
        jsonrpc: JSONRPC.VERSION,
        id: requestId,
        method: 'ping',
        params: {
          message: message
        }
      });

      this.socket!.write(request + '\n');

      const timeout = setTimeout(() => {
        reject(new Error(`Unity ping ${ERROR_MESSAGES.TIMEOUT}`));
      }, TIMEOUTS.PING);

      this.socket!.once('data', (data) => {
        clearTimeout(timeout);
        try {
          const response = JSON.parse(data.toString());
          if (response.error) {
            reject(new Error(`Unity error: ${response.error.message}`));
          } else {
            resolve(response.result || 'Unity pong');
          }
        } catch (error) {
          reject(new Error('Invalid response from Unity'));
        }
      });
    });
  }

  /**
   * Compile Unity project
   */
  async compileProject(forceRecompile: boolean = false): Promise<{
    success: boolean;
    errorCount: number;
    warningCount: number;
    completedAt: string;
    errors: Array<{
      message: string;
      file: string;
      line: number;
      column: number;
      type: string;
    }>;
    warnings: Array<{
      message: string;
      file: string;
      line: number;
      column: number;
      type: string;
    }>;
  }> {
    if (!this.connected) {
      // Throw a clear error if not connected (do not return dummy data)
      throw new Error(`${ERROR_MESSAGES.NOT_CONNECTED}. Cannot compile project without Unity connection. Please ensure Unity is running and MCP server is started.`);
    }

    return new Promise((resolve, reject) => {
      const requestId = Date.now();
      const request = JSON.stringify({
        jsonrpc: JSONRPC.VERSION,
        id: requestId,
        method: 'compile',
        params: {
          forceRecompile: forceRecompile
        }
      });

      this.socket!.write(request + '\n');

      const timeout = setTimeout(() => {
        reject(new Error(`Unity compile ${ERROR_MESSAGES.TIMEOUT}`));
      }, TIMEOUTS.COMPILE); // 30 second timeout

      this.socket!.once('data', (data) => {
        clearTimeout(timeout);
        try {
          const response = JSON.parse(data.toString());
          if (response.error) {
            reject(new Error(`Unity compile error: ${response.error.message}`));
          } else {
            resolve(response.result);
          }
        } catch (error) {
          reject(new Error(`${ERROR_MESSAGES.INVALID_RESPONSE} compile`));
        }
      });
    });
  }

  /**
   * Retrieve Unity console logs
   */
  async getLogs(logType: string = LOG_CONFIG.DEFAULT_TYPE, maxCount: number = LOG_CONFIG.DEFAULT_MAX_COUNT, searchText: string = LOG_CONFIG.DEFAULT_SEARCH_TEXT, includeStackTrace: boolean = LOG_CONFIG.DEFAULT_INCLUDE_STACK_TRACE): Promise<{
    logs: Array<{
      type: string;
      message: string;
      stackTrace?: string;
      timestamp: string;
    }>;
    totalCount: number;
  }> {
    if (!this.connected) {
      // Throw a clear error if not connected (do not return dummy data)
      throw new Error('${ERROR_MESSAGES.NOT_CONNECTED}. Cannot get logs without Unity connection. Please ensure Unity is running and MCP server is started.');
    }

    return new Promise((resolve, reject) => {
      const requestId = Date.now();
      const request = JSON.stringify({
        jsonrpc: JSONRPC.VERSION,
        id: requestId,
        method: 'getLogs',
        params: {
          logType: logType || LOG_CONFIG.DEFAULT_TYPE,
          maxCount: maxCount || LOG_CONFIG.DEFAULT_MAX_COUNT,
          searchText: searchText || LOG_CONFIG.DEFAULT_SEARCH_TEXT,
          includeStackTrace: includeStackTrace !== undefined ? includeStackTrace : LOG_CONFIG.DEFAULT_INCLUDE_STACK_TRACE
        }
      });

      this.socket!.write(request + '\n');

      const timeout = setTimeout(() => {
        reject(new Error(`Unity getLogs ${ERROR_MESSAGES.TIMEOUT}`));
      }, TIMEOUTS.GET_LOGS);

      this.socket!.once('data', (data) => {
        clearTimeout(timeout);
        try {
          const response = JSON.parse(data.toString());
          if (response.error) {
            reject(new Error(`Unity getLogs error: ${response.error.message}`));
          } else {
            resolve(response.result);
          }
        } catch (error) {
          reject(new Error(`${ERROR_MESSAGES.INVALID_RESPONSE} getLogs`));
        }
      });
    });
  }

  /**
   * Execute Unity Test Runner
   */
  async runTests(filterType: string = TEST_CONFIG.DEFAULT_FILTER_TYPE, filterValue: string = TEST_CONFIG.DEFAULT_FILTER_VALUE, saveXml: boolean = TEST_CONFIG.DEFAULT_SAVE_XML): Promise<{
    success: boolean;
    message: string;
    testResults?: {
      PassedCount: number;
      FailedCount: number;
      SkippedCount: number;
      TotalCount: number;
      Duration: number;
      FailedTests?: Array<{
        TestName: string;
        FullName: string;
        Message: string;
        StackTrace: string;
        Duration: number;
      }>;
    };
    xmlPath?: string;
    completedAt: string;
    error?: string;
  }> {
    if (!this.connected) {
      // Throw a clear error if not connected (do not return dummy data)
      throw new Error('${ERROR_MESSAGES.NOT_CONNECTED}. Cannot execute tests without Unity connection. Please ensure Unity is running and MCP server is started.');
    }

    return new Promise((resolve, reject) => {
      const requestId = Date.now();
      const request = JSON.stringify({
        jsonrpc: JSONRPC.VERSION,
        id: requestId,
        method: 'runtests',
        params: {
          filterType: filterType,
          filterValue: filterValue,
          saveXml: saveXml
        }
      });

      this.socket!.write(request + '\n');

      const timeout = setTimeout(() => {
        reject(new Error(`Unity runTests ${ERROR_MESSAGES.TIMEOUT}`));
      }, TIMEOUTS.RUN_TESTS); // 60 second timeout (test execution takes time)

      this.socket!.once('data', (data) => {
        clearTimeout(timeout);
        try {
          const response = JSON.parse(data.toString());
          if (response.error) {
            reject(new Error(`Unity runTests error: ${response.error.message}`));
          } else {
            resolve(response.result);
          }
        } catch (error) {
          reject(new Error(`${ERROR_MESSAGES.INVALID_RESPONSE} runTests`));
        }
      });
    });
  }

  /**
   * Get available commands from Unity
   */
  async getAvailableCommands(): Promise<string[]> {
    await this.ensureConnected();
    
    const request = {
      jsonrpc: JSONRPC.VERSION,
      id: this.generateId(),
      method: "getAvailableCommands",
      params: {}
    };

    const response = await this.sendRequest(request);
    
    if (response.error) {
      throw new Error(`Failed to get available commands: ${response.error.message}`);
    }

    return response.result || [];
  }

  /**
   * Get command details from Unity
   */
  async getCommandDetails(): Promise<Array<{name: string, description: string}>> {
    await this.ensureConnected();
    
    const request = {
      jsonrpc: JSONRPC.VERSION,
      id: this.generateId(),
      method: "getCommandDetails",
      params: {}
    };

    const response = await this.sendRequest(request);
    
    if (response.error) {
      throw new Error(`Failed to get command details: ${response.error.message}`);
    }

    return response.result || [];
  }

  /**
   * Execute any Unity command dynamically
   */
  async executeCommand(commandName: string, params: any = {}): Promise<any> {
    await this.ensureConnected();
    
    const request = {
      jsonrpc: JSONRPC.VERSION,
      id: this.generateId(),
      method: commandName,
      params: params
    };

    const response = await this.sendRequest(request);
    
    if (response.error) {
      throw new Error(`Failed to execute command '${commandName}': ${response.error.message}`);
    }

    return response.result;
  }

  /**
   * Generate unique request ID
   */
  private generateId(): number {
    return Date.now();
  }

  /**
   * Send request and wait for response
   */
  private async sendRequest(request: any): Promise<any> {
    return new Promise((resolve, reject) => {
      const timeout = setTimeout(() => {
        reject(new Error(`Request ${ERROR_MESSAGES.TIMEOUT}`));
      }, TIMEOUTS.PING);

      this.socket!.write(JSON.stringify(request) + '\n');

      this.socket!.once('data', (data) => {
        clearTimeout(timeout);
        try {
          const response = JSON.parse(data.toString());
          resolve(response);
        } catch (error) {
          reject(new Error('Invalid response from Unity'));
        }
      });
    });
  }

  /**
   * Disconnect
   */
  disconnect(): void {
    if (this.socket) {
      this.socket.destroy();
      this.socket = null;
    }
    this._connected = false;
  }
} 