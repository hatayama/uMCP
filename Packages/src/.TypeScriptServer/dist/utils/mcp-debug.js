/**
 * MCP Safe Debug Logger
 * Development logging functions that output to file only when MCP_DEBUG is set
 */
import * as fs from 'fs';
import * as path from 'path';
import * as os from 'os';
// Create log file path with timestamp
const logDir = path.join(os.homedir(), '.claude', 'umcp-logs');
const timestamp = new Date().toISOString().replace(/[:.]/g, '-').split('T');
const dateStr = timestamp[0]; // YYYY-MM-DD
const timeStr = timestamp[1].split('.')[0]; // HH-MM-SS
const logFile = path.join(logDir, `mcp-debug-${dateStr}_${timeStr}.log`);
// Track if directory has been created
let directoryCreated = false;
// Helper function to write to file
const writeToFile = (message) => {
    try {
        // Create directory only when first write attempt is made
        if (!directoryCreated) {
            fs.mkdirSync(logDir, { recursive: true });
            directoryCreated = true;
        }
        const timestamp = new Date().toISOString();
        fs.appendFileSync(logFile, `${timestamp} ${message}\n`);
    }
    catch (error) {
        // Silent failure - no fallback needed since stderr is not visible
    }
};
/**
 * MCP development debug log
 * Only outputs to file when MCP_DEBUG environment variable is set
 */
export const mcpDebug = (...args) => {
    if (process.env.MCP_DEBUG) {
        const message = args.map(arg => typeof arg === 'object' ? JSON.stringify(arg, null, 2) : String(arg)).join(' ');
        writeToFile(`[MCP-DEBUG] ${message}`);
    }
};
/**
 * MCP development information log
 */
export const mcpInfo = (...args) => {
    if (process.env.MCP_DEBUG) {
        const message = args.map(arg => typeof arg === 'object' ? JSON.stringify(arg, null, 2) : String(arg)).join(' ');
        writeToFile(`[MCP-INFO] ${message}`);
    }
};
/**
 * MCP development warning log
 */
export const mcpWarn = (...args) => {
    if (process.env.MCP_DEBUG) {
        const message = args.map(arg => typeof arg === 'object' ? JSON.stringify(arg, null, 2) : String(arg)).join(' ');
        writeToFile(`[MCP-WARN] ${message}`);
    }
};
/**
 * MCP development error log
 */
export const mcpError = (...args) => {
    if (process.env.MCP_DEBUG) {
        const message = args.map(arg => typeof arg === 'object' ? JSON.stringify(arg, null, 2) : String(arg)).join(' ');
        writeToFile(`[MCP-ERROR] ${message}`);
    }
};
//# sourceMappingURL=mcp-debug.js.map