import { BaseTool } from './base-tool.js';
import { ToolContext } from '../types/tool-types.js';
import { PARAMETER_SCHEMA, TOOL_NAMES } from '../constants.js';

/**
 * Tool to get available Unity commands
 */
export class GetAvailableCommandsTool extends BaseTool {
  name = TOOL_NAMES.GET_AVAILABLE_COMMANDS;
  description = 'Get list of available Unity commands';
  inputSchema = {
    type: 'object',
    properties: {},
    additionalProperties: false,
  };

  validateArgs(args: unknown) {
    return args || {};
  }

  async execute(args: unknown) {
    try {
      // Only get command details (which includes names)
      // BUG FIX: Removed duplicate API call to getAvailableCommands
      // Previously called both getAvailableCommands() and getCommandDetails(),
      // causing redundant Unity API calls when multiple MCP servers were connected
      const commandDetails = await this.context.unityClient.getCommandDetails();
      const commands = commandDetails.map((cmd: any) => cmd.Name || cmd.name).filter(Boolean);

      let responseText = `Available Unity Commands (${commands.length}):\n\n`;

      commandDetails.forEach((cmd: any, index: number) => {
        responseText += `${index + 1}. **${cmd.Name || cmd.name}**\n`;
        responseText += `   Description: ${cmd.Description || cmd.description}\n`;

        // Add parameter schema information using constants
        const schema = cmd.ParameterSchema || cmd.parameterSchema;
        if (
          schema &&
          schema[PARAMETER_SCHEMA.PROPERTIES_PROPERTY] &&
          Object.keys(schema[PARAMETER_SCHEMA.PROPERTIES_PROPERTY]).length > 0
        ) {
          responseText += '   Parameters:\n';
          Object.entries(schema[PARAMETER_SCHEMA.PROPERTIES_PROPERTY]).forEach(
            ([paramName, paramInfo]: [string, any]) => {
              responseText += `     - ${paramName} (${paramInfo[PARAMETER_SCHEMA.TYPE_PROPERTY]}): ${paramInfo[PARAMETER_SCHEMA.DESCRIPTION_PROPERTY]}`;
              if (paramInfo[PARAMETER_SCHEMA.DEFAULT_VALUE_PROPERTY] !== undefined) {
                responseText += ` [default: ${paramInfo[PARAMETER_SCHEMA.DEFAULT_VALUE_PROPERTY]}]`;
              }
              // Use constants for enum property access
              if (
                paramInfo[PARAMETER_SCHEMA.ENUM_PROPERTY] &&
                paramInfo[PARAMETER_SCHEMA.ENUM_PROPERTY].length > 0
              ) {
                responseText += ` [options: ${paramInfo[PARAMETER_SCHEMA.ENUM_PROPERTY].join(', ')}]`;
              }
              responseText += '\n';
            },
          );
        } else {
          responseText += '   Parameters: None\n';
        }
        responseText += '\n';
      });

      return {
        content: [
          {
            type: 'text',
            text: responseText,
          },
        ],
      };
    } catch (error) {
      return this.formatErrorResponse(error);
    }
  }

  formatErrorResponse(error: unknown) {
    const errorMessage = error instanceof Error ? error.message : 'Unknown error';
    return {
      content: [
        {
          type: 'text',
          text: `Failed to get available commands: ${errorMessage}`,
        },
      ],
    };
  }
}
