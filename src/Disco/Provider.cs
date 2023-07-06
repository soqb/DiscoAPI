using DiscoAPI;

namespace DiscoAPI;

public interface DiscoProvider
{
    /// <summary>
    /// The globally-unique identifier for this plugins in reverse domain name notation.
    /// <see href="https://en.wikipedia.org/wiki/Reverse_domain_name_notation" />
    /// </summary>
    public string Guid { get; }
    public virtual void OnDialogueBundleLoad(DiscoSource source) { }
    public virtual void OnSceneLoad(DiscoSource source) { }
}