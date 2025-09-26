using System;
using Newtonsoft.Json;

namespace DiscoAPI.Common.Assets;

public abstract record Asset : IAssetRef
{

    [JsonIgnore]
    public string? source;
    [JsonIgnore]
    public readonly AssetType assetType;

    [JsonProperty(Order = int.MinValue)]
    public readonly string id;
    public Asset(string id)
    {
        this.id = id;
        assetType = new(GetType());
    }

    Asset IAssetRef.Resolve(IDiscoManager mgr) => this;

    [JsonIgnore]
    public AssetLocation Location => new(assetType, source, id);
}

public readonly record struct AssetType(Type type);

public interface LocalIdResolver<T>
{
    T Resolve(int id);
    T Resolve(string id);
}

public record AssetId
{
    private readonly object inner;

    private AssetId(object inner)
    {
        this.inner = inner;
    }

    public T ResolveWith<T>(LocalIdResolver<T> resolver)
    {
        if (inner is string id) return resolver.Resolve(id);
        else return resolver.Resolve((int)inner);
    }

    public static implicit operator AssetId(int id) => new((object)id);
    public static implicit operator AssetId(string id) => new((object)id);

    public override string ToString() => inner.ToString()!;
}

public interface IAssetRef
{
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
    public readonly AssetType type;
    public readonly string source;
    public readonly AssetId id;

    public AssetLocation(AssetType type, string? source, AssetId id)
    {
        if (source == null || source == "") throw new Exception($"an asset ref ({id}, {type}) was created without a source guid.");

        this.type = type;
        this.id = id;
        this.source = source;
    }

    public AssetLocation(AssetLocation location)
    {
        type = location.type;
        id = location.id;
        source = location.source;
    }

    public AssetLocation(AssetType type, IDiscoSource source, AssetId id) : this(type, source.Guid, id) { }
    public AssetLocation(AssetType type, AssetId id) : this(type, "disco", id) { }

    AssetLocation IAssetRef.Location => new(this);
    public Asset? Resolve(IDiscoManager mgr) => mgr.Assets.Resolve(this);

    public override string ToString() => $"{source}:{id}";

    // public static (string, string) Parse(string id)
    // {
    //     string[] parts = id.Split(':');
    //     return parts.Length == 1 ? ("disco", parts[0]) : (parts[0], parts[1]);
    // }
}

public record AssetLocation<T> : AssetLocation, IAssetRef<T> where T : Asset
{
    public AssetLocation(AssetId id) : this("disco", id) { }
    public AssetLocation(string? source, AssetId id) : base(new(typeof(T)), source, id) { }

    public AssetLocation(IDiscoSource source, AssetId id) : this(source.Guid, id) { }

    AssetLocation<T> IAssetRef<T>.Location => new(this);
    AssetLocation IAssetRef.Location => new(this);

    public new T? Resolve(IDiscoManager mgr) => (T?)base.Resolve(mgr);
    Asset? IAssetRef.Resolve(IDiscoManager mgr) => Resolve(mgr);

    public override string ToString() => $"{source}:{id}";
}
