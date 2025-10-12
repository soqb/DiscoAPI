using System.Linq;
using DiscoAPI;
using DiscoAPI.Common.Assets;
using UnityEngine;
using SM = Sunshine.Metric;

namespace DiscoAPI.Runtime.Utils;

public static class Extensions
{
    public static SM.CharacterEffect AttachComponent(this Modifier ef, GameObject container)
    {
        var smEffect = container.AddComponent<SM.CharacterEffect>();
        smEffect.stringParameter = ef.stringParam ?? "";
        smEffect.parameter = ef.intParam ?? 0;
        smEffect.effect = ModifierUtils.Effects.GetRaw(ef.baseEffect.ResolveId());
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

    public static SM.ThoughtCabinetProject AttachComponent(this Thought thought, GameObject container)
    {
        var tcp = container.AddComponent<SM.ThoughtCabinetProject>();
        tcp.completionEffects = thought.completionEffects.Select(ef => ef.AttachComponent(container)).ToArray();
        tcp.researchEffects = thought.researchEffects.Select(ef => ef.AttachComponent(container)).ToArray();
        tcp.researchTime = thought.researchMins;
        return tcp;
    }
}