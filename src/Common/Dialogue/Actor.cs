using Newtonsoft.Json;
using DiscoAPI.Common.Assets;

namespace DiscoAPI.Common.Dialogue;

public record Actor : Asset, IAssetRef<Actor>
{
    [JsonProperty("name")]
    public string displayName;

    [JsonProperty("portrait")]
    public string? portraitName;

    public Actor(string id, string displayName) : base(id)
    {
        this.displayName = displayName;
    }

    [JsonIgnore]
    public new AssetLocation<Actor> Location => new(source, id);
    Actor? IAssetRef<Actor>.Resolve(IDiscoManager mgr) => (Actor?)((IAssetRef)this).Resolve(mgr);
}
