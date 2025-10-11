using DiscoAPI.Common;
using DiscoAPI.Common.Assets;
using Newtonsoft.Json;

public record Thought : Asset, IAssetRef<Thought>
{
     public readonly Modifier[] researchEffects;
     public readonly Modifier[] completionEffects;
     
     public Thought(string id, Modifier[] researchEffects, Modifier[] completionEffects) : base(id)
     {
          this.researchEffects = researchEffects;
          this.completionEffects = completionEffects;
     }
     
     [JsonIgnore]
     public new AssetLocation<Thought> Location => new(source, id);
     Thought? IAssetRef<Thought>.Resolve(IDiscoManager mgr) => (Thought?)((IAssetRef)this).Resolve(mgr);
     
}