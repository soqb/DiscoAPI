using System;

namespace DiscoAPI.Common.Assets;

public record Reputation : Asset, IAssetRef<Reputation>
{
    public readonly Action? repIncreaseEffect;
    public readonly string? confrontationOrbName;
    public readonly int? confrontationTriggerThreshold;
    
    public Reputation(string id, Action? repIncreaseEffect = null, 
        int? confrontationTriggerThreshold = null, string? confrontationOrbName = null) : base(id)
    {
        this.repIncreaseEffect = repIncreaseEffect;
        this.confrontationTriggerThreshold = confrontationTriggerThreshold;
        this.confrontationOrbName = confrontationOrbName;
    }
    
    public new AssetLocation<Reputation> Location => new(source, id);
    Reputation? IAssetRef<Reputation>.Resolve(IDiscoManager mgr) => (Reputation?)((IAssetRef)this).Resolve(mgr);
}