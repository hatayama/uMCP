using System;
using UnityEditor;

namespace io.github.hatayama.uMCP
{
    /// <summary>
    /// ScriptableSingletonの保存をマルチスレッドセーフに行うヘルパー
    /// </summary>
    public static class ScriptableSingletonHelper
    {
        // Note: ScriptableSingleton.Save()はprotectedなので、このヘルパーは使用できません
        // 代わりに各ScriptableSingletonクラス内でSaveを実行する必要があります
    }
}