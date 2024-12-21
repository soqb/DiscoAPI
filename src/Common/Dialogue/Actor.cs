using Newtonsoft.Json;

namespace DiscoAPI.Common.Dialogue;

public class Actor : Asset
{
    [JsonIgnore]
    public override AssetType Type => AssetType.Actor;
    [JsonIgnore]
    public override AssetRef Ref => new ActorRef(this);
    [JsonProperty("name")]
    public string displayName;

    public Actor(string id, string displayName) : base(id)
    {
        this.displayName = displayName;
    }

}
