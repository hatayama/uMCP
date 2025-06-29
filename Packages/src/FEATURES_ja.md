[English](FEATURES.md)

# uMCP 機能仕様

このドキュメントでは、Unity MCP（Model Context Protocol）の全コマンドと機能について詳細に説明します。

## 📋 共通パラメータ・レスポンス形式

すべてのUnity MCPコマンドは以下の共通要素を持ちます：

### 共通パラメータ
- `TimeoutSeconds` (number): コマンド実行のタイムアウト時間（秒）（デフォルト: 10秒）

### 共通レスポンスプロパティ
すべてのコマンドには以下のタイミング情報が自動的に含まれます：
- `StartedAt` (string): コマンド実行開始時刻（ローカル時間）
- `EndedAt` (string): コマンド実行終了時刻（ローカル時間）  
- `ExecutionTimeMs` (number): コマンド実行時間（ミリ秒）

---

## 🛠️ Unity コアコマンド

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
  - `FilterType` (enum): テストフィルタのタイプ - "all", "fullclassname"（デフォルト: "all"）
  - `FilterValue` (string): フィルタ値（FilterTypeがall以外の場合に指定）（デフォルト: ""）
    - `fullclassname`: 完全クラス名（例：io.github.hatayama.uMCP.CompileCommandTests）
  - `TestMode` (enum): テストモード - "EditMode", "PlayMode"（デフォルト: "EditMode"）
  - `SaveXml` (boolean): テスト結果をXMLファイルとして保存するかどうか（デフォルト: false）
    - XMLファイルは `TestResults/` フォルダに保存されます（プロジェクトルート）
    - **推奨**: `.gitignore` に `TestResults/` を追加してバージョン管理から除外してください
- **レスポンス**: 
  - `Success` (boolean): テスト実行が成功したかどうか
  - `Message` (string): テスト実行メッセージ
  - `CompletedAt` (string): テスト実行完了タイムスタンプ（ISO形式）
  - `TestCount` (number): 実行されたテストの総数
  - `PassedCount` (number): 合格したテストの数
  - `FailedCount` (number): 失敗したテストの数
  - `SkippedCount` (number): スキップされたテストの数
  - `XmlPath` (string): XML結果ファイルのパス（SaveXmlがtrueの場合）

### 4. unity.clearConsole
- **説明**: クリーンな開発ワークフローのためにUnityコンソールログをクリアします
- **パラメータ**: 
  - `AddConfirmationMessage` (boolean): クリア後に確認ログメッセージを追加するかどうか（デフォルト: true）
- **レスポンス**: 
  - `Success` (boolean): コンソールクリア操作が成功したかどうか
  - `ClearedLogCount` (number): コンソールからクリアされたログの数
  - `ClearedCounts` (object): タイプ別のクリアされたログの内訳
    - `ErrorCount` (number): クリアされたエラーログの数
    - `WarningCount` (number): クリアされた警告ログの数
    - `LogCount` (number): クリアされた情報ログの数
  - `Message` (string): クリア操作結果を説明するメッセージ
  - `ErrorMessage` (string): 操作が失敗した場合のエラーメッセージ

---

## 🔍 Unity 検索・発見コマンド

### 5. unity.unitySearch
- **説明**: Unity Search APIを使用してUnityプロジェクトを検索し、包括的なフィルタリングとエクスポートオプションを提供します
- **パラメータ**: 
  - `SearchQuery` (string): 検索クエリ文字列（Unity Search構文をサポート）（デフォルト: ""）
    - 例: "*.cs", "t:Texture2D", "ref:MyScript", "p:MyPackage"
  - `Providers` (array): 使用する特定の検索プロバイダー（空 = すべてのアクティブプロバイダー）（デフォルト: []）
    - 一般的なプロバイダー: "asset", "scene", "menu", "settings", "packages"
  - `MaxResults` (number): 返す検索結果の最大数（デフォルト: 50）
  - `IncludeDescription` (boolean): 結果に詳細な説明を含めるかどうか（デフォルト: true）
  - `IncludeThumbnails` (boolean): サムネイル/プレビュー情報を含めるかどうか（デフォルト: false）
  - `IncludeMetadata` (boolean): ファイルメタデータ（サイズ、更新日）を含めるかどうか（デフォルト: false）
  - `SearchFlags` (enum): Unity Search動作を制御する検索フラグ（デフォルト: "Default"）
  - `SaveToFile` (boolean): 検索結果を外部ファイルに保存するかどうか（デフォルト: false）
  - `OutputFormat` (enum): SaveToFileが有効な場合の出力ファイル形式 - "JSON", "CSV", "TSV"（デフォルト: "JSON"）
  - `AutoSaveThreshold` (number): 自動ファイル保存の閾値（デフォルト: 100）
  - `FileExtensions` (array): ファイル拡張子で結果をフィルタ（例: "cs", "prefab", "mat"）（デフォルト: []）
  - `AssetTypes` (array): アセットタイプで結果をフィルタ（例: "Texture2D", "GameObject", "MonoScript"）（デフォルト: []）
  - `PathFilter` (string): パスパターンで結果をフィルタ（ワイルドカードサポート）（デフォルト: ""）
