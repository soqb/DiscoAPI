using Newtonsoft.Json;

namespace DiscoAPI.Common.Assets;

public record Thought : Asset, IAssetRef<Thought>
{
     public readonly string bigImageLocation;
     public readonly string iconImageLocation;
     public readonly string displayName;
     public readonly string descripton;
     public readonly string completionDescription;
     public readonly int researchMins;
     public readonly Modifier[] researchEffects;
     public readonly Modifier[] completionEffects;
     
     public Thought(string id, string displayName, int researchMins, string descripton, string completionDescription,
          string bigImageLocation, string iconImageLocation, Modifier[] researchEffects, Modifier[] completionEffects) : base(id)
     {
          this.researchEffects = researchEffects;
          this.completionEffects = completionEffects;
          this.bigImageLocation = bigImageLocation;
          this.iconImageLocation = iconImageLocation;
          this.researchMins = researchMins;
          this.displayName = displayName;
          this.descripton = descripton;
          this.completionDescription = completionDescription;
     }
     
     [JsonIgnore]
     public new AssetLocation<Thought> Location => new(source, id);
     Thought? IAssetRef<Thought>.Resolve(IDiscoManager mgr) => (Thought?)((IAssetRef)this).Resolve(mgr);
     
}