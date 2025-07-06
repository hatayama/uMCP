# MCP C# SDK — **Parameter Description & Structured Input Guide**
*Updated 2025‑07‑06*

本ドキュメントは **DescriptionAttribute** による引数説明の付与方法と、
**独自クラス / record** を使った構造化パラメータの定義に特化してまとめたものです  
（`DelegatingMcpServerTool` など前処理フックは割愛）。

---

## 1. ツール定義の最小形

```csharp
[McpServerToolType]                   // クラス = ツールの“入れ物”
public static class EchoTool
{
    [McpServerTool,                   // メソッド = 1 つのツール
     Description("Echoes the message back to the client.")]
    public static string Echo(string message) => $"hello {message}";
}
```

SDK はリフレクションでこれを走査し、**入力スキーマ (JSON‑Schema)** と  
**出力スキーマ (Structured Output を有効化した場合)** を自動生成します。

---

## 2. `DescriptionAttribute` でパラメータに説明を埋め込む

```csharp
[McpServerToolType]
public static class EchoTool
{
    [McpServerTool,
     Description("Echoes the message back to the client.")]
    public static string Echo(
        [Description("The text the client sent. This will be echoed back verbatim.")]
        string message)                           // ★ 引数に説明を書く
        => $"hello {message}";
}
```

生成される **input schema** 抜粋:

```jsonc
{
  "type": "object",
  "properties": {
    "message": {
      "type": "string",
      "description": "The text the client sent. This will be echoed back verbatim."
    }
  },
  "required": ["message"]
}
```

*ポイント*

* `System.ComponentModel.DescriptionAttribute` を付与すると  
  `description` フィールドにそのまま落ちる
* メソッド自体の説明は `[Description(...)]` をメソッドに付ける  
  （ツールレベルの `Description`）

---

## 3. 引数を 1 つのクラス / record にまとめる

### 3‑1. 基本形

```csharp
public record PersonToCall(
    [property: Description("E.164 形式の電話番号")] string PhoneNumber,
    [property: Description("相手の表示名")]          string Name,
    [property: Description("発話する挨拶文")]        string? Greeting = null // optional
);

[McpServerToolType]
public static class CallTools
{
    [McpServerTool,
     Description("指定した相手に電話をかける")]
    public static void Call(PersonToCall person) =>
        Console.WriteLine($"Calling {person.Name}…");
}
```

生成される **input schema** 抜粋:

```jsonc
{
  "type": "object",
  "properties": {
    "person": {                // メソッド引数名がトップレベル key
      "type": "object",
      "properties": {
        "phoneNumber": { "type": "string", "description": "E.164 形式の電話番号" },
        "name":        { "type": "string", "description": "相手の表示名" },
        "greeting":    { "type": "string", "description": "発話する挨拶文" }
      },
      "required": ["phoneNumber", "name"]         // Nullable でないものだけ必須
    }
  },
  "required": ["person"]
}
```

### 3‑2. カスタマイズのヒント

| やりたいこと | 書き方 |
|--------------|--------|
| **JSON に別名を付けたい** | `[JsonPropertyName("phone")]` |
| **スキーマから除外したい** | `[JsonIgnore]` |
| **Optional フィールド** | `string?`, `int?`, `List<T>?` など *Nullable* にする |
| **配列／リスト** | `IEnumerable<T>` / `T[]` をプロパティに定義 |
| **ネストを減らす** | メソッド引数を *単一* にすることで `{ "argName": { … } }` を生成<br>複数引数にするとフラットになる |

> ⚠️ **ネストが深いと LLM が迷う** ため、3〜5 フィールド程度の浅い構造が推奨。

---

## 4. 出力スキーマ (Structured Output) を使う場合

```csharp
[McpServerTool(UseStructuredContent = true)]
public static AddResult Add(int x, int y) => new(x + y);

public record AddResult(int Sum);
```

`UseStructuredContent = true` を付けると、戻り値型の **output schema** が公開され、
`CallToolResult.StructuredContent` にシリアライズ済みで返却されます。  
単純な文字列返却 (`string`) の場合は従来どおり `TextContentBlock` 1 件で十分です。

---

## 5. 最小動作サンプル (Quick Start)

```bash
dotnet new console -n MinimalMcpServer
cd MinimalMcpServer
dotnet add package ModelContextProtocol --prerelease
dotnet add package Microsoft.Extensions.Hosting
```

`Program.cs`:

```csharp
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddMcpServer()
      .WithToolsFromAssembly(typeof(Program).Assembly)  // EchoTool など自動登録
      .WithStdioServerTransport();                      // Copilot 等から呼び出せる

await builder.Build().RunAsync();
```

---

## 6. 参考リンク

* GitHub リポジトリ: <https://github.com/modelcontextprotocol/csharp-sdk>
* Quick Start (Microsoft Learn): <https://learn.microsoft.com/dotnet/ai/quickstarts/build-mcp-server>
* `McpServerTool` API リファレンス: <https://modelcontextprotocol.github.io/csharp-sdk/api/ModelContextProtocol.Server.McpServerTool.html>

*最小のコードに **DescriptionAttribute** を付与し、  
必要に応じて record/POCO を引数に取るだけで、LLM との I/O を型安全に定義できます。*