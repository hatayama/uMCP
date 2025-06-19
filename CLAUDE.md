# CLAUDE.md

このファイルは、このリポジトリでコードを扱う際のClaude Code (claude.ai/code) への指針を提供します。

## プロジェクトについて

uMCPは、Model Context Protocol (MCP)を使用してUnity EditorをAIアシスタントに接続するUnityパッケージです。CursorなどのAIツールがブリッジアーキテクチャを通じて、コンパイル、ログ取得、テスト実行などのUnity操作を実行できるようにします。

## 開発コマンド

### Unity パッケージ開発
- **Unity バージョン**: 2020.3以上が必要
- **テスト**: ルートレベルでUnityプロジェクトを開き、`Packages/src`からPackage Manager経由でパッケージをインストール
- **パッケージテスト**: Window > uMCP でメインインターフェースを開く

### TypeScript MCP サーバー開発
```bash
# TypeScriptサーバーディレクトリに移動
cd Packages/src/TypeScriptServer

# 初期セットアップ（初回のみ、またはpackage.json変更後）
npm install

# バンドルサーバーをビルド
npm run build

# ファイル変更時の自動リビルド付き開発
npm run dev:watch

# 開発サーバーを実行（pingツール有効）
npm run dev

# 本番サーバー（pingツール無効）
npm start
```

### テストコマンド
```bash
# Unity-TypeScript通信テスト
node test/test-compile.js                    # 基本コンパイルテスト
node test/test-compile.js --force           # 強制再コンパイル
node test/test-logs.js                      # ログ取得テスト
node test/test-unity-connection.js          # 完全接続テスト
node test/test-all-logs.js --stats         # ログ統計

# Unity MCPサーバーがポート7400で動作している時の直接Unity通信
echo '{"jsonrpc":"2.0","id":1,"method":"ping","params":{"message":"test"}}' | nc localhost 7400
```

## アーキテクチャ概要

### 二層ブリッジアーキテクチャ
システムは、AIアシスタントをUnityに接続するためのデュアルブリッジアプローチを使用します：

1. **TypeScript MCP サーバー** (`Packages/src/TypeScriptServer/`)
   - AIアシスタント通信用のMCPプロトコルを実装
   - 提供ツール: `unity.ping`, `action.compileUnity`, `context.getUnityLogs`
   - TCP/IP JSON-RPC経由でUnityと通信

2. **Unity MCP ブリッジ** (`Packages/src/Editor/`)
   - TypeScriptサーバーからのコマンドを受信するUnity Editorパッケージ
   - Unity操作を実行して結果を返す
   - ScriptableSingletonクラスを通じて永続的な状態を管理

### ScriptableSingleton ベースの状態管理

**重要な設計決定**: コードベースは、Unityのドメインリロードを透過的に処理するため、すべての永続データをSessionStateからScriptableSingletonに移行しました。このアーキテクチャ変更により、コンパイルサイクル全体での状態管理の複雑さが排除されます。

**ScriptableSingleton クラス:**
- `McpServerData` - サーバー状態と設定
- `McpCompileData` - コンパイル要求の追跡  
- `McpCommunicationLogData` - 通信ログと保留中の要求
- `McpEditorWindowData` - UI状態と設定

### コマンドパターン実装

Unityコマンドは、`Packages/src/Editor/Api/Commands/`にあるレジストリベースのコマンドパターンを使用します：

- `IUnityCommand` - すべてのコマンドのベースインターフェース
- `UnityCommandRegistry` - 動的コマンド登録と実行
- 個別のコマンドクラス: `CompileCommand`, `GetLogsCommand`, `PingCommand`など
- すべてのコマンドが非同期操作用の`CancellationToken`をサポート

### 重要な技術的制約

**ドメインリロード動作**: Unityのドメインリロードは静的変数を破棄し、TCP接続を中断します。ScriptableSingletonアーキテクチャはサーバー状態の復元を自動的に処理しますが、長時間実行される操作（コンパイルなど）はクライアント側で接続タイムアウトが発生する可能性があります。

**TCP通信ライフサイクル**: Unityコンパイル中、ドメインリロードにより既存のTCP接続が終了されます。サーバーはScriptableSingletonの状態に基づいて自動的に再起動しますが、クライアントは適切に接続タイムアウトを処理する必要があります。

## コード構成

### Unity Editor 構造
```
Packages/src/Editor/
├── Api/            # JSON-RPC処理とコマンドハンドラー
├── Server/         # TCPサーバーとコントローラーロジック  
├── Data/           # ScriptableSingleton永続化クラス
├── Config/         # IDE設定管理 (Cursor, Claude Code)
├── UI/             # エディターウィンドウと通信ログ
├── Tools/          # 開発ユーティリティとログ取得
├── Build/          # TypeScriptサーバービルドユーティリティ
└── Utils/          # ログとヘルパーユーティリティ
```

### TypeScript サーバー構造
```
Packages/src/TypeScriptServer/
├── src/
│   ├── tools/      # MCPツール実装
│   ├── types/      # TypeScript型定義
│   ├── server.ts   # メインMCPサーバー
│   └── unity-client.ts # Unity通信クライアント
├── test/           # Unity通信用テストスクリプト
└── dist/           # ビルド成果物 (server.bundle.js)
```

## 主要な実装詳細

### バンドル配布
TypeScriptサーバーは、すべての依存関係を含む単一の`server.bundle.js`ファイルを作成するためにesbuildを使用します。これにより、パッケージ使用時にユーザーが`npm install`を実行する必要がなくなります。

### IDE統合
パッケージは、サポートされているIDE用にMCP設定を自動的に構成します：
- **Cursor**: `.cursor/mcp.json`を作成/更新
- **Claude Code**: Claude Code設定のサポートを計画中
- 設定は、ローカル開発とPackage Managerインストールパスの両方を処理

