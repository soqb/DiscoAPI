using System.Text.Json.Serialization;

namespace DiscoAPI.Common.Dialogue;

public class Actor : Asset
{
    [JsonIgnore]
    public override AssetType Type => AssetType.Actor;
    [JsonIgnore]
    public override AssetRef Ref => new ActorRef(this);
    [JsonPropertyName("name")]
    public string displayName;

    public Actor(string id, string displayName) : base(id)
    {
        this.displayName = displayName;
    }

}
