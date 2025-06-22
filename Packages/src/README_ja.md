[English](README.md)

[![Unity](https://img.shields.io/badge/Unity-2022.3+-red.svg)](https://unity3d.com/)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE.md)

# uMCP

Model Context Protocolを使用してUnity EditorをLLMツールに接続します。  
`Cursor`および`Claude Code`への自動接続機能を備えています。  
これにより、以下の機能を呼び出すことができます：  

## ✨ 機能

### 📋 共通パラメータ・レスポンス形式

すべてのUnity MCPコマンドは以下の共通要素を持ちます：

#### 共通パラメータ
- `TimeoutSeconds` (number): コマンド実行のタイムアウト時間（秒）（デフォルト: 300秒 = 5分）

#### 共通レスポンスプロパティ
すべてのコマンドには以下のタイミング情報が自動的に含まれます：
- `StartedAt` (string): コマンド実行開始時刻（ローカル時間）
- `EndedAt` (string): コマンド実行終了時刻（ローカル時間）  
- `ExecutionTimeMs` (number): コマンド実行時間（ミリ秒）

---

### 1. unity.compile
- **説明**: AssetDatabase.Refresh()を実行後、コンパイルを行います。詳細なタイミング情報付きでコンパイル結果を返します。
- **パラメータ**: 
  - `ForceRecompile` (boolean): 強制再コンパイルを実行するかどうか（デフォルト: false）
- **レスポンス**: 
  - `Success` (boolean): コンパイルが成功したかどうか
  - `ErrorCount` (number): エラーの総数
  - `WarningCount` (number): 警告の総数
  - `CompletedAt` (string): コンパイル完了時刻（ISO形式）
  - `Errors` (array): コンパイルエラーの配列（存在する場合）
    - `Message` (string): エラーメッセージ
    - `File` (string): エラーが発生したファイルパス
    - `Line` (number): エラーが発生した行番号
  - `Warnings` (array): コンパイル警告の配列（存在する場合）
    - `Message` (string): 警告メッセージ
    - `File` (string): 警告が発生したファイルパス
    - `Line` (number): 警告が発生した行番号
  - `Message` (string): 追加情報のためのオプションメッセージ

### 2. unity.getLogs
- **説明**: フィルタリングおよび検索機能付きでUnityコンソールからログ情報を取得します
- **パラメータ**: 
  - `LogType` (enum): フィルタするログタイプ - "Error", "Warning", "Log", "All"（デフォルト: "All"）
  - `MaxCount` (number): 取得するログの最大数（デフォルト: 100）
  - `SearchText` (string): ログメッセージ内で検索するテキスト（空の場合はすべて取得）（デフォルト: ""）
  - `IncludeStackTrace` (boolean): スタックトレースを表示するかどうか（デフォルト: true）
- **レスポンス**: 
  - `TotalCount` (number): 利用可能なログの総数
  - `DisplayedCount` (number): このレスポンスで表示されるログの数
  - `LogType` (string): 使用されたログタイプフィルタ
  - `MaxCount` (number): 使用された最大数制限
  - `SearchText` (string): 使用された検索テキストフィルタ
  - `IncludeStackTrace` (boolean): スタックトレースが含まれているかどうか
  - `Logs` (array): ログエントリの配列
    - `Type` (string): ログタイプ（Error, Warning, Log）
    - `Message` (string): ログメッセージ
    - `StackTrace` (string): スタックトレース（IncludeStackTraceがtrueの場合）
    - `File` (string): ログが発生したファイル名

### 3. unity.runTests
- **説明**: Unity Test Runnerを実行し、包括的なレポート付きでテスト結果を取得します
- **パラメータ**: 
  - `FilterType` (enum): テストフィルタのタイプ - "all", "fullclassname", "namespace", "testname", "assembly"（デフォルト: "all"）
  - `FilterValue` (string): フィルタ値（FilterTypeがall以外の場合に指定）（デフォルト: ""）
    - `fullclassname`: 完全クラス名（例：io.github.hatayama.uMCP.CompileCommandTests）
    - `namespace`: 名前空間（例：io.github.hatayama.uMCP）
    - `testname`: 個別テスト名
    - `assembly`: アセンブリ名
  - `SaveXml` (boolean): テスト結果をXMLファイルとして保存するかどうか（デフォルト: false）
- **レスポンス**: 
  - `Success` (boolean): テスト実行が成功したかどうか
  - `Message` (string): テスト実行メッセージ
  - `CompletedAt` (string): テスト実行完了タイムスタンプ（ISO形式）
  - `TestCount` (number): 実行されたテストの総数
  - `PassedCount` (number): 合格したテストの数
  - `FailedCount` (number): 失敗したテストの数
  - `SkippedCount` (number): スキップされたテストの数
  - `XmlPath` (string): XML結果ファイルのパス（SaveXmlがtrueの場合）
 
### 4. unity.ping
- **説明**: Unity側への接続テスト
- **パラメータ**: 
  - `Message` (string): Unity側に送信するメッセージ（デフォルト: "Hello from TypeScript MCP Server"）
- **レスポンス**: 
  - `Message` (string): Unity側からの応答メッセージ
- **注意**:
  - パフォーマンス監視のための詳細な実行タイミングを提供
  - 動的タイムアウト設定をサポート
  - 接続情報付きのフォーマットされたレスポンスを表示

### ⚡ 高度な機能

#### 型安全パラメータシステム
- すべてのコマンドは自動検証付きの強く型付けされたパラメータスキーマを使用
- Enumパラメータにより、より良いユーザー体験のための事前定義された値オプションを提供
- オプションパラメータにはデフォルト値が自動的に適用
- 包括的なパラメータ説明により適切な使用方法をガイド

#### BaseCommandResponseシステム
- **自動タイミング測定**: すべてのコマンドが実行時間を自動測定・報告
- **一貫したレスポンス形式**: すべてのレスポンスに標準化されたタイミング情報を含む
- **ローカル時間表示**: より良い可読性のためタイムスタンプをローカル時間に変換
- **パフォーマンス監視**: 実行時間によりパフォーマンスボトルネックの特定を支援

#### 動的タイムアウト設定
- **コマンド別タイムアウト**: 各コマンドは`TimeoutSeconds`パラメータによる個別タイムアウト設定をサポート
- **インテリジェントデフォルト**: コマンドの複雑さに基づく合理的なデフォルトタイムアウト（pingは5秒、テストは5分）
- **バッファ管理**: Unity側のタイムアウトが先に発動するようTypeScriptクライアントが10秒のバッファを追加
- **タイムアウト処理**: 詳細なエラー情報付きの適切なタイムアウトレスポンス

#### リアルタイムツール発見
- **イベント駆動更新**: Unityコマンドの変更が自動的に検出され、LLMツールに伝播
- **動的ツール登録**: 新しいカスタムコマンドがサーバー再起動なしでLLMツールに表示
- **ドメインリロード復旧**: Unityコンパイル後の自動再接続とツール同期

## 使用方法
1. Window > uMCPを選択します。専用ウィンドウが開くので、「Start Server」ボタンを押してください。
<img width="400" alt="image" src="https://github.com/user-attachments/assets/0a1b5ed4-56a9-4209-b2e7-0acbca3cb9a9" />

以下のように表示が変わったら成功です。

<img width="400" alt="image" src="https://github.com/user-attachments/assets/9f5d8294-2cde-4d30-ab22-f527e6c3bf66" />

2. 次に、LLM Tool SettingsセクションでターゲットIDEを選択します。「Auto Configure Settings」ボタンを押してIDEに自動接続してください。

<img width="400" alt="image" src="https://github.com/user-attachments/assets/379fe674-dee7-4962-9d93-6f43fca13227" />

3. IDE接続確認
  - 例えば、Cursorの場合、設定ページのTools & Integrationsを確認し、unity-mcp-{ポート番号}を見つけてください。トグルをクリックしてMCPを有効にします。黄色や赤い円が表示される場合は、Cursorを再起動してください。
<img width="657" alt="image" src="https://github.com/user-attachments/assets/14352ec0-c0a4-443d-98d5-35a6c86acd45" />

4. 手動設定（通常は不要）
必要に応じて、Cursorの設定ファイル（`.cursor/mcp.json`）を手動で編集できます：

```json
{
  "mcpServers": {
    "uMcp-{port}": {
      "command": "node",
      "args": [
        "[Unity Package Path]/TypeScriptServer/dist/server.bundle.js"
      ],
      "env": {
        "UNITY_TCP_PORT": "{port}"
      }
    }
  }
}
```

**パス例**:
- **Package Manager経由**: `"/Users/username/UnityProject/Library/PackageCache/io.github.hatayama.umpc@[hash]/TypeScriptServer/dist/server.bundle.js"`
> **注意**: Package Manager経由でインストールした場合、パッケージはハッシュ化されたディレクトリ名で`Library/PackageCache`に配置されます。「Auto Configure Cursor」ボタンを使用すると、正しいパスが自動的に設定されます。

5. 複数のUnityインスタンスのサポート
  - ポート番号を変更することで複数のUnityインスタンスをサポート

## 前提条件

⚠️ **重要**: 以下のソフトウェアが必要です
- **Unity 2022.3以上**
- **Node.js 18.0以上** ⭐ **必須** - MCPサーバー実行に必要
- Node.jsを[こちら](https://nodejs.org/en/download)からインストールしてください

## インストール

### Unity Package Manager経由

1. Unity Editorを開く
2. Window > Package Managerを開く
3. 「+」ボタンをクリック
4. 「Add package from git URL」を選択
5. 以下のURLを入力：
```
https://github.com/hatayama/uMCP.git?path=/Packages/src
```

### OpenUPM経由（推奨）

### Unity Package ManagerでScoped registryを使用
1. Project Settingsウィンドウを開き、Package Managerページに移動
2. Scoped Registriesリストに以下のエントリを追加：
```
Name: OpenUPM
URL: https://package.openupm.com
Scope(s): io.github.hatayama.umcp
```

3. Package Managerウィンドウを開き、My RegistriesセクションのhutayamaページからProject Settingsに移動

### Unity接続エラー
- Unity MCP Bridgeが実行されていることを確認（Window > Unity MCP）
- 設定されたポートが他のアプリケーションによって使用されていないことを確認

### Cursor設定エラー
- `.cursor/mcp.json`のパスが正しいことを確認
- JSON形式が正しいことを確認
- CursorのTools & Integrations > MCP Toolsで認識されているかを確認。「0 tool enable」や赤い円が表示される場合は、Cursorを再起動


[現在は上記の組み込み機能のみが利用可能ですが、将来的にパッケージ外でコマンドを自由に追加できる機能を検討しています](https://github.com/hatayama/uMCP/issues/14)

### 🔧 カスタムコマンド開発

uMCPシステムは、開発者がコアパッケージを変更することなく独自のコマンドを追加できる**動的カスタムコマンド登録**をサポートしています。カスタムコマンドを登録する方法は**2つ**あります：

#### 方法1: [McpTool]属性による自動登録（推奨）

これは**最も簡単で推奨される方法**です。Unityのコンパイル時にコマンドが自動的に発見・登録されます。

**ステップ1: スキーマクラスの作成**（パラメータを定義）：
```csharp
using System.ComponentModel;

public class MyCustomSchema : BaseCommandSchema
{
    [Description("パラメータの説明")]
    public string MyParameter { get; set; } = "default_value";
    
    [Description("操作タイプを選択")]
    public MyOperationType OperationType { get; set; } = MyOperationType.Process;
}

public enum MyOperationType
{
    Process,
    Validate,
    Export
}
```

**ステップ2: レスポンスクラスの作成**（戻りデータを定義）：
```csharp
public class MyCustomResponse : BaseCommandResponse
{
    public string Result { get; set; }
    public bool Success { get; set; }
    
    public MyCustomResponse(string result, bool success)
    {
        Result = result;
        Success = success;
    }
    
    // 必須のパラメータなしコンストラクタ
    public MyCustomResponse() { }
}
```

**ステップ3: コマンドクラスの作成**：
```csharp
[McpTool]  // ← この属性により自動登録が有効になります！
public class MyCustomCommand : AbstractUnityCommand<MyCustomSchema, MyCustomResponse>
{
    public override string CommandName => "myCustomCommand";
    public override string Description => "私のカスタムコマンドの説明";
    
    // メインスレッドで実行されます
    protected override Task<MyCustomResponse> ExecuteAsync(MyCustomSchema parameters)
    {
        // 型安全なパラメータアクセス
        string param = parameters.MyParameter;
        MyOperationType operation = parameters.OperationType;
        
        // カスタムロジックをここに実装
        string result = ProcessCustomLogic(param, operation);
        bool success = !string.IsNullOrEmpty(result);
        
        return Task.FromResult(new MyCustomResponse(result, success));
    }
    
    private string ProcessCustomLogic(string input, MyOperationType operation)
    {
        // カスタムロジックを実装
        return $"Processed '{input}' with operation '{operation}'";
    }
}
```

---

#### 方法2: CustomCommandManagerによる手動登録

この方法では、コマンドがいつ登録/登録解除されるかを**完全に制御**できます。

**ステップ1-2: スキーマとレスポンスクラスの作成**（方法1と同じですが、`[McpTool]`属性は**なし**）

**ステップ3: コマンドクラスの作成**（`[McpTool]`属性なし）：
```csharp
// 手動登録のため[McpTool]属性なし
public class MyManualCommand : AbstractUnityCommand<MyCustomSchema, MyCustomResponse>
{
    public override string CommandName => "myManualCommand";
    public override string Description => "手動登録されたカスタムコマンド";
    
    protected override Task<MyCustomResponse> ExecuteAsync(MyCustomSchema parameters)
    {
        // 方法1と同じ実装
        string result = ProcessCustomLogic(parameters.MyParameter, parameters.OperationType);
        return Task.FromResult(new MyCustomResponse(result, true));
    }
}
```

**ステップ4: 手動登録**：
```csharp
using UnityEngine;
using UnityEditor;

public static class MyCommandRegistration
{
    // Unityメニュー経由でコマンドを登録
    [MenuItem("MyProject/Register Custom Commands")]
    public static void RegisterMyCommands()
    {
        CustomCommandManager.RegisterCustomCommand(new MyManualCommand());
        Debug.Log("カスタムコマンドが登録されました！");
        
        // オプション: LLMツールに変更を手動で通知
        CustomCommandManager.NotifyCommandChanges();
    }
    
    // Unityメニュー経由でコマンドを登録解除  
    [MenuItem("MyProject/Unregister Custom Commands")]
    public static void UnregisterMyCommands()
    {
        CustomCommandManager.UnregisterCustomCommand("myManualCommand");
        Debug.Log("カスタムコマンドが登録解除されました！");
    }
    
    // 代替案: Unity起動時の自動登録
    // [InitializeOnLoad]
    // static MyCommandRegistration()
    // {
    //     RegisterMyCommands();
    // }
}
```

#### 🔧 カスタムコマンドのデバッグ

```csharp
// 登録されたすべてのコマンドを表示
[MenuItem("uMCP/Debug/Show Registered Commands")]
public static void ShowCommands()
{
    CommandInfo[] commands = CustomCommandManager.GetRegisteredCustomCommands();
    foreach (var cmd in commands)
    {
        Debug.Log($"Command: {cmd.Name} - {cmd.Description}");
    }
}
```

## ライセンス
MIT License