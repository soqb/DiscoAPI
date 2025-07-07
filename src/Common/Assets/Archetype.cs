using System.Collections.Generic;

namespace DiscoAPI.Common.Assets;

public record CharacterArchetype : Asset, IAssetRef<CharacterArchetype>
{
    public readonly IList<(AssetLocation<Skill>, int)>? skillBonuses;
    public readonly AssetLocation<Skill> signatureSkill;
    public readonly int intellect;
    public readonly int psyche;
    public readonly int fysique;
    public readonly int motorics;
    public readonly string description;
    public readonly string name;
    public readonly string portraitLocation;
    
    public CharacterArchetype(string id, AssetLocation<Skill> signatureSkill, int intellect, int psyche, int fysique, int motorics, 
        string description, string name, string portraitLocation, IList<(AssetLocation<Skill>, int)>? skillBonuses = null) 
        : base(id)
    {
        this.skillBonuses = skillBonuses;
        this.signatureSkill = signatureSkill;
        this.intellect = intellect;
        this.psyche = psyche;
        this.fysique = fysique;
        this.motorics = motorics;
        this.description = description;
        this.name = name;
        this.portraitLocation = portraitLocation;
    }

    // needs to move to runtime
    // public SunshineCharacterTemplate ToSunshineTemplate()
    // {
    //     var template = ScriptableObject.CreateInstance<SunshineCharacterTemplate>();
    //     template.Description = description;
    //     template.name = name;
    //     template.Intellect = intellect;
    //     template.Psyche = psyche;
    //     template.Fysique = fysique;
    //     template.Motorics = motorics;
    //     template.signatureSkill = SkillUtils.Skills.GetRaw(signatureSkill.ResolveId());
    //
    //     if (skillBonuses == null) return template;
    //     
    //     foreach ((AssetLocation<Skill> skill, int bonus) in skillBonuses)
    //     {
    //         var resolved = skill.Resolve(DiscoRunner.manager);
    //     }
    //     
    //     return template;
    // }

    public new AssetLocation<CharacterArchetype> Location => new(source, id);
    CharacterArchetype? IAssetRef<CharacterArchetype>.Resolve(IDiscoManager mgr) => (CharacterArchetype?)((IAssetRef)this).Resolve(mgr);
}