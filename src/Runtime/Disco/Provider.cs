namespace DiscoAPI.Runtime;

public interface DiscoProvider
{
    /// <summary>
    /// The globally-unique identifier for this plugin.
    /// <see href="https://en.wikipedia.org/wiki/Reverse_domain_name_notation" />
    /// </summary>
    public string Guid { get; }
    public virtual void OnRegister(DiscoSource source) { }
    public virtual void OnDialogueBundleLoad(DiscoSource source) { }
    public virtual void OnSceneLoad(DiscoSource source) { }
}