- **レスポンス**: 
  - `Results` (array): 検索結果アイテムの配列（結果がファイルに保存された場合は空）
  - `TotalCount` (number): 見つかった検索結果の総数
  - `DisplayedCount` (number): このレスポンスで表示される結果の数
  - `SearchQuery` (string): 実行された検索クエリ
  - `ProvidersUsed` (array): 検索に使用された検索プロバイダー
  - `SearchDurationMs` (number): 検索時間（ミリ秒）
  - `Success` (boolean): 検索が正常に完了したかどうか
  - `ErrorMessage` (string): 検索が失敗した場合のエラーメッセージ
  - `ResultsFilePath` (string): 保存された検索結果ファイルのパス（SaveToFileが有効な場合）
  - `ResultsSavedToFile` (boolean): 結果がファイルに保存されたかどうか
  - `SavedFileFormat` (string): 保存された結果のファイル形式
  - `SaveToFileReason` (string): 結果がファイルに保存された理由

### 6. unity.getProviderDetails
- **説明**: 表示名、説明、アクティブ状態、機能を含むUnity Searchプロバイダーの詳細情報を取得します
- **パラメータ**: 
  - `ProviderId` (string): 詳細を取得する特定のプロバイダーID（空 = すべてのプロバイダー）（デフォルト: ""）
    - 例: "asset", "scene", "menu", "settings"
  - `ActiveOnly` (boolean): アクティブなプロバイダーのみを含めるかどうか（デフォルト: false）
  - `SortByPriority` (boolean): 優先度でプロバイダーをソート（数値が小さい = 優先度が高い）（デフォルト: true）
  - `IncludeDescriptions` (boolean): 各プロバイダーの詳細な説明を含める（デフォルト: true）
- **レスポンス**: 
  - `Providers` (array): プロバイダー情報の配列
  - `TotalCount` (number): 見つかったプロバイダーの総数
  - `ActiveCount` (number): アクティブなプロバイダーの数
  - `InactiveCount` (number): 非アクティブなプロバイダーの数
  - `Success` (boolean): リクエストが成功したかどうか
  - `ErrorMessage` (string): リクエストが失敗した場合のエラーメッセージ
  - `AppliedFilter` (string): 適用されたフィルタ（特定のプロバイダーIDまたは"all"）
  - `SortedByPriority` (boolean): 結果が優先度でソートされているかどうか

### 7. unity.getMenuItems
- **説明**: プログラム実行のための詳細なメタデータ付きでUnity MenuItemsを取得します。Unity Searchのメニュープロバイダーとは異なり、自動化とデバッグに必要な実装詳細（メソッド名、アセンブリ、実行互換性）を提供します
- **パラメータ**: 
  - `FilterText` (string): MenuItemパスをフィルタするテキスト（すべてのアイテムの場合は空）（デフォルト: ""）
  - `FilterType` (enum): 適用するフィルタのタイプ - "contains", "exact", "startswith"（デフォルト: "contains"）
  - `IncludeValidation` (boolean): 結果に検証関数を含める（デフォルト: false）
  - `MaxCount` (number): 取得するメニューアイテムの最大数（デフォルト: 200）
- **レスポンス**: 
  - `MenuItems` (array): フィルタ条件に一致する発見されたMenuItemsのリスト
  - `TotalCount` (number): フィルタリング前に発見されたMenuItemsの総数
  - `FilteredCount` (number): フィルタリング後に返されたMenuItemsの数
  - `AppliedFilter` (string): 適用されたフィルタテキスト
  - `AppliedFilterType` (string): 適用されたフィルタタイプ

### 8. unity.executeMenuItem
- **説明**: パスによってUnity MenuItemを実行します
- **パラメータ**: 
  - `MenuItemPath` (string): 実行するメニューアイテムパス（例: "GameObject/Create Empty"）（デフォルト: ""）
  - `UseReflectionFallback` (boolean): EditorApplication.ExecuteMenuItemが失敗した場合にリフレクションをフォールバックとして使用するかどうか（デフォルト: true）
- **レスポンス**: 
  - `MenuItemPath` (string): 実行されたメニューアイテムパス
  - `Success` (boolean): 実行が成功したかどうか
  - `ExecutionMethod` (string): 使用された実行方法（EditorApplicationまたはReflection）
  - `ErrorMessage` (string): 実行が失敗した場合のエラーメッセージ
  - `Details` (string): 実行に関する追加情報
  - `MenuItemFound` (boolean): メニューアイテムがシステムで見つかったかどうか

