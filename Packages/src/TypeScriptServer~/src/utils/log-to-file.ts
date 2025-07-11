/**
 * File-based Logger for MCP Server
 *
 * Design document reference: Packages/src/TypeScriptServer~/ARCHITECTURE.md
 *
 * Related classes:
 * - All server classes use this utility for safe logging
 * - UnityMcpServer: Uses for main server logging
 * - UnityClient: Uses for connection and communication logging
 * - UnityConnectionManager: Uses for connection management logging
 * - UnityToolManager: Uses for tool lifecycle logging
 * - McpClientCompatibility: Uses for client compatibility logging
 * - UnityEventHandler: Uses for event and shutdown logging
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
 * Logs are written to: {project_root}/ULoopMCPLogs/mcp-debug-YYYY-MM-DD_HH-MM-SS.log
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
import { fileURLToPath } from 'url';

// Security: Path validation to ensure paths are within expected directories
const isValidPath = (filePath: string): boolean => {
  const normalized = path.normalize(filePath);
  return !normalized.includes('..') && path.isAbsolute(normalized);
};

// Create log file path with timestamp
// Find project root by looking for specific Unity project files
const findProjectRoot = (): string => {
  const __filename = fileURLToPath(import.meta.url);
  let currentDir = path.dirname(__filename);
  let searchDepth = 0;
  const maxSearchDepth = 10; // Prevent infinite loops

  while (searchDepth < maxSearchDepth) {
    const parentDir = path.dirname(currentDir);

    // Check if we've reached the root directory
    if (currentDir === parentDir) {
      break;
    }

    // Check for Unity project indicators
    const unityIndicators = ['ProjectSettings', 'Assets', 'Packages'];

    const hasUnityFiles = unityIndicators.every((indicator) => {
      try {
        const checkPath = path.join(currentDir, indicator);
        // eslint-disable-next-line security/detect-non-literal-fs-filename
        return isValidPath(checkPath) && fs.existsSync(checkPath);
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

const logDir = path.join(findProjectRoot(), 'ULoopMCPLogs');
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
      if (!isValidPath(logDir)) {
        return; // Security: Invalid path detected
      }
      // eslint-disable-next-line security/detect-non-literal-fs-filename
      fs.mkdirSync(logDir, { recursive: true });
      directoryCreated = true;
    }

    if (!isValidPath(logFile)) {
      return; // Security: Invalid file path detected
    }
    const timestamp = new Date().toISOString();
    // eslint-disable-next-line security/detect-non-literal-fs-filename
    fs.appendFileSync(logFile, `${timestamp} ${message}\n`);
  } catch (error) {
    // Silent failure - no fallback needed since stderr is not visible
  }
};

/**
 * MCP development debug log
 * Only outputs to file when MCP_DEBUG environment variable is set
 */
export const debugToFile = (...args: unknown[]): void => {
  if (process.env.MCP_DEBUG) {
    const message = args
      .map((arg) => (typeof arg === 'object' ? JSON.stringify(arg, null, 2) : String(arg)))
      .join(' ');
    writeToFile(`[MCP-DEBUG] ${message}`);
  }
};

/**
 * MCP development information log
 */
export const infoToFile = (...args: unknown[]): void => {
  if (process.env.MCP_DEBUG) {
    const message = args
      .map((arg) => (typeof arg === 'object' ? JSON.stringify(arg, null, 2) : String(arg)))
      .join(' ');
    writeToFile(`[MCP-INFO] ${message}`);
  }
};

/**
 * MCP development warning log
 */
export const warnToFile = (...args: unknown[]): void => {
  if (process.env.MCP_DEBUG) {
    const message = args
      .map((arg) => (typeof arg === 'object' ? JSON.stringify(arg, null, 2) : String(arg)))
      .join(' ');
    writeToFile(`[MCP-WARN] ${message}`);
  }
};

/**
 * MCP development error log
 */
export const errorToFile = (...args: unknown[]): void => {
  if (process.env.MCP_DEBUG) {
    const message = args
      .map((arg) => (typeof arg === 'object' ? JSON.stringify(arg, null, 2) : String(arg)))
      .join(' ');
    writeToFile(`[MCP-ERROR] ${message}`);
  }
};
