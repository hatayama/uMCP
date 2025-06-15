using System.Collections.Generic;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// MCP サーバー設定の比較を担当するクラス
    /// 単一責任原則：設定の比較のみを担当
    /// </summary>
    public static class McpServerConfigComparer
    {
        /// <summary>
        /// 2つの設定が等しいかチェック
        /// </summary>
        public static bool AreEqual(McpServerConfigData config1, McpServerConfigData config2)
        {
            // commandの比較
            if (config1.command != config2.command)
                return false;

            // argsの比較
            if (config1.args.Length != config2.args.Length)
                return false;
            
            for (int i = 0; i < config1.args.Length; i++)
            {
                if (config1.args[i] != config2.args[i])
                    return false;
            }

            // envの比較
            if (config1.env.Count != config2.env.Count)
                return false;

            foreach (KeyValuePair<string, string> kvp in config1.env)
            {
                if (!config2.env.ContainsKey(kvp.Key) || config2.env[kvp.Key] != kvp.Value)
                    return false;
            }

            return true;
        }
    }
} 