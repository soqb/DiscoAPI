using DiscoAPI.Common.Assets;
using HarmonyLib;
using SM = Sunshine.Metric;

namespace DiscoAPI.Runtime.Patches;

public static class ArchetypePatches
{
    [HarmonyPatch(typeof(ArchetypeSelectButton), nameof(ArchetypeSelectButton.SetArchetype))]
    [HarmonyPrefix]
    private static bool OnSetArchetype(ArchetypeSelectButton __instance, SunshineCharacterTemplate archetype)
    {
        __instance.Archetype = archetype;
        __instance.NameLocalization.Term = $"\0RAW\0{archetype.name}";
        __instance.DescriptionLocalization.Term = $"\0RAW\0{archetype.Description}";
        __instance.SignatureSkillLocalization.Term = $"\0RAW\0{SkillUtils.GetActorSkillName(archetype.signatureSkill)}";
        __instance.Int.SetData(SM.AbilityType.INT, __instance.Archetype);
        __instance.Psy.SetData(SM.AbilityType.PSY, __instance.Archetype);
        __instance.Phq.SetData(SM.AbilityType.FYS, __instance.Archetype);
        __instance.Mot.SetData(SM.AbilityType.MOT, __instance.Archetype);
        return false;
    }

    [HarmonyPatch(typeof(FourArchetypeSelector), nameof(FourArchetypeSelector.InitializeButtons))]
    [HarmonyPrefix]
    private static void OnSetPortrait(FourArchetypeSelector __instance)
    {
        var modTypes = ArchetypeUtils.ModArchetypes;

        for (var i = 0; i < modTypes.Count; i++)
        {
            var modType = modTypes[i];
            if (modType == null) continue;

            var template = ArchetypeUtils.ToSunshineTemplate(modType);
            var portrait = ArchetypeUtils.LoadArchetypeSprite(modType);
            
            if (i < __instance.archetypes.Length)
            {
                __instance.archetypes[i] = template;
            }
            else
            {
                __instance.archetypes.AddItem(template);
            }
            
            if (i < __instance.portraits.Length)
            {
                __instance.portraits[i] = portrait;
            }
            else
            {
                __instance.portraits.AddItem(portrait);
            }

        }
    }
    
    [HarmonyPatch(typeof(FourArchetypeSelector), nameof(FourArchetypeSelector.InitializeButtons))]
    [HarmonyPostfix]
    private static void OnSetPortraitPostfix(FourArchetypeSelector __instance)
    {
        var modTypes = ArchetypeUtils.ModArchetypes;

        for (var i = 0; i < modTypes.Count; i++)
        {
            var modType = modTypes[i];
            if (modType == null) continue;

            var createdButton = __instance.archetypeButtons[i];

            createdButton.IsCustomCharacterButton = modType.mode == CharacterArchetype.ArchetypeMode.CustomCharacter;
            createdButton.SetPortrait(__instance.portraits[i]);
        }
    }
}