import { BaseTool } from './base-tool.js';
import { PARAMETER_SCHEMA } from '../constants.js';
import { mcpDebug } from '../utils/mcp-debug.js';
/**
 * Dynamically generated tool for Unity commands
 */
export class DynamicUnityCommandTool extends BaseTool {
    name;
    description;
    inputSchema;
    commandName;
    constructor(context, commandName, description, parameterSchema) {
        super(context);
        this.commandName = commandName;
        this.name = commandName;
        this.description = description;
        this.inputSchema = this.generateInputSchema(parameterSchema);
    }
    generateInputSchema(parameterSchema) {
        mcpDebug(`[DynamicUnityCommandTool] Generating schema for ${this.commandName}:`, parameterSchema);
        if (!parameterSchema || !parameterSchema[PARAMETER_SCHEMA.PROPERTIES_PROPERTY] || Object.keys(parameterSchema[PARAMETER_SCHEMA.PROPERTIES_PROPERTY]).length === 0) {
            // For commands without parameters, return minimal schema without dummy parameters
            mcpDebug(`[DynamicUnityCommandTool] No parameters found for ${this.commandName}, using minimal schema`);
            return {
                type: "object",
                properties: {},
                additionalProperties: false
            };
        }
        const properties = {};
        const required = [];
        // Convert Unity parameter schema to JSON Schema format using constants
        for (const [propName, propInfo] of Object.entries(parameterSchema[PARAMETER_SCHEMA.PROPERTIES_PROPERTY])) {
            const info = propInfo;
            const property = {
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
    convertType(unityType) {
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
    validateArgs(args) {
        // If no real parameters are defined, return empty object
        if (!this.inputSchema.properties || Object.keys(this.inputSchema.properties).length === 0) {
            return {};
        }
        return args || {};
    }
    async execute(args) {
        try {
            // Validate and use the provided arguments
            const actualArgs = this.validateArgs(args);
            const result = await this.context.unityClient.executeCommand(this.commandName, actualArgs);
            return {
                content: [{
                        type: "text",
                        text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
                    }]
            };
        }
        catch (error) {
            return this.formatErrorResponse(error);
        }
    }
    formatErrorResponse(error) {
        const errorMessage = error instanceof Error ? error.message : "Unknown error";
        return {
            content: [{
                    type: "text",
                    text: `Failed to execute command '${this.commandName}': ${errorMessage}`
                }]
        };
    }
}
//# sourceMappingURL=dynamic-unity-command-tool.js.map