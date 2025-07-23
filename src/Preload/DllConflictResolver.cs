using System.Reflection;
using BepInEx.Preloader.Core.Patching;

namespace DiscoAPI.Preload;

[PatcherPluginInfo("DiscoAPI.Preload", "DiscoAPI.Preload", "0.0.1")]
public class DllConflictResolverPatch : BasePatcher
{
    private const string ProvidedNewtonsoftPath = "BepInEx/plugins/DiscoAPI/Newtonsoft.Json.dll";
    
    public override void Initialize()
    {
        Assembly.LoadFrom(ProvidedNewtonsoftPath);
    }
}