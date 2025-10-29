using System;
using Newtonsoft.Json;

namespace DiscoAPI.Common.Assets;

public record Modifier
{
    public readonly string? stringParam;
    public readonly int? intParam;
    public readonly IAssetRef<Skill>? skillType;
    public readonly AbilityType? abilityType;
    /// <summary>
    /// Line that comes after the effect amount
    /// </summary>
    public readonly string quipLine;
    public readonly IAssetRef<CharacterEffect> baseEffect;

    public Modifier(string quipLine, IAssetRef<CharacterEffect> baseEffect, string? stringParam = null, int? intParam = null, IAssetRef<Skill>? skillType = null, AbilityType? abilityType = null)
    {
        this.stringParam = stringParam;
        this.intParam = intParam;
        this.skillType = skillType;
        this.abilityType = abilityType;
        this.quipLine = quipLine;
        this.baseEffect = baseEffect;
    }
}

public record DamageOnceModifier : Modifier
{
    public DamageOnceModifier(string quipLine, Effects.CoreSkill target, int amount)
        : base(quipLine, Effects.DamageOnce, skillType: target == Effects.CoreSkill.VOLITION ? Skills.Volition : Skills.Endurance, intParam: amount)
    {
    }
}

public record HealOnceModifier : Modifier
{
    public HealOnceModifier(string quipLine, Effects.CoreSkill target, int amount)
        : base(quipLine, Effects.HealOnce, skillType: target == Effects.CoreSkill.VOLITION ? Skills.Volition : Skills.Endurance, intParam: amount)
    {
    }
}

public record RaiseLearningCapModifier : Modifier
{
    public readonly Effects.RaiseMode mode;
    public RaiseLearningCapModifier(string quipLine, Effects.RaiseMode mode, int amount, IAssetRef<Skill>? skillType = null, AbilityType? abilityType = null)
        : base(quipLine, Effects.RaiseLearningCap, skillType: skillType, abilityType: abilityType, intParam: amount)
    {
        this.mode = mode;
    }
}

public record CharacterEffect : Asset, IAssetRef<CharacterEffect>
{
    /// <summary>
    /// Should this effect be re-applied after saves are loaded?
    /// </summary>
    public bool isSavePersistent;
    public Action<Modifier>? applyEffect;
    public Action<Modifier>? removeEffect;

    public CharacterEffect(string id, Action<Modifier>? applyEffect, Action<Modifier>? removeEffect = null, bool isSavePersistent = true) : base(id)
    {
        this.applyEffect = applyEffect;
        this.removeEffect = removeEffect;
        this.isSavePersistent = isSavePersistent;
    }

    [JsonIgnore]
    public new AssetLocation<CharacterEffect> Location => new(source, id);
    CharacterEffect? IAssetRef<CharacterEffect>.Resolve(IDiscoManager mgr) => (CharacterEffect?)((IAssetRef)this).Resolve(mgr);
}


public static class Effects
{
    public const int VANILLA_MAX = 27;
    public enum CoreSkill { VOLITION, ENDURANCE }
    public enum RaiseMode { ALL_BY, ABILITY_BY, ABILITY_TO, SKILL_BY}
    public static AssetLocation<CharacterEffect> SkillBonus => new("skill-bonus");
    public static AssetLocation<CharacterEffect> AbilityBonus => new("stat-bonus");
    public static AssetLocation<CharacterEffect> DamageOnce => new("damage");
    public static AssetLocation<CharacterEffect> HealOnce => new("heal");
    public static AssetLocation<CharacterEffect> SetBoolean => new("set-variable");
    public static AssetLocation<CharacterEffect> OrbsGiveMoney => new("thc-orb-money");
    public static AssetLocation<CharacterEffect> OrbsGiveXp => new("thc-orb-xp");
    public static AssetLocation<CharacterEffect> ExpandCritRange => new("thc-crit-range-expand");
    public static AssetLocation<CharacterEffect> RedChecksFail => new("thc-red-check-failure");
    public static AssetLocation<CharacterEffect> BonusAgainstMen => new("male-target");
    public static AssetLocation<CharacterEffect> BonusAgainstWomen => new("female-target");
    public static AssetLocation<CharacterEffect> BonusAgainstKim => new("kim-target");
    public static AssetLocation<CharacterEffect> ReopenWhiteChecks => new("reopen-white");
    public static AssetLocation<CharacterEffect> RaiseLearningCap => new("max-learning-cap");
    public static AssetLocation<CharacterEffect> RunLua => new("lua-command");
    public static AssetLocation<CharacterEffect> NoShirtSkillBonus => new("skill-bonus-without-shirt");
    public static AssetLocation<CharacterEffect> UnarmedSkillBonus => new("skill-bonus-when-unarmed");
    public static AssetLocation<CharacterEffect> PassivesSucceed => new("passives-succeed");
    public static AssetLocation<CharacterEffect> PassiveDifficulty => new("passive-target-modifier");
    public static AssetLocation<CharacterEffect> GiveXpOnce => new("xp-reward");
    public static AssetLocation<CharacterEffect> FindBetterTare => new("find-better-items");
    public static AssetLocation<CharacterEffect> ReputationBonus => new("reputation-bonus");
    public static AssetLocation<CharacterEffect> CameraMaxZoomLimit => new("modify-camera-max-zoom-limit");
}