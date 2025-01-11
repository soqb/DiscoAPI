using DiscoAPI.Common.Assets;

namespace DiscoAPI.Common.Dialogue;

public record LineRef
{
    public IAssetRef<Conversation> conversation;
    public int lineId;

    public LineRef(IAssetRef<Conversation> conversation, int line)
    {
        this.conversation = conversation;
        lineId = line;
    }
    public LineRef(string source, AssetId conversationId, int line)
        : this(new AssetLocation<Conversation>(source, conversationId), line) { }
    public LineRef(IDiscoSource source, AssetId conversationId, int line)
        : this(new AssetLocation<Conversation>(source.Guid, conversationId), line) { }

    public override string ToString() => $"{conversation}#{lineId}";
}

public struct Link
{

    public LineRef? from;
    public LineRef to;

    public Link(LineRef? from, LineRef to)
    {
        this.from = from;
        this.to = to;
    }

    public Link(LineRef from)
        : this(null, from) { }

    public Link(IDiscoSource source, int conversationID, int lineID)
        : this(new LineRef(source, conversationID, lineID)) { }
}
