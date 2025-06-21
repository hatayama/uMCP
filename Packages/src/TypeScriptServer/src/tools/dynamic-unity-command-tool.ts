import { BaseTool } from './base-tool.js';
import { ToolContext } from '../types/tool-types.js';

/**
 * Dynamically generated tool for Unity commands
 */
export class DynamicUnityCommandTool extends BaseTool {
  public readonly name: string;
  public readonly description: string;
  public readonly inputSchema: any;
  private readonly commandName: string;

  constructor(context: ToolContext, commandName: string, description: string) {
    super(context);
    this.commandName = commandName;
    this.name = `unity-${commandName}`;
    this.description = description;
    this.inputSchema = {
      type: "object",
      properties: {
        random_string: {
          description: "Dummy parameter for no-parameter tools",
          type: "string"
        }
      },
      required: ["random_string"]
    };
  }

  validateArgs(args: unknown): any {
    return args || { random_string: "dummy" };
  }

  async execute(args: unknown): Promise<any> {
    try {
      const result = await this.context.unityClient.executeCommand(this.commandName, {});
      
      return {
        content: [{
          type: "text",
          text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
        }]
      };
    } catch (error) {
      return this.formatErrorResponse(error);
    }
  }

  protected formatErrorResponse(error: unknown) {
    const errorMessage = error instanceof Error ? error.message : "Unknown error";
    return {
      content: [{
        type: "text",
        text: `Failed to execute command '${this.commandName}': ${errorMessage}`
      }]
    };
  }
} 