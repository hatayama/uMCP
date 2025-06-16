import * as net from 'net';

/**
 * Unity側との通信を行うTCP/IPクライアント
 */
export class UnityClient {
  private socket: net.Socket | null = null;
  private _connected: boolean = false;
  private readonly port: number;
  private readonly host: string = 'localhost';

  constructor() {
    // 環境変数UNITY_TCP_PORTからポート番号を取得、デフォルトは7400
    this.port = parseInt(process.env.UNITY_TCP_PORT || '7400', 10);
  }

  get connected(): boolean {
    return this._connected && this.socket !== null && !this.socket.destroyed;
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
        jsonrpc: '2.0',
        id: requestId,
        method: 'ping',
        params: {
          message: message
        }
      });

      this.socket!.write(request + '\n');

      const timeout = setTimeout(() => {
        reject(new Error('Unity ping timeout'));
      }, 5000);

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
      throw new Error('Unity MCP Bridge is not connected. Cannot compile project without Unity connection. Please ensure Unity is running and MCP server is started.');
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

      this.socket!.write(request + '\n');

      const timeout = setTimeout(() => {
        reject(new Error('Unity compile timeout'));
      }, 30000); // 30秒タイムアウト

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
          reject(new Error('Invalid compile response from Unity'));
        }
      });
    });
  }

  /**
   * Unityコンソールのログを取得
   */
  async getLogs(logType: string = 'All', maxCount: number = 100): Promise<{
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
      throw new Error('Unity MCP Bridge is not connected. Cannot get logs without Unity connection. Please ensure Unity is running and MCP server is started.');
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

      this.socket!.write(request + '\n');

      const timeout = setTimeout(() => {
        reject(new Error('Unity getLogs timeout'));
      }, 10000);

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
          reject(new Error('Invalid getLogs response from Unity'));
        }
      });
    });
  }

  /**
   * Unity Test Runnerを実行
   */
  async runTests(filterType: string = 'all', filterValue: string = '', saveXml: boolean = false): Promise<{
    success: boolean;
    message: string;
    testResults?: {
      PassedCount: number;
      FailedCount: number;
      SkippedCount: number;
      TotalCount: number;
      Duration: number;
    };
    xmlPath?: string;
    completedAt: string;
    error?: string;
  }> {
    if (!this.connected) {
      // 接続されていない場合は明確なエラーを投げる（ダミーデータは返さない）
      throw new Error('Unity MCP Bridge is not connected. Cannot execute tests without Unity connection. Please ensure Unity is running and MCP server is started.');
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

      this.socket!.write(request + '\n');

      const timeout = setTimeout(() => {
        reject(new Error('Unity runTests timeout'));
      }, 60000); // 60秒タイムアウト（テスト実行は時間がかかる）

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
          reject(new Error('Invalid runTests response from Unity'));
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