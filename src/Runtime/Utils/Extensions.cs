using System.Collections.Generic;
using System.Linq;
using DiscoAPI.Common.Assets;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
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

    public static SM.InventoryItem AttachComponent(this Item item, GameObject container)
    {
        var smItem = container.AddComponent<SM.InventoryItem>();
        smItem.name = item.id;
        smItem.itemValue = item.valueInCents;
        smItem.isVessel = item.isVessel;
        smItem.conversation = item.conversation ?? "";
        smItem.multipleAllowed = item.multipleAllowed;
        smItem.sound = item.pickupSound ?? "";
        smItem.stackName = item.stackingItem?.Resolve()?.id ?? "";

        if (item is EquippableItem equippable)
        {
            smItem.type = (ItemType)equippable.equipSlot;
            smItem.equipOrbName = equippable.equipOrbName ?? "";
            if (equippable.coversSlots != null)
            {
                var smCoverList = new List<ItemType>();
                foreach (var slot in equippable.coversSlots)
                {
                    smCoverList.Add((ItemType)slot);
                }

                smItem.VisualHideItems = smCoverList.ToArray();
            }
            smItem.autoEquip = equippable.autoEquip;
            smItem.equipEffects = equippable.equipEffects?.Select(ef => ef.AttachComponent(container)).ToArray() ?? [];
        }
        else
        {
            smItem.equipEffects = new Il2CppReferenceArray<SM.CharacterEffect>(0);
            smItem.type = ItemType.NONE;
        }

        if (item is SubstanceItem substance)
        {
            smItem.consumable = true;
            smItem.substance = true;
            smItem.group = (ItemGroup)substance.group;
            smItem.substanceBuffs = CreateBuff(substance.substanceEffects, container);
        }

        return smItem;
    }

    public static SM.CharacterBuff[] CreateBuff(Modifier[]? modifiers, GameObject container)
    {
        if (modifiers == null) return [];
        var buff = container.AddComponent<SM.CharacterBuff>();
        buff.cause = SM.ModifierType.ELECTROCHEMISTRY;
        buff.effects = modifiers.Select(ef => ef.AttachComponent(container)).ToArray();
        return [buff];
    }
}