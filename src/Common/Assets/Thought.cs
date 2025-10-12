using Newtonsoft.Json;

namespace DiscoAPI.Common.Assets;

public record Thought : Asset, IAssetRef<Thought>
{
     public string imageLocation;
     public string displayName;
     public string descripton;
     public string completionDescription;
     public readonly int researchMins;
     public readonly Modifier[] researchEffects;
     public readonly Modifier[] completionEffects;
     
     public Thought(string id, string displayName, int researchMins, Modifier[] researchEffects, Modifier[] completionEffects, string imageLocation, string descripton, string completionDescription) : base(id)
     {
          this.researchEffects = researchEffects;
          this.completionEffects = completionEffects;
          this.imageLocation = imageLocation;
          this.researchMins = researchMins;
          this.displayName = displayName;
          this.descripton = descripton;
          this.completionDescription = completionDescription;
     }
     
     [JsonIgnore]
     public new AssetLocation<Thought> Location => new(source, id);
     Thought? IAssetRef<Thought>.Resolve(IDiscoManager mgr) => (Thought?)((IAssetRef)this).Resolve(mgr);
     
}