using UnityEditor;
using UnityEngine;

namespace io.github.hatayama.uMCP
{
    public class CompileCheckerExample
    {
        [MenuItem("uMCP/CompileWindow/Compile Checker Usage Example")]
        public static async void TestCompileChecker()
        {
            CompileChecker compileChecker = new CompileChecker();
            
            try
            {
                // How Masamichi requested to use it
                CompileResult result = await compileChecker.TryCompileAsync();
                var err = result.error;
                var warning = result.warning;

                Debug.Log($"Compilation result: Success={result.Success}");
                Debug.Log($"Number of errors: {err.Length}");
                Debug.Log($"Number of warnings: {warning.Length}");

                // Display error details
                foreach (var error in err)
                {
                    Debug.LogError($"Error: {error.message} at {error.file}:{error.line}");
                }

                // Display warning details
                foreach (var warn in warning)
                {
                    Debug.LogWarning($"Warning: {warn.message} at {warn.file}:{warn.line}");
                }
            }
            finally
            {
                compileChecker.Dispose();
            }
        }

        [MenuItem("uMCP/CompileWindow/Force Compile Checker Usage Example")]
        public static async void TestForceCompileChecker()
        {
            CompileChecker compileChecker = new CompileChecker();
            
            try
            {
                // Example of forced re-compilation
                CompileResult result = await compileChecker.TryCompileAsync(forceRecompile: true);
                var err = result.error;
                var warning = result.warning;

                Debug.Log($"Forced compilation result: Success={result.Success}");
                Debug.Log($"Number of errors: {err.Length}, Number of warnings: {warning.Length}");
            }
            finally
            {
                compileChecker.Dispose();
            }
        }
    }
} 