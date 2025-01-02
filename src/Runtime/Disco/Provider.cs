using System;
using System.IO;
using System.Reflection;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using DiscoAPI.Runtime.Assets;
namespace DiscoAPI.Runtime;

public interface IDiscoProvider
{
    /// <summary>
    /// The globally-unique identifier for this plugin.
    /// </summary>
    string Guid { get; }
    DiscoSource Source { get; }

    IAssetRouter Router { get; }

    virtual void OnRegister() { }
    virtual void OnDialogueBundleLoad() { }
    virtual void OnSceneLoad() { }
    virtual void OnUpdate() { }
}

public class DiscoPlugin : BasePlugin, IDiscoProvider
{
    public string Guid => Source.Guid;
    public DiscoSource Source { get; }
    public AssetSource Assets => Source.Assets;

    internal static bool FullPathEq(string a, string b)
    {
        string Norm(string x) => x.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        var cmp = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return string.Equals(Norm(a), Norm(b), cmp);
    }

    public static string? GetLocationFromAssembly(Assembly assembly, ManualLogSource? log)
    {
        string? path = Path.GetDirectoryName(assembly.Location);
        if (path == null)
        {
            if (log != null) log.LogWarning("no assembly path, so no viable location");
            return null;
        }

        DirectoryInfo? target = new DirectoryInfo(path).Parent;
        DirectoryInfo plugins = new(BepInEx.Paths.PluginPath);

        while (target != null)
        {
            if (FullPathEq(target.FullName, plugins.FullName))
            {
                return path;
            }

            target = target.Parent;
        }

        if (log != null) log.LogWarning("the assembly was not found to be inside the BepInEx plugins directory, so no viable location");
        return null;
    }

    public virtual string? Location { get; }
    public IAssetRouter Router { get; }

    public DiscoPlugin()
    {
        string guid = BepInEx.MetadataHelper.GetMetadata(this).GUID;
        Source = DiscoRunner.manager.CreateSource(guid, Log);
        Location = DiscoPlugin.GetLocationFromAssembly(GetType().Assembly, Log);
        Router = new MemoizedAssetRouter(GetRouter());

        Log.LogInfo($"my location is {Location}");
    }

    public virtual IAssetRouter GetRouter() => new EmptyAssetRouter();

    public virtual void OnRegister() { }
    public virtual void OnDialogueBundleLoad() { }
    public virtual void OnSceneLoad() { }
    public virtual void OnUpdate() { }
    void IDiscoProvider.OnRegister() => OnRegister();
    void IDiscoProvider.OnDialogueBundleLoad() => OnDialogueBundleLoad();
    void IDiscoProvider.OnSceneLoad() => OnSceneLoad();
    void IDiscoProvider.OnUpdate() => OnUpdate();

    public override void Load()
    {
        DiscoRunner.Register(this);
    }
}
