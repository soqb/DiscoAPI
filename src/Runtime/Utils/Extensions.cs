using System.Linq;
using DiscoAPI.Common.Assets;
using DiscoAPI.Runtime.Patches;
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
        smEffect.quipLineTerm = Rosetta.CreateTerm(ef.quipLine);
        return smEffect;
    }

    public static SM.ThoughtCabinetProject AttachComponent(this Thought thought, GameObject container)
    {
        var tcp = container.AddComponent<SM.ThoughtCabinetProject>();
        tcp.completionEffects = thought.completionEffects.Select(ef => ef.AttachComponent(container)).ToArray();
        tcp.researchEffects = thought.researchEffects.Select(ef => ef.AttachComponent(container)).ToArray();
        tcp.researchTime = thought.researchMins;
        tcp.displayNameTerm = Rosetta.CreateTerm(thought.displayName);
        tcp.completionDescriptionTerm = Rosetta.CreateTerm(thought.completionDescription);
        tcp.descriptionTerm = Rosetta.CreateTerm(thought.descripton);
        return tcp;
    }
    
    public static SunshineCharacterTemplate ToSunshineTemplate(this CharacterArchetype archetype)
    {
        var template = ScriptableObject.CreateInstance<SunshineCharacterTemplate>();
        template.Description = archetype.description;
        template.name = archetype.name;
        template.Intellect = archetype.intellect;
        template.Psyche = archetype.psyche;
        template.Fysique = archetype.fysique;
        template.Motorics = archetype.motorics;
        template.signatureSkill = archetype.signatureSkill != null
            ? SkillUtils.Skills.GetRaw(archetype.signatureSkill.ResolveId())
            : SM.SkillType.NONE;

        return template;
    }
}