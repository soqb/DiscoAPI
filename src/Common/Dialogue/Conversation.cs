using System.Collections.Generic;
using Newtonsoft.Json;
using DiscoAPI.Common.Assets;

namespace DiscoAPI.Common.Dialogue;

public class Conversation : Asset
{
    [JsonIgnore]
    public override AssetType Type => AssetType.Conversation;

    public string? name;
    public string? description;
    [JsonIgnore]
    public List<Line> lines;

    public Conversation(string id, List<Line> lines) : base(id)
    {
        this.lines = lines;
    }
}
