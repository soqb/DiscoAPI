using BepInEx.Unity.IL2CPP;
using DiscoAPI.Runtime.Assets;
namespace DiscoAPI.Runtime;

public interface IDiscoProvider
{
    /// <summary>
    /// The globally-unique identifier for this plugin.
    /// <see href="https://en.wikipedia.org/wiki/Reverse_domain_name_notation" />
    /// </summary>
    public string Guid { get; }
    public DiscoSource Source { get; }

    public virtual void OnRegister() { }
    public virtual void OnDialogueBundleLoad() { }
    public virtual void OnSceneLoad() { }
}

public class DiscoPlugin : BasePlugin, IDiscoProvider
{
    public string Guid => Log.SourceName;
    public DiscoSource Source { get; }
    public AssetSource Assets => Source.Assets;

    public DiscoPlugin()
    {
        Source = DiscoRunner.manager.CreateSource(Guid, Log);
    }

    public virtual void OnRegister() { }
    public virtual void OnDialogueBundleLoad() { }
    public virtual void OnSceneLoad() { }
    void IDiscoProvider.OnRegister() => OnRegister();
    void IDiscoProvider.OnDialogueBundleLoad() => OnDialogueBundleLoad();
    void IDiscoProvider.OnSceneLoad() => OnSceneLoad();

    public override void Load()
    {
        DiscoRunner.Register(this);
    }
}
