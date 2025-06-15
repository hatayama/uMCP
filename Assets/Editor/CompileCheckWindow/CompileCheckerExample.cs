using UnityEditor;
using UnityEngine;
using System.Threading.Tasks;

namespace io.github.hatayama.uMCP
{
    public class CompileCheckerExample
    {
        [MenuItem("Tools/CompileWindow/コンパイルチェッカー使用例")]
        public static async void TestCompileChecker()
        {
            CompileChecker compileChecker = new CompileChecker();
            
            try
            {
                // まさみちが要求した使い方
                CompileResult result = await compileChecker.TryCompileAsync();
                var err = result.error;
                var warning = result.warning;

                Debug.Log($"コンパイル結果: 成功={result.Success}");
                Debug.Log($"エラー数: {err.Length}");
                Debug.Log($"警告数: {warning.Length}");

                // エラーの詳細を表示
                foreach (var error in err)
                {
                    Debug.LogError($"エラー: {error.message} at {error.file}:{error.line}");
                }

                // 警告の詳細を表示
                foreach (var warn in warning)
                {
                    Debug.LogWarning($"警告: {warn.message} at {warn.file}:{warn.line}");
                }
            }
            finally
            {
                compileChecker.Dispose();
            }
        }

        [MenuItem("Tools/CompileWindow/強制コンパイルチェッカー使用例")]
        public static async void TestForceCompileChecker()
        {
            CompileChecker compileChecker = new CompileChecker();
            
            try
            {
                // 強制再コンパイルの例
                CompileResult result = await compileChecker.TryCompileAsync(forceRecompile: true);
                var err = result.error;
                var warning = result.warning;

                Debug.Log($"強制コンパイル結果: 成功={result.Success}");
                Debug.Log($"エラー数: {err.Length}, 警告数: {warning.Length}");
            }
            finally
            {
                compileChecker.Dispose();
            }
        }
    }
} 