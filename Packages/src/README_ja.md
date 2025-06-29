[English](README.md)

[![Unity](https://img.shields.io/badge/Unity-2022.3+-red.svg)](https://unity3d.com/)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE.md)
[![Ask DeepWiki](https://deepwiki.com/badge.svg)](https://deepwiki.com/hatayama/uMCP)

# uMCP

Model Context Protocolを使用してUnity EditorをLLMツールに接続します。  

# コンセプト

AIによるコーディング時、Unityをコンパイルさせたり、logを取得する部分は人間がやる必要があります。それを可能な限り少なくするというコンセプトで作られました。
uMCPを使えば、AIが人間の操作に頼らずに長時間自走してくれるでしょう。

## ✨ 機能

uMCPは8個のUnity MCPコマンドを提供し、コンパイル、ログ取得、テスト実行、Unity Search、MenuItems操作など包括的な機能を提供します。

### 主要機能
現在 下記8個のコマンドが使えます。
**Unity基本コマンド**:
- リフレッシュ & コンパイル（compile）- コンパイル結果を返却します
- ログ取得（getlogs）- logtype、文字列、取得件数などでのフィルタリング機能を備えています
- テスト実行（runtests）- テスト結果をxmlで書き出し、書き出したpathを伝えます
- コンソールクリア（clearconsole）

**Unity検索・発見機能**:
- Unity Search API実行（unitysearch）
- Unity Searchプロバイダー詳細取得（getproviderdetails）
- MenuItems取得（getmenuitems）
- MenuItems実行（executemenuitem）- AIがテストコードを作成し、実行させるのに便利です

**📖 詳細な機能仕様については [FEATURES_ja.md](FEATURES_ja.md) をご覧ください**

**高度な機能**:
- 型安全パラメータ
- 自動タイミング測定
- 動的タイムアウト
- リアルタイムツール発見
- ファイルエクスポートシステム

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
        "[Unity Package Path]/.TypeScriptServer/dist/server.bundle.js"
      ],
      "env": {
        "UNITY_TCP_PORT": "{port}"
      }
    }
  }
}
```

**パス例**:
- **Package Manager経由**: `"/Users/username/UnityProject/Library/PackageCache/io.github.hatayama.umcp@[hash]/.TypeScriptServer/dist/server.bundle.js"`
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


## 🔧 カスタムコマンド開発

uMCPは動的カスタムコマンド登録をサポートしています。開発者はコアパッケージを変更することなく独自のコマンドを追加できます。

**📖 カスタムコマンド開発の詳細については [FEATURES_ja.md](FEATURES_ja.md#カスタムコマンド開発) をご覧ください**

### 開発方法
- **方法1**: [McpTool]属性による自動登録（推奨）
- **方法2**: CustomCommandManagerによる手動登録
- **デバッグ**: 登録済みコマンドの確認機能

## ライセンス
MIT License