import { BaseTool } from './base-tool.js';
import { ToolContext, ToolResponse } from '../types/tool-types.js';
import { PARAMETER_SCHEMA } from '../constants.js';

// Type definitions for Unity parameter schema
interface UnityParameterInfo {
  [key: string]: unknown;
}

interface UnityParameterSchema {
  [key: string]: unknown;
}

interface JsonSchemaProperty {
  type: string;
  description?: string;
  default?: unknown;
  enum?: string[];
  items?: { type: string };
}

interface InputSchema {
  type: string;
  properties: Record<string, JsonSchemaProperty>;
  required?: string[];
  additionalProperties?: boolean;
}

/**
 * Dynamically generated tool for Unity commands
 *
 * Design document reference: Packages/src/TypeScriptServer~/ARCHITECTURE.md
 *
 * Related classes:
 * - UnityMcpServer: Instantiates and uses this tool
 * - UnityClient: Used to execute the actual command in Unity
 * - BaseTool: Base class providing common tool functionality
 */
export class DynamicUnityCommandTool extends BaseTool {
  public readonly name: string;
  public readonly description: string;
  public readonly inputSchema: InputSchema;
  private readonly commandName: string;

  constructor(
    context: ToolContext,
    commandName: string,
    description: string,
    parameterSchema?: UnityParameterSchema,
  ) {
    super(context);
    this.commandName = commandName;
    this.name = commandName;
    this.description = description;
    this.inputSchema = this.generateInputSchema(parameterSchema);
  }

  private generateInputSchema(parameterSchema?: UnityParameterSchema): InputSchema {
    if (this.hasNoParameters(parameterSchema)) {
      // For commands without parameters, return minimal schema without dummy parameters
      return {
        type: 'object',
        properties: {},
        additionalProperties: false,
      };
    }

    const properties: Record<string, JsonSchemaProperty> = {};
    const required: string[] = [];

    // Convert Unity parameter schema to JSON Schema format using constants
    const propertiesObj = parameterSchema[PARAMETER_SCHEMA.PROPERTIES_PROPERTY] as Record<
      string,
      UnityParameterInfo
    >;
    for (const [propName, propInfo] of Object.entries(propertiesObj)) {
      const info = propInfo;

      const property: JsonSchemaProperty = {
        type: this.convertType(String(info[PARAMETER_SCHEMA.TYPE_PROPERTY] || 'string')),
        description: String(
          info[PARAMETER_SCHEMA.DESCRIPTION_PROPERTY] || `Parameter: ${propName}`,
        ),
      };

      // Add default value if provided
      const defaultValue = info[PARAMETER_SCHEMA.DEFAULT_VALUE_PROPERTY];
      if (defaultValue !== undefined && defaultValue !== null) {
        property.default = defaultValue;
      }

      // Add enum values if provided using constants
      const enumValues = info[PARAMETER_SCHEMA.ENUM_PROPERTY];
      if (enumValues && Array.isArray(enumValues) && enumValues.length > 0) {
        property.enum = enumValues as string[];
      }

      // Handle array type
      if (
        info[PARAMETER_SCHEMA.TYPE_PROPERTY] === 'array' &&
        defaultValue &&
        Array.isArray(defaultValue)
      ) {
        property.items = {
          type: 'string',
        };
        property.default = defaultValue;
      }

      // eslint-disable-next-line security/detect-object-injection
      properties[propName] = property;
    }

    // Add required parameters using constants
    const requiredParams = parameterSchema[PARAMETER_SCHEMA.REQUIRED_PROPERTY];
    if (requiredParams && Array.isArray(requiredParams)) {
      required.push(...(requiredParams as string[]));
    }

    const schema = {
      type: 'object',
      properties: properties,
      required: required.length > 0 ? required : undefined,
    };

    return schema;
  }

  private convertType(unityType: string): string {
    switch (unityType?.toLowerCase()) {
      case 'string':
        return 'string';
      case 'number':
      case 'int':
      case 'float':
      case 'double':
        return 'number';
      case 'boolean':
      case 'bool':
        return 'boolean';
      case 'array':
        return 'array';
      default:
        return 'string'; // Default fallback
    }
  }

  private hasNoParameters(parameterSchema?: UnityParameterSchema): boolean {
    if (!parameterSchema) {
      return true;
    }

    const properties = parameterSchema[PARAMETER_SCHEMA.PROPERTIES_PROPERTY];
    if (!properties || typeof properties !== 'object') {
      return true;
    }

    return Object.keys(properties).length === 0;
  }

  validateArgs(args: unknown): Record<string, unknown> {
    // If no real parameters are defined, return empty object
    if (!this.inputSchema.properties || Object.keys(this.inputSchema.properties).length === 0) {
      return {};
    }

    return (args as Record<string, unknown>) || {};
  }

  async execute(args: Record<string, unknown>): Promise<ToolResponse> {
    try {
      // Validate and use the provided arguments
      const actualArgs: Record<string, unknown> = this.validateArgs(args);

      const result: unknown = await this.context.unityClient.executeCommand(
        this.commandName,
        actualArgs,
      );

      return {
        content: [
          {
            type: 'text',
            text: typeof result === 'string' ? result : JSON.stringify(result, null, 2),
          },
        ],
      };
    } catch (error) {
      return this.formatErrorResponse(error);
    }
  }

  protected formatErrorResponse(error: unknown): ToolResponse {
    const errorMessage = error instanceof Error ? error.message : 'Unknown error';
    return {
      content: [
        {
          type: 'text',
          text: `Failed to execute command '${this.commandName}': ${errorMessage}`,
        },
      ],
    };
  }
}
