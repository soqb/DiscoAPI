using System;
using Newtonsoft.Json;

namespace DiscoAPI.Common.Assets;

public abstract class Asset : IAssetRef
{
    [JsonIgnore]
    public AssetType Type => TypeOf(GetType());

    [JsonIgnore]
    public string? source;
    public string id;
    public Asset(string id)
    {
        this.id = id;
    }

    Asset IAssetRef.Resolve(IDiscoManager mgr) => this;

    [JsonIgnore]
    public AssetLocation Location => new(Type, source, id);

    public static AssetType TypeOf<T>() where T : Asset => TypeOf(typeof(T));

    public static AssetType TypeOf(Type type)
    {
        if (type.IsAssignableTo(typeof(Dialogue.Actor))) return AssetType.Actor;
        if (type.IsAssignableTo(typeof(Dialogue.Conversation))) return AssetType.Conversation;
        if (type.IsAssignableTo(typeof(Dialogue.Variable))) return AssetType.Variable;
        if (type.IsAssignableTo(typeof(Skill))) return AssetType.Skill;

        throw new InvalidOperationException($"{type.FullName} is not a recognised asset type");
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

interface IAssetId
{
    T ResolveIn<T>(LocalIdResolver<T> resolver);
}

record struct AssetIdInt(int id) : IAssetId
{
    public T ResolveIn<T>(LocalIdResolver<T> resolver) => resolver.ResolveId(id);

    public override string ToString() => id.ToString();
}

record struct AssetIdString(string id) : IAssetId
{
    public T ResolveIn<T>(LocalIdResolver<T> resolver) => resolver.ResolveId(id);

    public override string ToString() => id.ToString();
}

public class AssetId
{
    private IAssetId inner;

    private AssetId(IAssetId inner)
    {
        this.inner = inner;
    }

    public T ResolveIn<T>(LocalIdResolver<T> resolver) => inner.ResolveIn(resolver);

    public static implicit operator AssetId(int id) => new(new AssetIdInt(id));
    public static implicit operator AssetId(string id) => new(new AssetIdString(id));
}

public interface IAssetRef
{
    AssetType Type { get; }

    Asset? Resolve(IDiscoManager mgr);
    AssetLocation Location { get; }
}

public interface IAssetRef<T> : IAssetRef where T : Asset
{
    new T? Resolve(IDiscoManager mgr);
    new AssetLocation<T> Location { get; }
}

public record AssetLocation : IAssetRef
{
    public AssetType type;
    public string source;
    public AssetId id;

    AssetType IAssetRef.Type => type;

    public AssetLocation(AssetType type, string? source, AssetId id)
    {
        if (source == null || source == "") throw new Exception($"an asset ref ({id}, {type}) was created without a source guid.");

        this.type = type;
        this.id = id;
        this.source = source;
    }

    public AssetLocation(AssetType type, IDiscoSource source, AssetId id) : this(type, source.Guid, id) { }

    AssetLocation IAssetRef.Location => new(this);
    public Asset? Resolve(IDiscoManager mgr) => mgr.Assets.Resolve(this);

    public override string ToString() => $"{source}:{id}";
}

public record AssetLocation<T> : AssetLocation, IAssetRef<T> where T : Asset
{
    public AssetLocation(string? source, AssetId id) : base(Asset.TypeOf<T>(), source, id) { }

    public AssetLocation(IDiscoSource source, AssetId id) : this(source.Guid, id) { }

    AssetLocation<T> IAssetRef<T>.Location => new(this);
    AssetLocation IAssetRef.Location => new AssetLocation<T>(this);

    public new T? Resolve(IDiscoManager mgr) => (T?)base.Resolve(mgr);
    Asset? IAssetRef.Resolve(IDiscoManager mgr) => base.Resolve(mgr);

    public override string ToString() => $"{source}:{id}";
}
