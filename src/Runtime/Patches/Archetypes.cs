using System;
using Cpp2IL.Core;
using DiscoAPI.Common.Assets;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Dynamic.Utils;
using UnityEngine;
using Array = Il2CppSystem.Array;
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
    private static void OnInitializeButtons(ref FourArchetypeSelector __instance)
    {
        var modTypes = ArchetypeUtils.ModArchetypes;

        for (var i = 0; i < Math.Min(4, modTypes.Count); i++)
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
                __instance.archetypes.AddLast(template);
            }
            
            if (i < __instance.portraits.Length)
            {
                __instance.portraits[i] = portrait;
            }
            else
            {
                var newArray = new Sprite[i + 1];
                __instance.portraits.CopyTo(newArray, 0);
                newArray[i] = portrait;
                __instance.portraits = newArray;
            }

        }
    }
    
    [HarmonyPatch(typeof(FourArchetypeSelector), nameof(FourArchetypeSelector.InitializeButtons))]
    [HarmonyPostfix]
    private static void OnInitializeButtonsPostfix(ref FourArchetypeSelector __instance)
    {
        var modTypes = ArchetypeUtils.ModArchetypes;

        for (var i = 0; i < Math.Min(4, modTypes.Count); i++)
        {
            var modType = modTypes[i];
            if (modType == null) continue;

            var createdButton = __instance.archetypeButtons[i];

            createdButton.IsCustomCharacterButton = modType.mode == CharacterArchetype.ArchetypeMode.CustomCharacter;
            createdButton.SetPortrait(__instance.portraits[i]);
            __instance.archetypeButtons[i] = createdButton;
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ArchetypeSelectButton), nameof(ArchetypeSelectButton.PlayShowAnimation))]
    private static void OnShowAnimation(ArchetypeSelectButton __instance)
    {
        if (!__instance.IsCustomCharacterButton)
        {
            __instance.Int.FlipClockNumber.Clear();
            __instance.Int.FlipClockNumber.SetValue(__instance.Archetype.Intellect);
            __instance.Psy.FlipClockNumber.Clear();
            __instance.Psy.FlipClockNumber.SetValue(__instance.Archetype.Psyche);
            __instance.Phq.FlipClockNumber.Clear();
            __instance.Phq.FlipClockNumber.SetValue(__instance.Archetype.Fysique);
            __instance.Mot.FlipClockNumber.Clear();
            __instance.Mot.FlipClockNumber.SetValue(__instance.Archetype.Motorics);
        }
        __instance.animatorHelper.Visible = true;
    }
}