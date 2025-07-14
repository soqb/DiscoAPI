using System.Collections.Generic;

namespace DiscoAPI.Common.Assets;

public record CharacterArchetype : Asset, IAssetRef<CharacterArchetype>
{
    public readonly IList<(AssetLocation<Skill>, int)>? skillBonuses;
    public readonly AssetLocation<Skill> signatureSkill;
    public ArchetypeMode mode;
    public readonly int intellect;
    public readonly int psyche;
    public readonly int fysique;
    public readonly int motorics;
    public readonly string description;
    public readonly string name;
    public readonly string portraitLocation;
    
    public CharacterArchetype(string id, AssetLocation<Skill> signatureSkill, int intellect, int psyche, int fysique, int motorics, 
        string description, string name, string portraitLocation, ArchetypeMode mode = ArchetypeMode.Template, IList<(AssetLocation<Skill>, int)>? skillBonuses = null) 
        : base(id)
    {
        this.skillBonuses = skillBonuses;
        this.signatureSkill = signatureSkill;
        this.mode = mode;
        this.intellect = intellect;
        this.psyche = psyche;
        this.fysique = fysique;
        this.motorics = motorics;
        this.description = description;
        this.name = name;
        this.portraitLocation = portraitLocation;
    }

    public new AssetLocation<CharacterArchetype> Location => new(source, id);
    CharacterArchetype? IAssetRef<CharacterArchetype>.Resolve(IDiscoManager mgr) => (CharacterArchetype?)((IAssetRef)this).Resolve(mgr);
    
    public enum ArchetypeMode
    {
        Template,
        CustomCharacter
    }
}