各種toolの定義方法を変更したい。

現在は3つのクラスから成り立っている。
1. XxxCommand
2. XxxResponse
3. XxxSchema

これを変更し、staticクラスを宣言するだけでOKとする
例は下記。特徴は
- staticクラス
- XxxSchemaがなくなり、直接引数で宣言している。[Description]アトリビュートで説明する
- Responseクラスも不要。Pingメソッドの返り値Task<PingToolResult>で判断している

```
    [McpServerToolType]
    public static class PingTools
    {
        /// <summary>
        /// Connection test and message echo
        /// </summary>
        [McpServerTool(Name = "ping")]
        [Description("Connection test and message echo")]
        public static Task<PingToolResult> Ping(
            [Description("Message to send to Unity")] 
            string message = "Hello from TypeScript MCP Server",
            CancellationToken cancellationToken = default)
        {
            // Echo the message back with Unity prefix
            string response = $"Unity MCP Bridge received: {message}";
            
            return Task.FromResult(new PingToolResult(
                message: response,
                receivedMessage: message,
                timestamp: System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            ));
        }
        
        /// <summary>
        /// Result for ping tool
        /// </summary>
        public record PingToolResult(
            [property: Description("The response message from Unity")] string message,
            [property: Description("The original message that was received")] string receivedMessage,
            [property: Description("Timestamp when the ping was processed")] string timestamp
        );
    }

```

このように変更をたのむ。
すでに Packages/src/Editor/Api/Attributes/McpAttributes.cs にattributeは定義されている。

先行実装として、下記3つがある。
Packages/src/Editor/Api/Tools/PingTools.cs
Packages/src/Editor/Api/Tools/SetClientNameTools.cs
Packages/src/Editor/Api/Tools/GetCommandDetailsTools.cs

まずは起動時に必須のGetCommandDetailsCommandも変更が必須だ。

後方互換を考える必要はない。
まずはpingメソッドが正しく動作するようにしてほしい。


BaseCommandResponse、BaseCommandSchemaに定義されている共通プロパティは不要。実装する必要なし。
まずはpingコマンドが正常に動くところまで確認する。

それが出来たらユーザーに確認すること。

具体的なクラス定義が無くなったため、リフレクションを使用して動的に情報を構築する必要がある。そのためのシステムを最初に作らねばならない。



