using System;
using Newtonsoft.Json;

namespace DiscoAPI.Common.Assets;

public record Reputation : Asset, IAssetRef<Reputation>
{
    /// <summary>
    /// Triggers when reputation is increased.
    /// </summary>
    public readonly Action? repIncreaseEffect;
    /// <summary>
    /// Orb to prompt the player with when the threshold is met.
    /// </summary>
    public readonly string? confrontationOrbName;
    /// <summary>
    /// The level of reputation required to trigger the confrontation orb. Default: 3
    /// </summary>
    public readonly int? confrontationTriggerThreshold;
    
    public Reputation(string id, Action? repIncreaseEffect = null, 
        int? confrontationTriggerThreshold = null, string? confrontationOrbName = null) : base(id)
    {
        this.repIncreaseEffect = repIncreaseEffect;
        this.confrontationTriggerThreshold = confrontationTriggerThreshold;
        this.confrontationOrbName = confrontationOrbName;
    }
    
    [JsonIgnore]
    public new AssetLocation<Reputation> Location => new(source, id);
    Reputation? IAssetRef<Reputation>.Resolve(IDiscoManager mgr) => (Reputation?)((IAssetRef)this).Resolve(mgr);
}