# uMCP

Unity と Cursor 間の橋渡しを行う Model Context Protocol (MCP) サーバーです。Unity エディターを LLM ツールに接続し、開発ワークフローを効率化します。

## 機能・提供ツール

### 1. unity.ping
- **説明**: Unity側への接続テスト（TCP/IP通信確認）
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
  - `filterValue` (string): フィルター値（filterTypeがall以外の場合に指定）
    - `fullclassname`: フルクラス名 (例: io.github.hatayama.uMCP.CompileCommandTests)
    - `namespace`: ネームスペース (例: io.github.hatayama.uMCP)
    - `testname`: 個別テスト名
    - `assembly`: アセンブリ名
  - `saveXml` (boolean): テスト結果をXMLファイルとして保存するかどうか（デフォルト: false）

## アーキテクチャ

### 設計原則
- **MVP (Model-View-Presenter) パターン**: Presenterがビジネスロジックを担当し、ViewはUIの表示のみに専念
- **高い凝集度**: 各コンポーネントが単一の責任を持つ
- **拡張性**: 新しいツールを簡単に追加できる設計
- **型安全性**: C# および TypeScript の型システムを活用

### 構成要素

```
Unity MCP (uMCP)
├── Unity Side (C#)
│   ├── API Layer - JSON-RPC プロセッサ
│   ├── Commands - 各機能の実装
│   ├── Tools - コンパイル、ログ、テスト実行
│   └── UI - Editor Window
├── TypeScript Server
│   ├── MCP Server - Cursor連携
│   ├── Unity Client - TCP/IP通信
│   └── Tools - 各種ツール実装
└── Cursor Integration
    └── MCP Tools - LLM連携
```

## 前提条件

⚠️ **重要**: 以下のソフトウェアが必要です

- **Unity 2022.3 以上**
- **Node.js 18.0 以上** ⭐ **必須** - MCP Serverの実行に必要
- **Cursor エディタ** - MCPクライアントとして使用

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

### OpenUPM経由

#### UPM with Scoped registry の使用方法
1. Project Settings window を開き Package Manager ページに移動
2. Scoped Registries リストに以下のエントリを追加：
```
Name: OpenUPM
URL: https://package.openupm.com
Scope(s): io.github.hatayama.umpc
```
3. Package Manager window を開き、My Registries セクションの "hatayama" ページに移動

## セットアップ

### 1. Unity側の設定

パッケージインストール後、Unity Editorで以下を実行：

1. **Window > Unity MCP** を開く
2. **"Start Server"** ボタンをクリック
3. 設定したportでTCP/IPサーバーが起動

### 2. TypeScript Server のビルド

#### 自動ビルド
- **GitHub Actions**: mainブランチプッシュ時に自動ビルド・コミット
- **postinstall**: npm install実行時に自動ビルド
- **prepublishOnly**: パッケージ公開前に自動ビルド

#### 手動ビルド
```bash
# 新しい環境・初回セットアップ
cd Packages/src/TypeScriptServer
npm install
npm run build

# 継続開発（node_modulesが既にある場合）
npm run build
```

#### ビルド確認
```bash
# node_modulesの存在確認
ls node_modules/ > /dev/null 2>&1 && echo "OK: npm run build可能" || echo "NG: npm install必要"

# TypeScriptコンパイラの確認
npx tsc --version || echo "npm install必要"
```

### 3. Cursor の設定

#### 自動設定（推奨）
Unity Editorで「**Window > Unity MCP**」を開き、「**Auto Configure Cursor**」ボタンをクリック。
これにより`.cursor/mcp.json`が自動的に作成・更新されます。

#### 手動設定
必要に応じて、Cursorの設定ファイル（`.cursor/mcp.json`）を手動で編集：

```json
{
  "mcpServers": {
    "unity-mcp-{設定したport}": {
      "command": "node",
      "args": [
        "[Unity Package Path]/TypeScriptServer/dist/server.js"
      ],
      "env": {
        "UNITY_TCP_PORT": "{設定したport}"
      }
    }
  }
}
```

**パス例**:
- **Package Manager経由**: `"/Users/username/UnityProject/Library/PackageCache/io.github.hatayama.umpc@[hash]/TypeScriptServer/dist/server.js"`
- **ローカル開発**: `"/Users/username/UnityProject/Packages/src/TypeScriptServer/dist/server.js"`

> **注意**: Package Manager経由でインストールした場合、パッケージは`Library/PackageCache`に配置され、ハッシュ付きのディレクトリ名になります。「Auto Configure Cursor」ボタンを使用することで、正しいパスが自動的に設定されます。

## 使用方法

1. Unity で **Window > Unity MCP** を選択し、専用ウィンドウを開く
2. **"Start Server"** でUnity側のTCP/IPサーバーを起動
3. Cursor で MCP ツールが利用可能になる

### 開発時の環境設定

#### 本番環境（通常利用）
```bash
npm start
```

#### 開発環境（デバッグ用pingツール有効）
```bash
npm run dev
# または
ENABLE_PING_TOOL=true npm start
```

## 開発者向け情報

### 新しいツールの追加方法

1. **Unity側 (C#)**: `Packages/src/Editor/Api/Commands/` に新しいコマンドクラスを作成
2. **TypeScript側**: `Packages/src/TypeScriptServer/src/tools/` に新しいツールクラスを作成
3. **レジストリ登録**: 各側のレジストリに新しいツールを登録

詳細は `Packages/src/TypeScriptServer/README.md` を参照してください。

### コーディング規約
- 型宣言は必須（`var` 禁止、明示的な型宣言を推奨）
- 早期return でネストを浅く保つ
- record型を活用した値オブジェクトの使用
- MVPパターンに従ったPresenter-View分離

## トラブルシューティング

### Node.js関連
- `node --version` でNode.js 18以上がインストールされているか確認
- Node.jsのインストールは[こちら](https://nodejs.org/ja/download)

### Unity接続エラー
- Unity MCP Bridge が起動しているか確認（Window > Unity MCP）
- 設定したポートが他のアプリケーションで使用されていないか確認
- ポート7400がデフォルトで使用されます

### Cursor設定エラー
- `.cursor/mcp.json` のパスが正しいか確認
- JSON形式が正しいか確認
- CursorのTools & Integrations > MCP Toolsで認識されているか確認
- 0 tool enable や赤丸が表示されていたらCursorを再起動

### TypeScript Server ビルドエラー
```bash
# 依存関係の再インストール
cd Packages/src/TypeScriptServer
rm -rf node_modules package-lock.json
npm install
npm run build
```

### 型エラー
- `Packages/src/TypeScriptServer/src/types/tool-types.ts` で型定義を確認
- MCPサーバーの戻り値型に合致しているか確認

## ライセンス

MIT License

## 作者

Masamichi Hatayama

## 貢献

新しいツールの追加や改善提案は歓迎します。プルリクエストを送信する前に、既存のコーディング規約に従ってください。