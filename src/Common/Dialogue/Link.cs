using DiscoAPI.Common.Assets;

namespace DiscoAPI.Common.Dialogue;

public record LineRef
{
    public IAssetRef<Conversation> conversation;
    public int lineID;

    public LineRef(IAssetRef<Conversation> conversation, int lineID)
    {
        this.conversation = conversation;
        this.lineID = lineID;
    }
    public LineRef(string source, int conversationID, int lineID)
        : this(new AssetLocation<Conversation>(source, conversationID), lineID) { }
    public LineRef(IDiscoSource source, int conversationID, int lineID)
        : this(new AssetLocation<Conversation>(source.Guid, conversationID), lineID) { }

    public override string ToString() => $"{conversation}#{lineID}";
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
