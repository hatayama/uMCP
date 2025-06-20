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
 * Unity側との通信を行うTCP/IPクライアント
 */
export class UnityClient {
  private socket: net.Socket | null = null;
  private _connected: boolean = false;
  private readonly port: number;
  private readonly host: string = UNITY_CONNECTION.DEFAULT_HOST;

  constructor() {
    // 環境変数UNITY_TCP_PORTからポート番号を取得、デフォルトは7400
    this.port = parseInt(process.env.UNITY_TCP_PORT || UNITY_CONNECTION.DEFAULT_PORT, 10);
  }

  get connected(): boolean {
    return this._connected && this.socket !== null && !this.socket.destroyed;
  }

  /**
   * Unity側の接続状態を実際にテストする
   * ソケットの状態だけでなく、実際の通信可能性を確認
   */
  async testConnection(): Promise<boolean> {
    if (!this._connected || this.socket === null || this.socket.destroyed) {
      return false;
    }

    try {
      // 簡単なpingを送って実際に通信できるかテスト
      await this.ping(UNITY_CONNECTION.CONNECTION_TEST_MESSAGE);
      return true;
    } catch {
      // 通信失敗の場合は接続を切断状態にする
      this._connected = false;
      return false;
    }
  }

  /**
   * Unity側に接続（必要に応じて再接続）
   */
  async ensureConnected(): Promise<void> {
    // 既に接続済みで実際に通信可能な場合はそのまま
    if (await this.testConnection()) {
      return;
    }

    // 接続が失われている場合は再接続
    this.disconnect();
    await this.connect();
  }

  /**
   * Unity側に接続
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
   * Unity側にpingを送信
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
   * Unityプロジェクトをコンパイル
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
      // 接続されていない場合は明確なエラーを投げる（ダミーデータは返さない）
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
      }, TIMEOUTS.COMPILE); // 30秒タイムアウト

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
   * Unityコンソールのログを取得
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
      // 接続されていない場合は明確なエラーを投げる（ダミーデータは返さない）
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
   * Unity Test Runnerを実行
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
      // 接続されていない場合は明確なエラーを投げる（ダミーデータは返さない）
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
      }, TIMEOUTS.RUN_TESTS); // 60秒タイムアウト（テスト実行は時間がかかる）

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
   * 接続を切断
   */
  disconnect(): void {
    if (this.socket) {
      this.socket.destroy();
      this.socket = null;
    }
    this._connected = false;
  }
} 