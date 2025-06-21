import { BaseTool } from './base-tool.js';
import { ToolContext } from '../types/tool-types.js';

/**
 * Tool to get available Unity commands
 */
export class GetAvailableCommandsTool extends BaseTool {
  name = "unity-get-available-commands";
  description = "Get list of available Unity commands";
  inputSchema = {
    type: "object",
    properties: {},
    additionalProperties: false
  };

  validateArgs(args: unknown) {
    return args || {};
  }

  async execute(args: unknown) {
    try {
      const commands = await this.context.unityClient.getAvailableCommands();
      const commandDetails = await this.context.unityClient.getCommandDetails();
      
      let responseText = `Available Unity Commands (${commands.length}):\n\n`;
      
      commandDetails.forEach((cmd: any, index: number) => {
        responseText += `${index + 1}. **${cmd.Name || cmd.name}**\n`;
        responseText += `   Description: ${cmd.Description || cmd.description}\n\n`;
      });
      
      return {
        content: [{
          type: "text",
          text: responseText
        }]
      };
    } catch (error) {
      return this.formatErrorResponse(error);
    }
  }

  formatErrorResponse(error: unknown) {
    const errorMessage = error instanceof Error ? error.message : "Unknown error";
    return {
      content: [{
        type: "text",
        text: `Failed to get available commands: ${errorMessage}`
      }]
    };
  }
} 
 