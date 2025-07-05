[English](README.md)

[![Unity](https://img.shields.io/badge/Unity-2022.3+-red.svg)](https://unity3d.com/)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE.md)
[![Ask DeepWiki](https://deepwiki.com/badge.svg)](https://deepwiki.com/hatayama/uMCP)  
![ClaudeCode](https://img.shields.io/badge/Claude_Code-555?logo=claude)
![Cursor](https://img.shields.io/badge/Cursor-111?logo=Cursor)
![GitHubCopilot](https://img.shields.io/badge/GitHub_Copilot-111?logo=githubcopilot)
![Windsurf](https://img.shields.io/badge/Windsurf-111?logo=Windsurf)
![WSL2](https://img.shields.io/badge/WSL2-28b?logo=WSL2)

# uMCP

様々なLLMツールからUnity Editorを操作する事ができます。  

# コンセプト

AIによるコーディング時、Unityをコンパイルさせたり、logを取得する部分は人間がやる必要があります。それを可能な限り少なくするというコンセプトで作られました。
uMCPを使えば、AIが人間の操作に頼らず、可能な限り長時間自走してくれるでしょう。

### 主要機能
#### 1. compile - コンパイルの実行
AssetDatabase.Refresh()をした後、コンパイルします。内蔵のLinterでは発見できないエラー・警告を見つける事ができます。  
差分コンパイルと強制全体コンパイルを選択できます。
```
→ compile実行
→ エラー内容を解析
→ 該当ファイルを自動修正
→ 再度compileで確認
```

#### 2. get-logs - UnityのConsoleと同じ内容のLogを取得します
LogTypeや検索対象の文字列で絞り込む事ができます。また、stacktraceの有無も選択できます。
これにより、コンテキストを小さく保ちながらlogを取得できます。
```
→ get-logs (LogType: Error, SearchText: "NullReference")
→ スタックトレースから原因箇所を特定
→ 該当コードを修正
```

#### 3. run-tests - TestRunnerの実行 (PlayMode, EditMode対応)
Unity Test Runnerを実行し、テスト結果を取得します。FilterTypeとFilterValueで条件を設定できます。
- FilterType: all（全テスト）、fullclassname（完全クラス名）など
- FilterValue: フィルタータイプに応じた値（クラス名、名前空間など）  
テスト結果をxmlで出力する事が可能です。出力pathを返すので、それをAIに読み取ってもらう事ができます。  
これもコンテキストを圧迫しないための工夫です。
```
→ run-tests (FilterType: fullclassname, FilterValue: "PlayerControllerTests")
→ 失敗したテストを確認
→ 実装を修正してテストをパス
```
> [!WARNING]
> PlayModeテスト実行の際、Domain Reloadは強制的にOFFにされます。Static変数がリセットされない事に注意して下さい。

#### 4. clear-console - ログのクリーンアップ
log検索時、ノイズのとなるlogをクリアする事ができます。
```
→ clear-console
→ 新しいデバッグセッションを開始
```

#### 5. unity-search - UnitySearchによるプロジェクト検索
[UnitySearch](https://docs.unity3d.com/ja/2022.3/Manual/search-overview.html)を使うことができます。
```
→ unity-search (SearchQuery: "*.prefab")
→ 特定の条件に合うPrefabをリストアップ
→ 問題のあるPrefabを特定する
```

#### 6. get-provider-details - UnitySearch検索プロバイダーの確認
UnitySearchが提供する検索プロバイダーを取得します
```
→ get-provider-details
→ 各プロバイダーの機能を理解
→ 最適な検索方法を選択
```

#### 7. get-menu-items - メニュー項目の取得
[MenuItem("xxx")]属性で定義されたメニュー項目を取得します。文字列指定でフィルター出来ます。
```
→ 利用可能なメニューアイテムを確認
```

#### 8. execute-menu-item - メニュー項目の実行
[MenuItem("xxx")]属性で定義されたメニュー項目を実行できます。
```
→ AIにテストプログラムを生成させる
→ execute-menu-item (MenuItemPath: "Tools/xxx")
→ 生成させたテストプログラムの実行
→ get-logsで結果を確認
```

#### 9. find-game-objects - シーン内オブジェクト検索
オブジェクトを取得し、コンポーネントのパラメータを調べます
```
→ find-game-objects (RequiredComponents: ["Camera"])
→ Cameraコンポーネントのパラメータを調査
```

#### 10. get-hierarchy - シーン構造の解析
現在アクティブなHierarchyの情報を取得します。ランタイムでも動作します。
```
→ get-hierarchy
→ GameObject間の親子関係を理解
→ 構造的な問題を発見・修正
```

> [!NOTE]
> これらのコマンドを組み合わせることで、AIが人間の介入なしに複雑なタスクを完了できます。
> 特にエラー修正、テスト実行などの反復的なタスクで威力を発揮します。
 
機能詳細は[FEATURES.md](FEATURES_ja.md)を御覧ください。

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
> [!NOTE]
> 通常は自動設定で十分ですが、必要に応じて、Cursorの設定ファイル（`.cursor/mcp.json`）を手動で編集できます：

```json
{
  "mcpServers": {
    "uMcp": {
      "command": "node",
      "args": [
        "[Unity Package Path]/TypeScriptServer~/dist/server.bundle.js"
      ],
      "env": {
        "UNITY_TCP_PORT": "{port}"
      }
    }
  }
}
```

**パス例**:
- **Package Manager経由**: `"/Users/username/UnityProject/Library/PackageCache/io.github.hatayama.umcp@[hash]/TypeScriptServer~/dist/server.bundle.js"`
> [!NOTE]
> Package Manager経由でインストールした場合、パッケージはハッシュ化されたディレクトリ名で`Library/PackageCache`に配置されます。「Auto Configure Cursor」ボタンを使用すると、正しいパスが自動的に設定されます。

5. 複数のUnityインスタンスのサポート
> [!NOTE]
> ポート番号を変更することで複数のUnityインスタンスをサポートできます。各インスタンスに異なるポート番号を割り当ててください。

## 前提条件

> [!WARNING]
> 以下のソフトウェアが必須です：
> - **Unity 2022.3以上**
> - **Node.js 18.0以上** - MCPサーバー実行に必要
> - Node.jsを[こちら](https://nodejs.org/en/download)からインストールしてください

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
> [!CAUTION]
> 接続エラーが発生した場合：
> - Unity MCP Bridgeが実行されていることを確認（Window > Unity MCP）
> - 設定されたポートが他のアプリケーションによって使用されていないことを確認

### Cursor設定エラー
> [!WARNING]
> 以下の点を確認してください：
> - `.cursor/mcp.json`のパスが正しいことを確認
> - JSON形式が正しいことを確認
> - CursorのTools & Integrations > MCP Toolsで認識されているかを確認。「0 tool enable」や赤い円が表示される場合は、Cursorを再起動


## カスタムコマンド開発
コアパッケージを変更することなく、プロジェクト独自のコマンドを簡単に追加できます。

**ステップ1: スキーマクラスの作成**（パラメータを定義）：
```csharp
using System.ComponentModel;

public class MyCustomSchema : BaseCommandSchema
{
    [Description("パラメータの説明")]
    public string MyParameter { get; set; } = "default_value";
    
    [Description("Enumパラメータの例")]
    public MyEnum EnumParameter { get; set; } = MyEnum.Option1;
}

public enum MyEnum
{
    Option1,
    Option2,
    Option3
}
```

**ステップ2: レスポンスクラスの作成**（返却データを定義）：
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
[McpTool]  // ← この属性により自動登録されます
public class MyCustomCommand : AbstractUnityCommand<MyCustomSchema, MyCustomResponse>
{
    public override string CommandName => "myCustomCommand";
    public override string Description => "私のカスタムコマンドの説明";
    
    // メインスレッドで実行されます
    protected override Task<MyCustomResponse> ExecuteAsync(MyCustomSchema parameters)
    {
        // 型安全なパラメータアクセス
        string param = parameters.MyParameter;
        MyEnum enumValue = parameters.EnumParameter;
        
        // カスタムロジックをここに実装
        string result = ProcessCustomLogic(param, enumValue);
        bool success = !string.IsNullOrEmpty(result);
        
        return Task.FromResult(new MyCustomResponse(result, success));
    }
    
    private string ProcessCustomLogic(string input, MyEnum enumValue)
    {
        // カスタムロジックを実装
        return $"Processed '{input}' with enum '{enumValue}'";
    }
}
```

[カスタムコマンドのサンプル](/Assets/Editor/CustomCommandSamples)も参考になります。

### 開発方法
- **方法1**: [McpTool]属性による自動登録（推奨）
- **方法2**: CustomCommandManagerによる手動登録
- **デバッグ**: 登録済みコマンドの確認機能

## ライセンス
MIT License