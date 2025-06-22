import { BaseTool } from './base-tool.js';
import { ToolContext } from '../types/tool-types.js';
import { PARAMETER_SCHEMA } from '../constants.js';
import { mcpDebug } from '../utils/mcp-debug.js';

/**
 * Dynamically generated tool for Unity commands
 */
export class DynamicUnityCommandTool extends BaseTool {
  public readonly name: string;
  public readonly description: string;
  public readonly inputSchema: any;
  private readonly commandName: string;

  constructor(context: ToolContext, commandName: string, description: string, parameterSchema?: any) {
    super(context);
    this.commandName = commandName;
    this.name = commandName;
    this.description = description;
    this.inputSchema = this.generateInputSchema(parameterSchema);
  }

  private generateInputSchema(parameterSchema?: any): any {
    mcpDebug(`[DynamicUnityCommandTool] Generating schema for ${this.commandName}:`, parameterSchema);
    
    if (!parameterSchema || !parameterSchema[PARAMETER_SCHEMA.PROPERTIES_PROPERTY] || Object.keys(parameterSchema[PARAMETER_SCHEMA.PROPERTIES_PROPERTY]).length === 0) {
      // Fallback to dummy schema for commands without parameters
      mcpDebug(`[DynamicUnityCommandTool] No schema found for ${this.commandName}, using dummy schema`);
      return {
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

    const properties: any = {};
    const required: string[] = [];

    // Convert Unity parameter schema to JSON Schema format using constants
    for (const [propName, propInfo] of Object.entries(parameterSchema[PARAMETER_SCHEMA.PROPERTIES_PROPERTY])) {
      const info = propInfo as any;
      
      const property: any = {
        type: this.convertType(info[PARAMETER_SCHEMA.TYPE_PROPERTY]),
        description: info[PARAMETER_SCHEMA.DESCRIPTION_PROPERTY] || `Parameter: ${propName}`
      };

      // Add default value if provided
      if (info[PARAMETER_SCHEMA.DEFAULT_VALUE_PROPERTY] !== undefined && info[PARAMETER_SCHEMA.DEFAULT_VALUE_PROPERTY] !== null) {
        property.default = info[PARAMETER_SCHEMA.DEFAULT_VALUE_PROPERTY];
      }

      // Add enum values if provided using constants
      if (info[PARAMETER_SCHEMA.ENUM_PROPERTY] && Array.isArray(info[PARAMETER_SCHEMA.ENUM_PROPERTY]) && info[PARAMETER_SCHEMA.ENUM_PROPERTY].length > 0) {
        property.enum = info[PARAMETER_SCHEMA.ENUM_PROPERTY];
      }

      // Handle array type
      if (info[PARAMETER_SCHEMA.TYPE_PROPERTY] === "array" && info[PARAMETER_SCHEMA.DEFAULT_VALUE_PROPERTY] && Array.isArray(info[PARAMETER_SCHEMA.DEFAULT_VALUE_PROPERTY])) {
        property.items = {
          type: "string"
        };
        property.default = info[PARAMETER_SCHEMA.DEFAULT_VALUE_PROPERTY];
      }

      properties[propName] = property;
    }

    // Add required parameters using constants
    if (parameterSchema[PARAMETER_SCHEMA.REQUIRED_PROPERTY] && Array.isArray(parameterSchema[PARAMETER_SCHEMA.REQUIRED_PROPERTY])) {
      required.push(...parameterSchema[PARAMETER_SCHEMA.REQUIRED_PROPERTY]);
    }

    const schema = {
      type: "object",
      properties: properties,
      required: required.length > 0 ? required : undefined
    };
    
    mcpDebug(`[DynamicUnityCommandTool] Generated schema for ${this.commandName}:`, schema);
    return schema;
  }

  private convertType(unityType: string): string {
    switch (unityType?.toLowerCase()) {
      case "string":
        return "string";
      case "number":
      case "int":
      case "float":
      case "double":
        return "number";
      case "boolean":
      case "bool":
        return "boolean";
      case "array":
        return "array";
      default:
        return "string"; // Default fallback
    }
  }

  validateArgs(args: unknown): any {
    // If no real parameters are defined, use dummy parameter
    if (!this.inputSchema.properties || Object.keys(this.inputSchema.properties).length === 1 && 'random_string' in this.inputSchema.properties) {
      return { random_string: "dummy" };
    }
    
    return args || {};
  }

  async execute(args: unknown): Promise<any> {
    try {
      // Pass the actual arguments to Unity (not the dummy ones)
      const actualArgs = this.validateArgs(args);
      const unityParams = actualArgs.random_string === "dummy" ? {} : actualArgs;
      
      const result = await this.context.unityClient.executeCommand(this.commandName, unityParams);
      
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