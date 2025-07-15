using System;
using System.Linq;
using DiscoAPI.Common.Assets;
using HarmonyLib;
using LocalizationCustomSystem;
using TMPro;
using UnityEngine;
using SM = Sunshine.Metric;

namespace DiscoAPI.Runtime.Patches;

public static class ArchetypePatches
{
    [HarmonyPatch(typeof(ArchetypeSelectButton), nameof(ArchetypeSelectButton.SetArchetype))]
    [HarmonyPrefix]
    private static bool OnSetArchetype(ArchetypeSelectButton __instance, SunshineCharacterTemplate archetype)
    {
        __instance.Archetype = archetype;
        __instance.NameLocalization.mLocalizeTarget.Cast<TextMeshProUGUI>().text = archetype.name;
        __instance.DescriptionLocalization.mLocalizeTarget.Cast<TextMeshProUGUI>().text = archetype.Description;
        __instance.SignatureSkillLocalization.mLocalizeTarget.Cast<TextMeshProUGUI>().text = 
            SkillUtils.GetActorSkillName(archetype.signatureSkill);
        __instance.Int.SetData(SM.AbilityType.INT, __instance.Archetype);
        __instance.Psy.SetData(SM.AbilityType.PSY, __instance.Archetype);
        __instance.Phq.SetData(SM.AbilityType.FYS, __instance.Archetype);
        __instance.Mot.SetData(SM.AbilityType.MOT, __instance.Archetype);
        return false;
    }

    [HarmonyPatch(typeof(FourArchetypeSelector), nameof(FourArchetypeSelector.InitializeButtons))]
    [HarmonyPrefix]
    private static void OnInitializeButtons(ref FourArchetypeSelector __instance)
    {
        var modTypes = ArchetypeUtils.ModArchetypes;
        var typeCount = modTypes.Count;

        if (typeCount >= 4) // these arrays only account for 3 archetypes but we let modders provide 4
        {
            __instance.portraits = __instance.portraits.Resize(typeCount);
            __instance.archetypes = __instance.archetypes.Resize(typeCount);
        }

        for (var i = 0; i < Math.Min(4, typeCount); i++)
        {
            var modType = modTypes[i];
            if (modType == null) continue;

            var template = ArchetypeUtils.ToSunshineTemplate(modType);
            var portrait = ArchetypeUtils.LoadArchetypeSprite(modType);
            
            __instance.archetypes[i] = template;
            __instance.portraits[i] = portrait;
        }
        
    }
    
    [HarmonyPatch(typeof(FourArchetypeSelector), nameof(FourArchetypeSelector.InitializeButtons))]
    [HarmonyPostfix]
    private static void OnInitializeButtonsPostfix(ref FourArchetypeSelector __instance)
    {
        var modTypes = ArchetypeUtils.ModArchetypes;
        var typeCount = modTypes.Count;
        
        if (typeCount >= 4) // ignore vanilla custom button in favor of our own but keep the positioning 
        {
            var customChar = __instance.CustomCharacterButton;
            __instance.archetypeButtons.Remove(customChar);
            __instance.archetypeButtons[3].transform.parent = customChar.transform.parent;
            __instance.archetypeButtons[3].transform.position = new Vector3(1.21f, 58.6486f, 101.0723f); 
            customChar.gameObject.SetActive(false); 
        }

        for (var i = 0; i < Math.Min(4, typeCount); i++)
        {
            var createdButton = __instance.archetypeButtons[i];
            if (createdButton.Archetype == null) continue;

            var modType = modTypes.FirstOrDefault(a => a.name == createdButton.Archetype.name);
            if (modType == null) continue;

            if (modType.mode == CharacterArchetype.ArchetypeMode.CustomCharacter)
            {
                createdButton.IsCustomCharacterButton = true;
                createdButton.Int.gameObject.SetActive(false);
                createdButton.Psy.gameObject.SetActive(false);
                createdButton.Mot.gameObject.SetActive(false);
                createdButton.Phq.gameObject.SetActive(false);
                createdButton.transform.GetChild(0).GetChild(8).gameObject.SetActive(false);
            }
            
            // createdButton.SetPortrait(__instance.portraits[i]);

        }
        
    }
    
}