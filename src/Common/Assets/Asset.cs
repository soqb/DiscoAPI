using System;
using Newtonsoft.Json;

namespace DiscoAPI.Common.Assets;

public abstract class Asset
{
    public abstract AssetType Type { get; }
    [JsonIgnore]
    public AssetRef Ref => new(this);
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
    Skill,
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

public record AssetRef
{
    public string sourceGuid;
    public AssetId id;
    public AssetType type;
    public AssetRef(AssetType type, string source, AssetId id)
    {
        this.type = type;
        sourceGuid = source;
        this.id = id;

        if (sourceGuid == null || sourceGuid == "") throw new Exception($"an asset ref ({id}, {type}) was created without a source guid.");
    }
    public AssetRef(AssetType type, string source, int id)
        : this(type, source, (AssetIdInt)id) { }
    public AssetRef(AssetType type, string source, string id)
        : this(type, source, (AssetIdString)id) { }
    public AssetRef(Asset asset)
        : this(asset.Type, asset.sourceGuid!, (AssetIdString)asset.id) { }
    public AssetRef(AssetType type, IDiscoSource source, int id)
        : this(type, source.Guid, (AssetIdInt)id) { }
    public AssetRef(AssetType type, IDiscoSource source, string id)
        : this(type, source.Guid, (AssetIdString)id) { }

    public override string ToString() => $"{sourceGuid}:{id}";
}
