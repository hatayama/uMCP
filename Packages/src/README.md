# uMCP

Model Context Protocolを使用し、UnityエディターをLLMツールに接続します。

## 機能

### 1. unity.ping
- **説明**: Unity側へのpingテスト（TCP/IP通信確認）
- **パラメータ**: 
  - `message` (string): Unity側に送信するメッセージ（デフォルト: "Hello from TypeScript MCP Server"）

### 2. unity.compile
- **説明**: Unityプロジェクトのコンパイルを実行し、エラー情報を取得する
- **パラメータ**: 
  - `forceRecompile` (boolean): 強制再コンパイルを行うかどうか（デフォルト: false）

### 3. unity.getLogs
- **説明**: Unityコンソールのログ情報を取得する
- **パラメータ**: 
  - `logType` (string): フィルタリングするログタイプ (Error, Warning, Log, All)（デフォルト: "All"）
  - `maxCount` (number): 取得する最大ログ数（デフォルト: 100）

### 4. unity.runTests
- **説明**: Unity Test Runnerを実行してテスト結果を取得する
- **パラメータ**: 
  - `filterType` (string): テストフィルターの種類 (all, fullclassname, namespace, testname, assembly)（デフォルト: "all"）
  - `filterValue` (string): フィルター値（filterTypeがall以外の場合に指定）（デフォルト: ""）
  - `saveXml` (boolean): テスト結果をXMLファイルとして保存するかどうか（デフォルト: false）

## 前提条件

⚠️ **重要**: 以下のソフトウェアが必要です

- **Unity 2020.3 以上**
- **Node.js 18.0 以上** ⭐ **必須** - MCP Serverの実行に必要
- **LLM エディタ** - MCPクライアントとして使用


## インストール

### Unity Package Manager

1. Unity Editorを開く
2. Window > Package Manager を開く
3. "+" ボタンをクリック
4. "Add package from git URL" を選択
5. 以下のURLを入力：
```
https://github.com/hatayama/uMCP.git?path=/Packages/src
```

### OpenUPM経由 (推奨)

### Unity Package ManagerでScoped registryを使用する方法
1. Project Settings ウィンドウを開き、Package Manager ページに移動します
2. Scoped Registries リストに以下のエントリを追加します：
```
Name：OpenUPM
URL: https://package.openupm.com
Scope(s)：io.github.hatayama.umcp
```

3. Package Manager ウィンドウを開き、My Registries セクションの "hatayama" ページに移動します

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
- **Package Manager経由**: `"/Users/username/UnityProject/Library/PackageCache/io.github.hatayama.umpc@[hash]/TypeScriptServer/dist/server.bundle.js"`
- **ローカル開発**: `"/Users/username/UnityProject/Packages/src/TypeScriptServer/dist/server.bundle.js"`

> **注意**: Package Manager経由でインストールした場合、パッケージは`Library/PackageCache`に配置され、ハッシュ付きのディレクトリ名になります。「Auto Configure Cursor」ボタンを使用することで、正しいパスが自動的に設定されます。

## 使用方法
Window > uMCP を選択。専用windowが立ち上がります。

## トラブルシューティング

### Node.js関連
- `node --version` でNode.js 18以上がインストールされているか確認
- パスが正しく設定されているか確認

nodejsのinstallは[こちら](https://nodejs.org/ja/download)

### Unity接続エラー
- Unity MCP Bridge が起動しているか確認（Window > Unity MCP）
- 設定したポートが他のアプリケーションで使用されていないか確認

### Cursor設定エラー
- `.cursor/mcp.json` のパスが正しいか確認
- JSON形式が正しいか確認
- CursorのTools & Integrations > MCP Toolsで認識されているか確認。0 tool enableや赤丸が表示されていたらCursorを再起動する

## License
MIT License

## Author
Masamichi Hatayama