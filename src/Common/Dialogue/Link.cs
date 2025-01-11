using DiscoAPI.Common.Assets;

namespace DiscoAPI.Common.Dialogue;

public record LineRef(IAssetRef<Conversation> conversation, int lineId)
{
    public LineRef(string source, AssetId conversationId, int line)
        : this(new AssetLocation<Conversation>(source, conversationId), line) { }
    public LineRef(IDiscoSource source, AssetId conversationId, int line)
        : this(new AssetLocation<Conversation>(source.Guid, conversationId), line) { }

    public override string ToString() => $"{conversation}#{lineId}";
}

public readonly record struct Link(LineRef? from, LineRef to)
{
    public Link(LineRef to)
        : this(null, to) { }

    public Link(IDiscoSource source, int conversationID, int lineID)
        : this(new LineRef(source, conversationID, lineID)) { }
}
