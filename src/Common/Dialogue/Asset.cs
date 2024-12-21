using Newtonsoft.Json;

namespace DiscoAPI.Common.Dialogue;

public abstract class Asset
{
    public abstract AssetType Type { get; }
    public abstract AssetRef Ref { get; }
    [JsonIgnore]
    public string? sourceGuid;
    public string id;
    public Asset(string id)
    {
        this.id = id;
    }
}

public enum AssetType
{
    Actor,
    Conversation,
    Variable,
}

public interface LocalIdResolver<T>
{
    public T ResolveId(int id);
    public T ResolveId(string id);
}

public interface AssetId
{
    public T ResolveIn<T>(LocalIdResolver<T> resolver);
}

public struct AssetIdInt : AssetId
{
    public int id;

    public T ResolveIn<T>(LocalIdResolver<T> resolver) => resolver.ResolveId(id);

    public static implicit operator AssetIdInt(int id) => new AssetIdInt { id = id };
    public override string ToString() => id.ToString();
}

public struct AssetIdString : AssetId
{
    public string id;

    public T ResolveIn<T>(LocalIdResolver<T> resolver) => resolver.ResolveId(id);

    public static implicit operator AssetIdString(string id) => new AssetIdString { id = id };
    public override string ToString() => id.ToString();
}

public abstract record AssetRef
{
    public string sourceGuid;
    public AssetId id;
    public abstract AssetType Type { get; }
    public AssetRef(string source, AssetId id)
    {
        sourceGuid = source;
        this.id = id;
    }
    public AssetRef(string source, int id)
        : this(source, (AssetIdInt)id) { }
    public AssetRef(string source, string id)
        : this(source, (AssetIdString)id) { }
    public AssetRef(Asset asset)
        : this(asset.sourceGuid!, (AssetIdString)asset.id) { }

    public override string ToString() => $"{sourceGuid}:{id}";
}

public record ConversationRef : AssetRef
{
    public override AssetType Type => AssetType.Conversation;
    public ConversationRef(IDiscoSource source, AssetId id) : base(source.Guid, id) { }
    public ConversationRef(IDiscoSource source, int id) : base(source.Guid, id) { }
    public ConversationRef(IDiscoSource source, string id) : base(source.Guid, id) { }
    public ConversationRef(string source, AssetId id) : base(source, id) { }
    public ConversationRef(string source, int id) : base(source, id) { }
    public ConversationRef(string source, string id) : base(source, id) { }
    public ConversationRef(Conversation asset) : base(asset) { }

    public static implicit operator ConversationRef(Conversation asset) => new(asset);

    public override string ToString() => $"{sourceGuid}:{id}";
}

public record ActorRef : AssetRef
{
    public override AssetType Type => AssetType.Actor;
    public ActorRef(IDiscoSource source, AssetId id) : base(source.Guid, id) { }
    public ActorRef(IDiscoSource source, int id) : base(source.Guid, id) { }
    public ActorRef(IDiscoSource source, string id) : base(source.Guid, id) { }
    public ActorRef(string source, AssetId id) : base(source, id) { }
    public ActorRef(string source, int id) : base(source, id) { }
    public ActorRef(string source, string id) : base(source, id) { }
    public ActorRef(Actor asset) : base(asset) { }

    public static implicit operator ActorRef(Actor asset) => new(asset);

    public override string ToString() => $"{sourceGuid}:{id}";
}

public record VariableRef : AssetRef
{
    public override AssetType Type => AssetType.Variable;
    public VariableRef(IDiscoSource source, AssetId id) : base(source.Guid, id) { }
    public VariableRef(IDiscoSource source, int id) : base(source.Guid, id) { }
    public VariableRef(IDiscoSource source, string id) : base(source.Guid, id) { }
    public VariableRef(string source, AssetId id) : base(source, id) { }
    public VariableRef(string source, int id) : base(source, id) { }
    public VariableRef(string source, string id) : base(source, id) { }
    public VariableRef(Variable asset) : base(asset) { }

    public static implicit operator VariableRef(Variable asset) => new(asset);

    public override string ToString() => $"{sourceGuid}:{id}";
}
