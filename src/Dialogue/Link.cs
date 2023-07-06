namespace DiscoAPI.Dialogue;

public record Link
{
    public record LineRef
    {
        public ConversationRef conversation;
        public int lineID;

        public LineRef(ConversationRef conversation, int lineID)
        {
            this.conversation = conversation;
            this.lineID = lineID;
        }
        public LineRef(DialogueSource source, int conversationID, int lineID)
            : this(new(source, conversationID), lineID) { }
    }

    public LineRef? from;
    public LineRef to;

    public Link(LineRef? from, LineRef to)
    {
        this.from = from;
        this.to = to;
    }

    public Link(LineRef from)
        : this(null, from) { }

    public Link(DialogueSource source, int conversationID, int lineID)
        : this(new LineRef(source, conversationID, lineID)) { }

}