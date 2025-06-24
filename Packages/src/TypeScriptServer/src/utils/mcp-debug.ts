/**
 * MCP Safe Debug Logger
 * Development logging functions that output to stderr without polluting stdout
 */

/**
 * MCP development debug log
 * Only outputs to stderr when MCP_DEBUG environment variable is set
 * stdout is dedicated to JSON-RPC messages and must never be polluted
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
 * MCP development information log
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
 * MCP development warning log
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
 * MCP development error log
 */
export const mcpError = (...args: any[]): void => {
  if (process.env.MCP_DEBUG) {
    const message = args.map(arg => 
      typeof arg === 'object' ? JSON.stringify(arg, null, 2) : String(arg)
    ).join(' ');
    process.stderr.write(`[MCP-ERROR] ${message}\n`);
  }
}; 