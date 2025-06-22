/**
 * MCP Safe Debug Logger
 * stdout を汚さずに stderr にログ出力する開発用ログ関数
 */

/**
 * MCP開発用デバッグログ
 * MCP_DEBUG環境変数が設定されている時のみ stderr にログ出力
 * stdout は JSON-RPC メッセージ専用なので絶対に汚してはいけない
 */
export const mcpDebug = (...args: any[]): void => {
  if (process.env.MCP_DEBUG) {
    const message = args.map(arg => 
      typeof arg === 'object' ? JSON.stringify(arg, null, 2) : String(arg)
    ).join(' ');
    process.stderr.write(`[MCP-DEBUG] ${message}\n`);
  }
};

/**
 * MCP開発用情報ログ
 */
export const mcpInfo = (...args: any[]): void => {
  if (process.env.MCP_DEBUG) {
    const message = args.map(arg => 
      typeof arg === 'object' ? JSON.stringify(arg, null, 2) : String(arg)
    ).join(' ');
    process.stderr.write(`[MCP-INFO] ${message}\n`);
  }
};

/**
 * MCP開発用警告ログ
 */
export const mcpWarn = (...args: any[]): void => {
  if (process.env.MCP_DEBUG) {
    const message = args.map(arg => 
      typeof arg === 'object' ? JSON.stringify(arg, null, 2) : String(arg)
    ).join(' ');
    process.stderr.write(`[MCP-WARN] ${message}\n`);
  }
};

/**
 * MCP開発用エラーログ
 */
export const mcpError = (...args: any[]): void => {
  if (process.env.MCP_DEBUG) {
    const message = args.map(arg => 
      typeof arg === 'object' ? JSON.stringify(arg, null, 2) : String(arg)
    ).join(' ');
    process.stderr.write(`[MCP-ERROR] ${message}\n`);
  }
}; 