import { UnityClient } from './unity-client.js';
import { DynamicUnityCommandTool } from './tools/dynamic-unity-command-tool.js';
import { Tool } from '@modelcontextprotocol/sdk/types.js';
import { errorToFile, debugToFile, infoToFile } from './utils/log-to-file.js';
import { ENVIRONMENT } from './constants.js';

/**
 * Unity Tool Manager - Manages dynamic tool generation and management
 *
 * Design document reference: Packages/src/Editor/ARCHITECTURE.md
 *
 * Related classes:
 * - UnityClient: Manages communication with Unity Editor
 * - DynamicUnityCommandTool: Implementation of dynamic tools
 * - UnityMcpServer: Main server class that uses this manager
 *
 * Key features:
 * - Dynamic tool generation from Unity commands
 * - Tool refresh management
 * - Command details fetching and parsing
 * - Development mode support
 */
export class UnityToolManager {
  private unityClient: UnityClient;
  private readonly isDevelopment: boolean;
  private readonly dynamicTools: Map<string, DynamicUnityCommandTool> = new Map();
  private isRefreshing: boolean = false;
  private clientName: string = '';

  constructor(unityClient: UnityClient) {
    this.unityClient = unityClient;
    this.isDevelopment = process.env.NODE_ENV === ENVIRONMENT.NODE_ENV_DEVELOPMENT;
  }

  /**
   * Set client name for Unity communication
   */
  setClientName(clientName: string): void {
    this.clientName = clientName;
  }

  /**
   * Get dynamic tools map
   */
  getDynamicTools(): Map<string, DynamicUnityCommandTool> {
    return this.dynamicTools;
  }

  /**
   * Get tools from Unity
   */
  async getToolsFromUnity(): Promise<Tool[]> {
    if (!this.unityClient.connected) {
      return [];
    }

    try {
      const commandDetails = await this.fetchCommandDetailsFromUnity();

      if (!commandDetails) {
        return [];
      }

      this.createDynamicToolsFromCommands(commandDetails);

      // Convert dynamic tools to Tool array
      const tools: Tool[] = [];
      for (const [toolName, dynamicTool] of this.dynamicTools) {
        tools.push({
          name: toolName,
          description: dynamicTool.description,
          inputSchema: dynamicTool.inputSchema,
        });
      }

      return tools;
    } catch (error) {
      errorToFile('[Unity Tool Manager] Failed to get tools from Unity:', error);
      return [];
    }
  }

  /**
   * Initialize dynamic Unity command tools
   */
  async initializeDynamicTools(): Promise<void> {
    try {
      await this.unityClient.ensureConnected();

      const commandDetails = await this.fetchCommandDetailsFromUnity();
      if (!commandDetails) {
        return;
      }

      this.createDynamicToolsFromCommands(commandDetails);

      // Command details processed successfully
    } catch (error) {
      errorToFile('[Unity Tool Manager] Failed to initialize dynamic tools:', error);
      // Continue without dynamic tools
    }
  }

  /**
   * Fetch command details from Unity
   */
  private async fetchCommandDetailsFromUnity(): Promise<unknown[] | null> {
    // Get detailed command information including schemas
    // Include development-only commands if in development mode
    const params = { IncludeDevelopmentOnly: this.isDevelopment };
    const commandDetailsResponse = await this.unityClient.executeCommand(
      'get-command-details',
      params,
    );

    // Handle new GetCommandDetailsResponse structure
    const commandDetails =
      (commandDetailsResponse as { Commands?: unknown[] })?.Commands || commandDetailsResponse;
    if (!Array.isArray(commandDetails)) {
      errorToFile('[Unity Tool Manager] Invalid command details response:', commandDetailsResponse);
      return null;
    }

    return commandDetails;
  }

  /**
   * Create dynamic tools from Unity command details
   */
  private createDynamicToolsFromCommands(commandDetails: unknown[]): void {
    // Create dynamic tools for each Unity command
    this.dynamicTools.clear();
    const toolContext = { unityClient: this.unityClient };

    for (const commandInfo of commandDetails) {
      const commandName = (commandInfo as { name: string }).name;
      const description =
        (commandInfo as { description?: string }).description ||
        `Execute Unity command: ${commandName}`;
      const parameterSchema = (commandInfo as { parameterSchema?: unknown }).parameterSchema;
      const displayDevelopmentOnly =
        (commandInfo as { displayDevelopmentOnly?: boolean }).displayDevelopmentOnly || false;

      // Skip development-only commands in production mode
      if (displayDevelopmentOnly && !this.isDevelopment) {
        continue;
      }

      const toolName = commandName;

      const dynamicTool = new DynamicUnityCommandTool(
        toolContext,
        commandName,
        description,
        parameterSchema, // Pass schema information
      );

      this.dynamicTools.set(toolName, dynamicTool);
    }
  }

  /**
   * Refresh dynamic tools by re-fetching from Unity
   * This method can be called to update the tool list when Unity commands change
   */
  async refreshDynamicTools(sendNotification?: () => void): Promise<void> {
    await this.initializeDynamicTools();
    
    // Send tools changed notification to MCP client if callback provided
    if (sendNotification) {
      sendNotification();
    }
  }

  /**
   * Safe version of refreshDynamicTools that prevents duplicate execution
   */
  async refreshDynamicToolsSafe(sendNotification?: () => void): Promise<void> {
    if (this.isRefreshing) {
      if (this.isDevelopment) {
        debugToFile('[Unity Tool Manager] refreshDynamicToolsSafe skipped: already in progress');
      }
      return;
    }

    this.isRefreshing = true;
    try {
      if (this.isDevelopment) {
        const stack = new Error().stack;
        const callerLine = stack?.split('\n')[2]?.trim() || 'Unknown caller';
        const timestamp = new Date().toISOString().split('T')[1].slice(0, 12);
        debugToFile(`[Unity Tool Manager] refreshDynamicToolsSafe called at ${timestamp} from: ${callerLine}`);
      }

      await this.refreshDynamicTools(sendNotification);
    } finally {
      this.isRefreshing = false;
    }
  }

  /**
   * Check if tool exists
   */
  hasTool(toolName: string): boolean {
    return this.dynamicTools.has(toolName);
  }

  /**
   * Get tool by name
   */
  getTool(toolName: string): DynamicUnityCommandTool | undefined {
    return this.dynamicTools.get(toolName);
  }

  /**
   * Get all tools as array
   */
  getAllTools(): Tool[] {
    const tools: Tool[] = [];
    for (const [toolName, dynamicTool] of this.dynamicTools) {
      tools.push({
        name: toolName,
        description: dynamicTool.description,
        inputSchema: dynamicTool.inputSchema,
      });
    }
    return tools;
  }

  /**
   * Get tools count
   */
  getToolsCount(): number {
    return this.dynamicTools.size;
  }
}