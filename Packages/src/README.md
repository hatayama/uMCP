# uMCP

Model Context Protocolを使用し、UnityエディターをLLMツールに接続します。

## 機能

- Unityプロジェクトのコンパイル実行・エラー取得
- Unityコンソールログの取得
- LLMツールからUnityを直接操作

## 前提条件

⚠️ **重要**: 以下のソフトウェアが必要です

- **Unity 2020.3 以上**
- **Node.js 18.0 以上** ⭐ **必須** - MCP Serverの実行に必要
- **LLM エディタ** - MCP クライアントとして使用

## インストール

### Unity Package Manager via Git URL

1. Unity Editorを開く
2. Window > Package Manager を開く
3. "+" ボタンをクリック
4. "Add package from git URL" を選択
5. 以下のURLを入力：
```
https://github.com/hatayama/uMCP.git?path=/Packages/src
```

### セットアップ

#### 1. Unity側の設定

パッケージインストール後、Unity Editorで以下を実行：
- Window > Unity MCP を開く
- "Start Server" ボタンをクリック
- 設定したportでTCP/IPサーバーが起動

#### 2. LLMツールの設定

ここでは例としてCursorで説明します。
Unity Editorで「Window > Unity MCP」を開き、「Auto Configure Cursor」ボタンをクリックしてください。
これにより`.cursor/mcp.json`が自動的に作成・更新されます。

**手動設定**:
必要に応じて、Cursorの設定ファイル（`.cursor/mcp.json`）を手動で編集することも可能です：

```json
{
  "mcpServers": {
    "unity-mcp-{設定したport}": {
      "command": "node",
      "args": [
        "[Unity Package Path]/TypeScriptServer/dist/server.bundle.js"
      ],
      "env": {
        "UNITY_TCP_PORT": "{設定したport}"
      }
    }
  }
}
```

**パス例**:
- **Package Manager経由**: `"/Users/username/UnityProject/Library/PackageCache/io.github.hatayama.unitypocketmcp@[hash]/TypeScriptServer/dist/server.bundle.js"`
- **ローカル開発**: `"/Users/username/UnityProject/Packages/src/TypeScriptServer/dist/server.bundle.js"`

> **注意**: Package Manager経由でインストールした場合、パッケージは`Library/PackageCache`に配置され、ハッシュ付きのディレクトリ名になります。「Auto Configure Cursor」ボタンを使用することで、正しいパスが自動的に設定されます。

## 使用方法

Cursorで以下のコマンドが使用可能：

- `unity.ping` - Unity側への接続テスト
- `action.compileUnity` - Unityプロジェクトのコンパイル実行
- `context.getUnityLogs` - Unityコンソールログの取得

## トラブルシューティング

### Node.js関連
- `node --version` でNode.js 18以上がインストールされているか確認
- パスが正しく設定されているか確認

### Unity接続エラー
- Unity MCP Bridge が起動しているか確認（Window > Unity MCP）
- 設定したポートが他のアプリケーションで使用されていないか確認

### Cursor設定エラー
- `.cursor/mcp.json` のパスが正しいか確認
- JSON形式が正しいか確認

## License
MIT License

## Author
Masamichi Hatayama