using Newtonsoft.Json;
using DiscoAPI.Common.Assets;

namespace DiscoAPI.Common.Dialogue;

public class Actor : Asset
{
    [JsonIgnore]
    public override AssetType Type => AssetType.Actor;
    [JsonProperty("name")]
    public string displayName;

    [JsonProperty("portrait")]
    public string? portraitName;

    public Actor(string id, string displayName) : base(id)
    {
        this.displayName = displayName;
    }

}