### 開発モード vs 本番モード
- **開発モード**: `npm run dev`でテスト用pingツールを有効化
- **本番モード**: `npm start`でエンドユーザー向けの最小ツールセットを実行
- 環境変数`ENABLE_PING_TOOL=true`で本番動作をオーバーライド可能

## ドメイン固有の考慮事項

### Unity Package Manager 互換性
コードベースは、`UnityMcpPathResolver`での動的パス解決を通じて、ローカル開発（Packages/src）とPackage Managerインストール（Library/PackageCache/io.github.hatayama.umcp@hash）の両方を処理します。

### Unity での Async/Await
すべてのUnityコマンド操作は、適切な`CancellationToken`サポート付きでasync/awaitを使用します。`MainThreadSwitcher`ユーティリティは、必要に応じてUnity APIコールがメインスレッドで実行されることを保証します。

### コンパイル統合
コンパイルシステムは、UnityのCompilationPipelineと統合し、非同期コンパイル追跡に`CompileChecker`を使用します。コンパイル中のドメインリロードは、ScriptableSingleton永続化層を通じて適切に処理されます。

## テスト戦略

### 自動テスト
- Unity: `Assets/Tests/Editor/`でのEdit Modeテスト
- TypeScript: Unity通信検証用の`test/`ディレクトリのテストスクリプト

### 統合テスト
テストスクリプトは、ドメインリロードシナリオを含む完全な通信パイプラインを検証し、TypeScriptとUnity層間の包括的な統合テストを提供します。

## 既知の問題と制限

1. **コンパイルタイムアウト**: ドメインリロードによるTCP接続の切断により、Unity側でコンパイルが正常に完了していても、クライアント側で長時間実行されるコンパイル操作がタイムアウトする場合があります。

2. **ThreadAbortException**: Unityドメインリロード中に時折スレッド中止例外が発生しますが、これらは適切に処理され、機能には影響しません。

3. **バックグラウンド操作の遅延**: Unityがバックグラウンドにあるとき、`EditorApplication.delayCall`の実行が遅くなり、自動再起動動作に影響します。

## compileコマンドレスポンス問題の解決過程

### 問題の発見
**症状**: compileコマンドを実行してもレスポンスが返らず、TypeScriptクライアント側でタイムアウトが発生する。

**原因分析**: 
- `AssetDatabase.Refresh()`がドメインリロードを引き起こす
- ドメインリロード時にTCP接続が強制切断される
- レスポンスを返す前に接続が失われる

### 解決策の検討過程

#### 1. 一段階通信の限界
従来のアプローチ：
```
クライアント → compile要求 → Unity処理 → レスポンス
```
問題：ドメインリロードで接続が切断され、レスポンスが届かない

#### 2. 二段階通信アプローチの検討
新しいアプローチ：
```
ステップ1: compile要求 → 即座に"accepted"レスポンス
ステップ2: getCompileResult要求 → 結果取得（ポーリング）
```

**利点**:
- ドメインリロード前に応答を返せる
- ScriptableSingletonで状態を永続化
- 確実な通信が可能

#### 3. Long Polling の検討と断念
**検討したアプローチ**: Ajax/XHRのような長時間接続で待機

**技術的制約により断念**:
- Unityのドメインリロードは**AppDomain全体の破棄・再作成**
- 別スレッドで動作するTCPサーバーも強制終了される
- 実行環境そのものが破棄されるため、接続の維持は不可能

#### 4. Unity Domain Reload の技術的仕組み
```
Domain Reload の流れ:
1. 現在のAppDomain破棄
2. すべてのマネージドスレッド強制終了
3. TCP接続、ファイルハンドル等も強制クローズ
4. 新しいAppDomain作成
5. アセンブリ再ロード
```

**結論**: プロセス再起動に近い重い処理のため、別スレッドでも接続切断は避けられない

### 最終的な解決方針

#### 採用した方式: 二段階通信 + ポーリング
1. **即座の応答**: compileコマンドで"accepted"ステータスを即座に返却
2. **状態永続化**: ScriptableSingletonでリクエスト状態を保存
3. **ポーリング取得**: 1秒間隔でgetCompileResultを呼び出し
4. **結果取得**: ドメインリロード完了後に実際の結果を返却

#### 実装詳細
```javascript
// TypeScript側
const result = await sendCompileRequest(forceRecompile);
// → 即座に { status: "accepted", requestId: "..." }

// 1秒間隔でポーリング
const finalResult = await pollCompileResult(requestId);
// → { status: "completed", result: {...} }
```

#### 妥協点と利点
**妥協点**:
- 完全にリアルタイムではない（1-3秒の遅延）
- 若干複雑なクライアント実装

**利点**:
- 確実な通信（タイムアウトなし）
- 従来の10-30秒タイムアウトから3-4秒に大幅改善
- アーキテクチャ変更が最小限
- 実用的なレスポンス時間

### 学んだ技術的知見

1. **Unity ドメインリロードの影響範囲**
   - 別スレッドも含めすべてのマネージドコードが終了
   - ネイティブレイヤー以外は生存不可能
   - TCP接続の切断は避けられない

2. **AssetDatabase.Refresh()の必要性**
   - 新規C#ファイルの認識に必須
   - ドメインリロードの引き金となる
   - この処理を回避する方法は存在しない

3. **実用的な解決策の重要性**
   - 完璧な技術的解決策が常に存在するわけではない
   - 制約内での最適化と妥協点の見極めが重要
   - ユーザー体験の改善が最優先

この経験により、Unityのアーキテクチャ制約を深く理解し、現実的な解決策を見つけることの重要性を学びました。