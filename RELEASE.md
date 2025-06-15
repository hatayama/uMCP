# UnityMCP リリース手順

## 概要

UnityMCPパッケージは、Unity Package Manager経由でGit URLから直接インストール可能な形で配布されます。

## 配布戦略

### 1. Pre-built Assets方式
- TypeScript Serverは事前にビルドしてリポジトリにコミット
- ユーザーは**npm install不要**でインストール可能
- ⚠️ **Node.js ランタイムは必須** - MCP Serverの実行に必要

### 2. Git URL配布
```
https://github.com/hatayama/UnityMCP.git?path=/Packages/src
```

## ユーザー要件

### 必須ソフトウェア
- **Unity 2020.3以上**
- **Node.js 18.0以上** ⭐ **必須**
- **Cursor エディタ**

### インストール不要
- npm install（依存関係は事前解決済み）
- TypeScript コンパイラ
- webpack等のビルドツール

## リリース手順

### 開発時
1. TypeScript Serverのソースコード変更
2. ローカルでビルド確認
   ```bash
   cd Packages/src/TypeScriptServer
   npm run build
   ```
3. 変更をコミット・プッシュ
4. GitHub Actionsが自動でビルド・コミット

### リリース時
1. バージョン番号を更新
   ```bash
   # package.jsonのversionを更新
   vim Packages/src/package.json
   ```

2. CHANGELOGを更新
   ```bash
   vim Packages/src/CHANGELOG.md
   ```

3. Gitタグを作成・プッシュ
   ```bash
   git tag v0.1.0
   git push origin v0.1.0
   ```

4. GitHub Actionsが自動でリリース作成

## ユーザーインストール手順

### 1. 前提条件確認
```bash
# Node.jsバージョン確認
node --version  # v18.0.0以上であることを確認
```

### 2. Unity Package Manager
1. Unity Editor > Window > Package Manager
2. "+" > "Add package from git URL"
3. URL入力: `https://github.com/hatayama/UnityMCP.git?path=/Packages/src`

### 3. Cursor設定
`.cursor/mcp.json` に以下を追加：
```json
{
  "mcpServers": {
    "unity-mcp": {
      "command": "node",
      "args": [
        "[Unity Package Path]/TypeScriptServer/dist/server.js"
      ]
    }
  }
}
```

### 特定バージョン指定
```
https://github.com/hatayama/UnityMCP.git?path=/Packages/src#v0.1.0
```

## ファイル構成

```
Packages/src/                    # Unity Package Root
├── package.json                 # Unity Package定義
├── README.md                    # インストール・使用方法
├── CHANGELOG.md                 # 変更履歴
├── Editor/                      # Unity Editor Scripts
│   ├── *.cs                    # C# Scripts
│   └── *.asmdef                # Assembly Definition
└── TypeScriptServer/           # 埋め込みTypeScript Server
    ├── package.json            # Node.js依存関係（開発用）
    ├── src/                    # TypeScriptソース（開発用）
    └── dist/                   # ビルド済みファイル（実行用）
        ├── server.js           # メインサーバー ⭐ 実行ファイル
        ├── unity-client.js     # Unity通信クライアント
        ├── tools/              # 各種ツール
        └── types/              # 型定義
```

## 注意事項

### コミット対象
- ✅ `dist/` フォルダ（ビルド済みファイル）
- ❌ `node_modules/` フォルダ
- ❌ `package-lock.json`（開発時のみ）

### ユーザー体験
- ✅ **簡単**: npm install不要
- ✅ **高速**: 依存関係解決不要
- ⚠️ **Node.js必須**: ランタイムとして必要

### GitHub Actions
- mainブランチプッシュ時：自動ビルド・コミット
- タグプッシュ時：リリース作成
- PR時：ビルド検証のみ

### バージョニング
- Semantic Versioning (semver) に従う
- `v0.1.0`, `v0.1.1`, `v0.2.0` 形式
- Unity package.jsonのversionと一致させる

## トラブルシューティング

### Node.js関連
```bash
# Node.jsインストール確認
node --version
npm --version

# パス確認
which node
```

### 実行エラー
```bash
# 直接実行テスト
cd [Unity Package Path]/TypeScriptServer
node dist/server.js
``` 