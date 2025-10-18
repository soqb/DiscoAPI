using System.IO;
using System.Reflection;
using BepInEx.Preloader.Core.Patching;

namespace DiscoAPI.Preload;

[PatcherPluginInfo("DiscoAPI.Preload", "DiscoAPI.Preload", "0.1.0-alpha")]
public class DllConflictResolverPatch : BasePatcher
{
    private const string ProvidedNewtonsoftPath = "BepInEx/plugins/DiscoAPI/Newtonsoft.Json.dll";
    
    public override void Initialize()
    {
        if (!File.Exists(ProvidedNewtonsoftPath))
        {
            this.Log.LogWarning("Attempted to patch Newtonsoft.Json but the replacement library cannot be found! Path: " + ProvidedNewtonsoftPath);
            return;
        }
        
        Assembly.LoadFrom(ProvidedNewtonsoftPath);
    }
}