#!/usr/bin/env node

import { Server } from '@modelcontextprotocol/sdk/server/index.js';
import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';
import {
  CallToolRequestSchema,
  ListToolsRequestSchema,
  Tool,
} from '@modelcontextprotocol/sdk/types.js';
import { UnityClient } from './unity-client.js';
import { ToolRegistry } from './tools/tool-registry.js';
import { ToolContext } from './types/tool-types.js';

/**
 * Unity MCP Server
 * CursorとUnity間の橋渡しを行うMCPサーバー
 */
class McpServer {
  private server: Server;
  private unityClient: UnityClient;
  private toolRegistry: ToolRegistry;

  constructor() {
    console.log('🚀 Unity MCP Server initializing...');
    this.server = new Server(
      {
        name: 'unity-mcp-server',
        version: '0.1.0',
      },
      {
        capabilities: {
          tools: {},
        },
      }
    );

    this.unityClient = new UnityClient();
    
    // ツールコンテキストを作成
    const context: ToolContext = {
      unityClient: this.unityClient
    };
    
    this.toolRegistry = new ToolRegistry(context);
    this.setupHandlers();
  }

  private setupHandlers(): void {
    // ツール一覧の提供
    this.server.setRequestHandler(ListToolsRequestSchema, async () => {
      const toolDefinitions = this.toolRegistry.getAllDefinitions();
      return {
        tools: toolDefinitions.map(def => ({
          name: def.name,
          description: def.description,
          inputSchema: def.inputSchema
        } as Tool))
      };
    });

    // ツール実行の処理
    this.server.setRequestHandler(CallToolRequestSchema, async (request) => {
      const { name, arguments: args } = request.params;
      const result = await this.toolRegistry.execute(name, args);
      
      // MCP SDKの期待する形式に変換
      return {
        content: result.content,
        isError: result.isError || false
      };
    });
  }

  /**
   * サーバーを開始する
   */
  async start(): Promise<void> {
    const transport = new StdioServerTransport();
    await this.server.connect(transport);
    
    console.error('Unity MCP Server started successfully');
  }

  /**
   * クリーンアップ
   */
  cleanup(): void {
    this.unityClient.disconnect();
  }
}

// サーバーを起動
const server = new McpServer();

// プロセス終了時のクリーンアップ
process.on('SIGINT', () => {
  server.cleanup();
  process.exit(0);
});

process.on('SIGTERM', () => {
  server.cleanup();
  process.exit(0);
});

server.start().catch((error) => {
  console.error('Failed to start Unity MCP Server:', error);
  process.exit(1);
}); 