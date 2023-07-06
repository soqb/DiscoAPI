using System.Collections.Generic;

namespace DiscoAPI.Dialogue;

public class Conversation : Asset
{
    public List<Line> lines;

    public Conversation(int internalID, string id, List<Line> lines) : base(internalID, id)
    {
        this.lines = lines;
    }
}