---

## 🔧 開発・デバッグコマンド

### 9. unity.ping（開発専用）
- **説明**: Unity側への接続テスト
- **パラメータ**: 
  - `Message` (string): Unity側に送信するメッセージ（デフォルト: "Hello from TypeScript MCP Server"）
- **レスポンス**: 
  - `Message` (string): Unity側からの応答メッセージ
- **注意**:
  - パフォーマンス監視のための詳細な実行タイミングを提供
  - 動的タイムアウト設定をサポート
  - 接続情報付きのフォーマットされたレスポンスを表示
  - 開発モードでのみ表示

### 10. unity.getCommandDetails（開発専用）
- **説明**: 登録されたすべてのUnity MCPコマンドの詳細情報を取得します
- **パラメータ**: 
  - `IncludeDevelopmentOnly` (boolean): 結果に開発専用コマンドを含めるかどうか（デフォルト: false）
- **レスポンス**: 
  - `Commands` (array): 詳細なコマンド情報の配列
- **注意**:
  - 開発モードでのみ表示
  - デバッグと開発に有用

### 11. unity.setClientName（開発専用）
- **説明**: Unity MCPサーバーでの識別のためにクライアント名を登録します
- **パラメータ**: 
  - `ClientName` (string): MCPクライアントツールの名前（デフォルト: "Unknown Client"）
- **レスポンス**: 
  - `Message` (string): 成功ステータスメッセージ
  - `ClientName` (string): 登録されたクライアント名
- **注意**:
  - 開発モードでのみ表示
  - TypeScriptクライアントが自身を識別するために内部的に使用

---

## ⚡ 高度な機能

### 型安全パラメータシステム
- すべてのコマンドは自動検証付きの強く型付けされたパラメータスキーマを使用
- Enumパラメータにより、より良いユーザー体験のための事前定義された値オプションを提供
- オプションパラメータにはデフォルト値が自動的に適用
- 包括的なパラメータ説明により適切な使用方法をガイド

### BaseCommandResponseシステム
- **自動タイミング測定**: すべてのコマンドが実行時間を自動測定・報告
- **一貫したレスポンス形式**: すべてのレスポンスに標準化されたタイミング情報を含む
- **ローカル時間表示**: より良い可読性のためタイムスタンプをローカル時間に変換
- **パフォーマンス監視**: 実行時間によりパフォーマンスボトルネックの特定を支援

### 動的タイムアウト設定
- **コマンド別タイムアウト**: 各コマンドは`TimeoutSeconds`パラメータによる個別タイムアウト設定をサポート
- **インテリジェントデフォルト**: コマンドの複雑さに基づく合理的なデフォルトタイムアウト（ping: 5秒、テスト: 30秒）
- **バッファ管理**: Unity側のタイムアウトが先に発動するようTypeScriptクライアントが10秒のバッファを追加
- **タイムアウト処理**: 詳細なエラー情報付きの適切なタイムアウトレスポンス

### リアルタイムツール発見
- **イベント駆動更新**: Unityコマンドの変更が自動的に検出され、LLMツールに伝播
- **動的ツール登録**: 新しいカスタムコマンドがサーバー再起動なしでLLMツールに表示
- **ドメインリロード復旧**: Unityコンパイル後の自動再接続とツール同期

### ファイルエクスポートシステム
- **大規模結果管理**: トークン消費を避けるため大きな検索結果の自動ファイルエクスポート
- **複数フォーマット**: JSON、CSV、TSVエクスポート形式をサポート
- **自動クリーンアップ**: ディスク容量問題を防ぐため古いエクスポートファイルを自動クリーンアップ
- **閾値ベースエクスポート**: 自動ファイル保存のための設定可能な閾値

---

## 🔧 カスタムコマンド開発

uMCPシステムは、開発者がコアパッケージを変更することなく独自のコマンドを追加できる**動的カスタムコマンド登録**をサポートしています。カスタムコマンドを登録する方法は**2つ**あります：

### 方法1: [McpTool]属性による自動登録（推奨）

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

### 方法2: CustomCommandManagerによる手動登録

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
}
```

### カスタムコマンドのデバッグ

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

---

## 📚 関連ドキュメント

- [メインREADME](README_ja.md) - プロジェクト概要とセットアップ
- [アーキテクチャドキュメント](Editor/ARCHITECTURE.md) - 技術アーキテクチャの詳細
- [TypeScriptサーバーアーキテクチャ](.TypeScriptServer/ARCHITECTURE.md) - TypeScriptサーバー実装
- [変更履歴](CHANGELOG.md) - バージョン履歴と更新 