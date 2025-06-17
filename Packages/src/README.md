[![Unity](https://img.shields.io/badge/Unity-2020.3+-red.svg)](https://unity3d.com/)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE.md)

# uMCP

Model Context Protocolを使用し、UnityエディターをLLMツールに接続します。
それにより、下記の機能を呼び出せるようになります。

## ✨機能

### 1. unity.compile
- **説明**: Unityプロジェクトのコンパイルを実行し、コンパイル結果を取得する
- **パラメータ**: 
  - `forceRecompile` (boolean): 強制再コンパイルを行うかどうか（デフォルト: false）
- **レスポンス**: 
  - `success` (boolean): コンパイルが成功したかどうか
  - `errors` (array): コンパイルエラーの配列（エラーがある場合）
    - `message` (string): エラーメッセージ
    - `file` (string): エラーが発生したファイルパス
    - `line` (number): エラーが発生した行番号
    - `column` (number): エラーが発生した列番号
    - `errorCode` (string): エラーコード（CS0117など）
  - `warnings` (array): コンパイル警告の配列（警告がある場合）
    - `message` (string): 警告メッセージ
    - `file` (string): 警告が発生したファイルパス
    - `line` (number): 警告が発生した行番号
    - `column` (number): 警告が発生した列番号
    - `warningCode` (string): 警告コード（CS0162など）
  - `compileTime` (number): コンパイル所要時間（ミリ秒）

### 2. unity.getLogs
- **説明**: Unityコンソールのログ情報を取得する
- **パラメータ**: 
  - `logType` (string): フィルタリングするログタイプ (Error, Warning, Log, All)（デフォルト: "All"）
  - `maxCount` (number): 取得する最大ログ数（デフォルト: 100）
- **レスポンス**: 
  - `logs` (array): ログエントリの配列
    - `type` (string): ログタイプ (Error, Warning, Log)
    - `message` (string): ログメッセージ
    - `stackTrace` (string): スタックトレース
    - `timestamp` (string): ログの時刻
  - `totalCount` (number): 取得したログの総数
- **注意**
  - 現状、Consoleの表示と連動しています。フィルターで非表示になっているlog typeは取得できません。これは将来的に改善する予定です

### 3. unity.runTests
- **説明**: Unity Test Runnerを実行してテスト結果を取得する
- **パラメータ**: 
  - `filterType` (string): テストフィルターの種類 (all, fullclassname, namespace, testname, assembly)（デフォルト: "all"）
  - `filterValue` (string): フィルター値（filterTypeがall以外の場合に指定）（デフォルト: ""）
  - `saveXml` (boolean): テスト結果をXMLファイルとして保存するかどうか（デフォルト: false）
- **レスポンス**: 
  - `success` (boolean): テスト実行が成功したかどうか
  - `totalTests` (number): 実行されたテストの総数
  - `passedTests` (number): 成功したテスト数
  - `failedTests` (number): 失敗したテスト数
  - `skippedTests` (number): スキップされたテスト数
  - `executionTime` (number): テスト実行時間（秒）
  - `results` (array): 個別テスト結果の配列
    - `name` (string): テスト名
    - `fullName` (string): テストのフルネーム
    - `status` (string): テスト結果 (Passed, Failed, Skipped)
    - `duration` (number): テスト実行時間（秒）
    - `errorMessage` (string): エラーメッセージ（失敗時）
    - `stackTrace` (string): スタックトレース（失敗時）
    - `assertionFailures` (array): アサーション失敗の詳細（失敗時）
  - `xmlPath` (string): XMLファイルのパス（saveXmlがtrueの場合）
 
### 4. unity.ping
- **説明**: Unity側へのpingテスト（TCP/IP通信確認）
- **パラメータ**: 
  - `message` (string): Unity側に送信するメッセージ（デフォルト: "Hello from TypeScript MCP Server"）
- **レスポンス**: 
  - `success` (boolean): 通信が成功したかどうか
  - `response` (string): Unity側からの応答メッセージ
  - `responseTime` (number): 応答時間（ミリ秒）

[現状は上記の組み込み機能しか使えませんが、将来的にpackage外で自由にコマンドを増やす事ができる機能を追加する予定です]

## 使用方法
1. Window > uMCP を選択。専用windowが立ち上がります。"Start Server"ボタンを押します。
<img width="400" alt="image" src="https://github.com/user-attachments/assets/0a1b5ed4-56a9-4209-b2e7-0acbca3cb9a9" />


下記のように表示が変われば成功です。


<img width="400" alt="image" src="https://github.com/user-attachments/assets/9f5d8294-2cde-4d30-ab22-f527e6c3bf66" />



2. 次はLLM Tool Settings項目で接続先のIDEを選択します。"設定を自動構成" ボタンを押すと、自動的にIDEに接続されます。

<img width="400" alt="image" src="https://github.com/user-attachments/assets/379fe674-dee7-4962-9d93-6f43fca13227" />


3. IDEの接続確認
  - 例えばCursorでは、設定ページのTools & Integrationsをチェックして、unity-mcp-{port番号} を見つけます。toggleをクリックし、mcpが動いた状態にしてください。黄色や赤の丸が表示される場合、Cursorを再起動してください。
<img width="657" alt="image" src="https://github.com/user-attachments/assets/14352ec0-c0a4-443d-98d5-35a6c86acd45" />

4. 手動設定 (通常は不要です)
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
> **注意**: Package Manager経由でインストールした場合、パッケージは`Library/PackageCache`に配置され、ハッシュ付きのディレクトリ名になります。「Auto Configure Cursor」ボタンを使用することで、正しいパスが自動的に設定されます。

5. 複数Unity起動への対応
  - port番号を変更する事で、複数のUnity起動に対応しています

## 前提条件

⚠️ **重要**: 以下のソフトウェアが必要です
- **Unity 2020.3 以上**
- **Node.js 18.0 以上** ⭐ **必須** - MCP Serverの実行に必要
- node.jsのinstallは[こちら](https://nodejs.org/ja/download)


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

### Node.js関連
- `node --version` でNode.js 18以上がインストールされているか確認
- パスが正しく設定されているか確認

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
