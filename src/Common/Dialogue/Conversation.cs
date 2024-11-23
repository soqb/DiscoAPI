using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DiscoAPI.Common.Dialogue;

public class Conversation : Asset
{
    [JsonIgnore]
    public override AssetType Type => AssetType.Conversation;
    [JsonIgnore]
    public override AssetRef Ref => new ConversationRef(this);

    public string? name;
    public string? description;
    [JsonIgnore]
    public List<Line> lines;

    public Conversation(string id, List<Line> lines) : base(id)
    {
        this.lines = lines;
    }
}
