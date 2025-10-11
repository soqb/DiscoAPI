using DiscoAPI.Common.Assets;
using DiscoAPI.Runtime.Assets;
using UnityEngine;
using SM = Sunshine.Metric;

namespace DiscoAPI.Runtime.Utils;

public class ModifierUtils
{
    public static EnumArena<SM.EffectType, CharacterEffect> Effects => (EnumArena<SM.EffectType, CharacterEffect>)DiscoRunner.manager.Assets.GetArena<CharacterEffect>();
    
    public static CharacterEffect? Lookup(SM.EffectType et) => Effects[Effects.ReverseId(et)];

    public static bool EffectIsVanilla(SM.EffectType type) => (int)type <= Common.Assets.Effects.VANILLA_MAX;
    public static CharacterEffect RecoverEffect(SM.EffectType type)
    {
        return new CharacterEffect(FormatUtils.Slugify(type.ToString()), null, null);
    }

    public static bool EffectIsReal(SM.EffectType type) => type switch
    {
        SM.EffectType.NONE => false,
        SM.EffectType.COMMUNISM_XP_BONUS => false,
        SM.EffectType.DRUGS_ARE_BAD_MKAY => false,
        _ => true
    };

    public static SM.CharacterEffect ResolveComponent(GameObject container, Modifier ef)
    {
        var smEffect = container.AddComponent<SM.CharacterEffect>();
        smEffect.stringParameter = ef.stringParam ?? "";
        smEffect.parameter = ef.intParam ?? 0;
        smEffect.abilityType = ef.abilityType.HasValue
            ? SkillUtils.AbilityToSunshine(ef.abilityType.Value)
            : SM.AbilityType.Error;
        smEffect.skillType = ef.skillType != null 
            ? SkillUtils.Skills.GetRaw(ef.skillType.ResolveId())
            : SM.SkillType.NONE;
        if (ef is RaiseLearningCapModifier rlcm)
        {
            // Ensures skill/ability types correctly set for ThoughtAlterant.GetSkillCapType incase user gives contradictory data
            if (rlcm.mode == Common.Assets.Effects.RaiseMode.ALL_BY)
            {
                smEffect.abilityType = SM.AbilityType.Error;
                smEffect.skillType = SM.SkillType.NONE;
            }
            else if (rlcm.mode == Common.Assets.Effects.RaiseMode.ABILITY_BY)
            {
                smEffect.skillType = SM.SkillType.ALT;
                if (smEffect.abilityType == SM.AbilityType.Error)
                {
                    DiscoRunner.Log.LogError("RaiseLearningCapModifier: You opted to raise an Ability Cap but did not provide a valid ability for the modifier!");
                }
            } else if (rlcm.mode == Common.Assets.Effects.RaiseMode.SKILL_BY)
            {
                smEffect.abilityType = SM.AbilityType.Error;
            }
            else
            {
                smEffect.skillType = SM.SkillType.NONE;
            }
        }
        smEffect.quipLine = ef.quipLine;
        return smEffect;
    }

}