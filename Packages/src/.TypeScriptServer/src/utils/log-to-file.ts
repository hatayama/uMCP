/**
 * File-based Logger for MCP Server
 * 
 * Why file output?
 * MCP (Model Context Protocol) servers communicate with clients via JSON-RPC over stdout/stdin.
 * This means we CANNOT use console.log() or any stdout output for debugging, as it would
 * corrupt the JSON-RPC communication channel and cause the MCP connection to fail.
 * 
 * Even stderr (console.error) can be problematic in some environments, so file-based
 * logging is the safest approach for debugging MCP servers.
 * 
 * Where are logs written?
 * Logs are written to: {project_root}/UmcpLogs/mcp-debug-YYYY-MM-DD_HH-MM-SS.log
 * - Each server session creates a new timestamped log file
 * - Directory is created automatically on first log write
 * - Logs are only written when MCP_DEBUG environment variable is set
 * 
 * Usage:
 * Set MCP_DEBUG=true when starting the server to enable logging
 * Example: MCP_DEBUG=true npm start
 * 
 * Log functions:
 * - mcpDebug(): Debug-level messages
 * - mcpInfo(): Information messages
 * - mcpWarn(): Warning messages
 * - mcpError(): Error messages (only one that logs regardless of MCP_DEBUG)
 */
import * as fs from 'fs';
import * as path from 'path';
import * as os from 'os';

// Create log file path with timestamp
// Find project root by looking for specific Unity project files
const findProjectRoot = (): string => {
  let currentDir = __dirname;
  let searchDepth = 0;
  const maxSearchDepth = 10; // Prevent infinite loops
  
  while (searchDepth < maxSearchDepth) {
    const parentDir = path.dirname(currentDir);
    
    // Check if we've reached the root directory
    if (currentDir === parentDir) {
      break;
    }
    
    // Check for Unity project indicators
    const unityIndicators = [
      'ProjectSettings',
      'Assets', 
      'Packages'
    ];
    
    const hasUnityFiles = unityIndicators.every(indicator => {
      try {
        return fs.existsSync(path.join(currentDir, indicator));
      } catch {
        return false;
      }
    });
    
    if (hasUnityFiles) {
      return currentDir;
    }
    
    currentDir = parentDir;
    searchDepth++;
  }
  
  // Fallback to current working directory if project root not found
  return process.cwd();
};

const logDir = path.join(findProjectRoot(), 'UmcpLogs');
const timestamp = new Date().toISOString().replace(/[:.]/g, '-').split('T');
const dateStr = timestamp[0]; // YYYY-MM-DD
const timeStr = timestamp[1].split('.')[0]; // HH-MM-SS
const logFile = path.join(logDir, `mcp-debug-${dateStr}_${timeStr}.log`);

// Track if directory has been created
let directoryCreated = false;

// Helper function to write to file
const writeToFile = (message: string): void => {
  try {
    // Create directory only when first write attempt is made
    if (!directoryCreated) {
      fs.mkdirSync(logDir, { recursive: true });
      directoryCreated = true;
    }
    
    const timestamp = new Date().toISOString();
    fs.appendFileSync(logFile, `${timestamp} ${message}\n`);
  } catch (error) {
    // Silent failure - no fallback needed since stderr is not visible
  }
};

/**
 * MCP development debug log
 * Only outputs to file when MCP_DEBUG environment variable is set
 */
export const debugToFile = (...args: any[]): void => {
  if (process.env.MCP_DEBUG) {
    const message = args.map(arg => 
      typeof arg === 'object' ? JSON.stringify(arg, null, 2) : String(arg)
    ).join(' ');
    writeToFile(`[MCP-DEBUG] ${message}`);
  }
};

/**
 * MCP development information log
 */
export const infoToFile = (...args: any[]): void => {
  if (process.env.MCP_DEBUG) {
    const message = args.map(arg => 
      typeof arg === 'object' ? JSON.stringify(arg, null, 2) : String(arg)
    ).join(' ');
    writeToFile(`[MCP-INFO] ${message}`);
  }
};

/**
 * MCP development warning log
 */
export const warnToFile = (...args: any[]): void => {
  if (process.env.MCP_DEBUG) {
    const message = args.map(arg => 
      typeof arg === 'object' ? JSON.stringify(arg, null, 2) : String(arg)
    ).join(' ');
    writeToFile(`[MCP-WARN] ${message}`);
  }
};

/**
 * MCP development error log
 */
export const errorToFile = (...args: any[]): void => {
  if (process.env.MCP_DEBUG) {
    const message = args.map(arg => 
      typeof arg === 'object' ? JSON.stringify(arg, null, 2) : String(arg)
    ).join(' ');
    writeToFile(`[MCP-ERROR] ${message}`);
  }
}; 