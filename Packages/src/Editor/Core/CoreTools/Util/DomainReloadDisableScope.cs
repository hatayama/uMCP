using System;
using UnityEditor;

namespace io.github.hatayama.uLoopMCP
{
    public class DomainReloadDisableScope : IDisposable
    {
        private readonly bool originalEnabled;
        private readonly EnterPlayModeOptions originalOptions;
        
        public DomainReloadDisableScope()
        {
            originalEnabled = EditorSettings.enterPlayModeOptionsEnabled;
            originalOptions = EditorSettings.enterPlayModeOptions;
            
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
        }
        
        public void Dispose()
        {
            EditorSettings.enterPlayModeOptionsEnabled = originalEnabled;
            EditorSettings.enterPlayModeOptions = originalOptions;
        }
    }
}