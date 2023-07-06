using System.Collections.Generic;
using PC = PixelCrushers.DialogueSystem;
using System;
using Sunshine.Metric;

namespace DiscoAPI.Dialogue;

public abstract class Asset
{
    public string? sourceGuid;
    public int internalID;
    public string id;
    public Asset(int internalID, string id)
    {
        this.internalID = internalID;
        this.id = id;
    }
}

public abstract record AssetRef
{
    public string sourceGuid;
    public int id;
    public AssetRef(DialogueSource source, int id)
    {
        sourceGuid = source.Guid;
        this.id = id;
    }
    public AssetRef(Asset asset)
    {
        sourceGuid = asset.sourceGuid!;
        this.id = asset.internalID;
    }
    public abstract int IndexOffset(DialogueSource source);
    public int AsId(DialogueManager mng)
    {
        return IndexOffset(mng.GetSource(sourceGuid)) + id;
    }

    public override string ToString()
    {
        return $"{sourceGuid}:{id}";
    }
}

public record ConversationRef : AssetRef
{
    public ConversationRef(DialogueSource source, int id) : base(source, id) { }
    public ConversationRef(Conversation asset) : base(asset) { }

    public override int IndexOffset(DialogueSource source)
    {
        return source.conversationOffset;
    }
}

public record ActorRef : AssetRef
{
    public ActorRef(DialogueSource source, int id) : base(source, id) { }
    public ActorRef(Actor asset) : base(asset) { }

    public override int IndexOffset(DialogueSource source)
    {
        return source.actorOffset;
    }
}

public record VariableRef : AssetRef
{
    public VariableRef(DialogueSource source, int id) : base(source, id) { }
    public VariableRef(Variable asset) : base(asset) { }

    public override int IndexOffset(DialogueSource source)
    {
        return source.variableOffset;
    }
}