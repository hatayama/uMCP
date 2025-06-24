/**
 * MCP Safe Debug Logger
 * Development logging functions that output to stderr without polluting stdout
 */
import * as fs from 'fs';
import * as path from 'path';
import * as os from 'os';

// Create log file path
const logDir = path.join(os.homedir(), 'tmp');
const logFile = path.join(logDir, 'mcp-debug.log');

// Ensure log directory exists
try {
  fs.mkdirSync(logDir, { recursive: true });
} catch (error) {
  // Directory already exists or creation failed, continue
}

// Helper function to write to file
const writeToFile = (message: string): void => {
  try {
    const timestamp = new Date().toISOString();
    fs.appendFileSync(logFile, `${timestamp} ${message}\n`);
  } catch (error) {
    // Fallback to stderr if file writing fails
    process.stderr.write(`[FILE-WRITE-ERROR] ${message}\n`);
  }
};

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
    writeToFile(`[MCP-DEBUG] ${message}`);
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
    writeToFile(`[MCP-INFO] ${message}`);
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
    writeToFile(`[MCP-WARN] ${message}`);
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
    writeToFile(`[MCP-ERROR] ${message}`);
  }
}; 