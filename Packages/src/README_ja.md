[English](README.md)

[![Unity](https://img.shields.io/badge/Unity-2022.3+-red.svg)](https://unity3d.com/)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE.md)
[![Ask DeepWiki](https://deepwiki.com/badge.svg)](https://deepwiki.com/hatayama/uMCP)  
![ClaudeCode](https://img.shields.io/badge/Claude_Code-555?logo=claude)
![Cursor](https://img.shields.io/badge/Cursor-111?logo=Cursor)
![GitHubCopilot](https://img.shields.io/badge/GitHub_Copilot-111?logo=githubcopilot)
![Windsurf](https://img.shields.io/badge/Windsurf-111?logo=Windsurf)
![WSL2](https://img.shields.io/badge/WSL2-28b?logo=WSL2)

<h1 align="center">
    <img width="500" alt="uMCP" src="https://github.com/user-attachments/assets/0b7c4fcf-af5f-4025-b0d3-e596897d41b7" />  
</h1>     

様々なLLMツールからUnity Editorを操作する事ができます。  

# コンセプト
AIによるコーディングを可能な限り長時間自走させる事をコンセンプトに作りました。
通常、Unityをコンパイルさせたり、Testを走らせたり、logをAIに伝える部分は人間がやる必要があります。その面倒をuMCPが解決します。

# 特徴
1. packageをinstallして、お使いのLLMツールに接続するボタンを押すだけで、すぐに使い始める事ができます。
2. 簡単に機能拡張ができます。自分専用のmcp toolをすぐに作る事が可能です。(おそらくAIに頼めばすぐに作ってくれるはずです)
3. コンテキストをなるべく消費しないようにするオプションを実装しています。 

# ツールwindow
<img width="350" alt="image" src="https://github.com/user-attachments/assets/5863b58b-7b48-48ae-9a40-c874ddc11488" />

 - サーバーの状態を管理・モニターします
 - LLMツールの接続状況を把握できます
 - LLMツールの設定ボタンを押すことで、簡単にツールとの接続が可能です

# 主要機能
#### 1. compile - コンパイルの実行
AssetDatabase.Refresh()をした後、コンパイルして結果を返却します。内蔵のLinterでは発見できないエラー・警告を見つける事ができます。  
差分コンパイルと強制全体コンパイルを選択できます。
```
→ compile実行、エラー・警告内容を解析
→ 該当ファイルを自動修正
→ 再度compileで確認
```

#### 2. get-logs - UnityのConsoleと同じ内容のLogを取得します
LogTypeや検索対象の文字列で絞り込む事ができます。また、stacktraceの有無も選択できます。
これにより、コンテキストを小さく保ちながらlogを取得できます。
```
→ get-logs (LogType: Error, SearchText: "NullReference")
→ スタックトレースから原因箇所を特定、該当コードを修正
```

#### 3. run-tests - TestRunnerの実行 (PlayMode, EditMode対応)
Unity Test Runnerを実行し、テスト結果を取得します。FilterTypeとFilterValueで条件を設定できます。
- FilterType: all（全テスト）、fullclassname（完全クラス名）など
- FilterValue: フィルタータイプに応じた値（クラス名、名前空間など）  
テスト結果をxmlで出力する事が可能です。出力pathを返すので、それをAIに読み取ってもらう事ができます。  
これもコンテキストを圧迫しないための工夫です。
```
→ run-tests (FilterType: fullclassname, FilterValue: "PlayerControllerTests")
→ 失敗したテストを確認、実装を修正してテストをパス
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
→ 各プロバイダーの機能を理解、最適な検索方法を選択
```

#### 7. get-menu-items - メニュー項目の取得
[MenuItem("xxx")]属性で定義されたメニュー項目を取得します。文字列指定でフィルター出来ます。

#### 8. execute-menu-item - メニュー項目の実行
[MenuItem("xxx")]属性で定義されたメニュー項目を実行できます。
```
→ AIにテストプログラムを生成させる
→ execute-menu-item (MenuItemPath: "Tools/xxx") で生成させたテストプログラムの実行
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
→ GameObject間の親子関係を理解。構造的な問題を発見・修正
```

> [!NOTE]
> これらのツールを組み合わせることで、AIが人間の介入なしに複雑なタスクを完了できます。
> 特にエラー修正、テスト実行などの反復的なタスクで威力を発揮します。

## セキュリティ設定

> [!WARNING]
> **デフォルトで無効化されている機能**
> 
> 任意のコードを自由に実行できてしまうため、以下の機能はデフォルトで無効化されています：
> - `execute-menu-item`: メニュー項目の実行
> - `run-tests`: テストの実行
> 
> これらの機能を使用するには、uMCPウィンドウのSecurity Settingsで該当する設定を有効にする必要があります：
> - **Allow Test Execution**: `run-tests`ツールを有効にします
> - **Allow Menu Item Execution**: `execute-menu-item`ツールを有効にします
> これらの機能を有効にする場合は、信頼できる環境でのみ使用してください。

機能詳細は[FEATURES_ja.md](./FEATURES_ja.md)を御覧ください。

## 使用方法
1. Window > uMCPを選択します。専用ウィンドウが開くので、「Start Server」ボタンを押してください。  
<img width="335" alt="image" src="https://github.com/user-attachments/assets/4cfd7f26-7739-442d-bad9-b3f6d113a0d7" />

3. 次に、LLM Tool SettingsセクションでターゲットIDEを選択します。黄色い「Configure {LLM Tool名}」ボタンを押してIDEに自動接続してください。  
<img width="335" alt="image" src="https://github.com/user-attachments/assets/25f1f4f9-e3c8-40a5-a2f3-903f9ed5f45b" />

4. IDE接続確認
  - 例えばCursorの場合、設定ページのTools & Integrationsを確認し、uMCPを見つけてください。トグルをクリックしてMCPを有効にします。赤い円が表示される場合は、Cursorを再起動してください。  
<img width="545" alt="image" src="https://github.com/user-attachments/assets/ed54d051-b78a-4bb4-bb2f-7ab23ebc1840" />


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
> ポート番号を変更することで複数のUnityインスタンスをサポートできます。uMCP起動時に自動的に使われいないportが割り当てられます。

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

3. Package Managerウィンドウを開き、My RegistriesセクションのOpenUPMを選択。uMCPが表示されます。

## カスタムツール開発
コアパッケージを変更することなく、プロジェクト独自のツールを簡単に追加できます。

**ステップ1: スキーマクラスの作成**（パラメータを定義）：
```csharp
using System.ComponentModel;

public class MyCustomSchema : BaseToolSchema
{
    [Description("パラメータの説明")]
    public string MyParameter { get; set; } = "default_value";
    
    [Description("Enumパラメータの例")]
    public MyEnum EnumParameter { get; set; } = MyEnum.Option1;
}

public enum MyEnum
{
    Option1 = 0,
    Option2 = 1,
    Option3 = 2
}
```

**ステップ2: レスポンスクラスの作成**（返却データを定義）：
```csharp
public class MyCustomResponse : BaseToolResponse
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

**ステップ3: ツールクラスの作成**：
```csharp
[McpTool(Description = "私のカスタムツールの説明")]  // ← この属性により自動登録されます
public class MyCustomTool : AbstractUnityTool<MyCustomSchema, MyCustomResponse>
{
    public override string ToolName => "my-custom-tool";
    
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

[カスタムツールのサンプル](/Assets/Editor/CustomToolSamples)も参考にして下さい。

## Cursorでmcpの実行を自動で行う
CursorはデフォルトでMCP実行時にユーザーの許可を必要とします。
これを無効にするには、Cursor Settings > Chat > MCP Tools ProtectionをOffにします。
MCPの種類・ツール事に制御できず、全てのMCPが許可不要になってしまうため、セキュリティとのトレードオフになります。そこを留意して設定してください。

## WindowsでClaude Codeを使う際、WSL2の対応
WSL2のミラーモードを有効化します。`C:/Users/[username]/.wslconfig` に、下記を記述します。
```
[wsl2]
networkingMode=mirrored
```
その後、下記コマンドを実行して設定を反映させます。
```bash
wsl --shutdown
wsl
```

## ライセンス
MIT License
