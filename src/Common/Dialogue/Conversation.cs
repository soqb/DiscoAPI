using System.Collections.Generic;
using Newtonsoft.Json;
using DiscoAPI.Common.Assets;

namespace DiscoAPI.Common.Dialogue;

public class Conversation : Asset, IAssetRef<Conversation>
{
    public string? name;
    public string? description;
    [JsonIgnore]
    public List<Line> lines;


    public Conversation(string id, List<Line> lines) : base(id)
    {
        this.lines = lines;
    }

    [JsonIgnore]
    public new AssetLocation<Conversation> Location => new(source, id);
    Conversation? IAssetRef<Conversation>.Resolve(IDiscoManager mgr) => (Conversation?)((IAssetRef)this).Resolve(mgr);
}
