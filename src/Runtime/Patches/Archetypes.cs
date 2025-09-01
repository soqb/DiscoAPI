using System;
using System.Linq;
using DiscoAPI.Common.Assets;
using DiscoAPI.Runtime.Components;
using DiscoAPI.Runtime.Dialogue;
using DiscoAPI.Runtime.Utils;
using HarmonyLib;
using I2.Loc;
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
        __instance.DescriptionLocalization.LocalizeEvent.AddListener(
            new Action(() => BogusLocalizationOverrideDelegate(archetype.Description)));
        __instance.NameLocalization.LocalizeEvent.AddListener(
            new Action(() => BogusLocalizationOverrideDelegate(archetype.name)));
        __instance.SignatureSkillLocalization.LocalizeEvent.AddListener(
            new Action(() => BogusLocalizationOverrideDelegate(SkillUtils.Lookup(archetype.signatureSkill)?.displayName ?? "NONE")));
        __instance.Int.SetData(SM.AbilityType.INT, __instance.Archetype);
        __instance.Psy.SetData(SM.AbilityType.PSY, __instance.Archetype);
        __instance.Phq.SetData(SM.AbilityType.FYS, __instance.Archetype);
        __instance.Mot.SetData(SM.AbilityType.MOT, __instance.Archetype);
        return false;
    }

    private static void BogusLocalizationOverrideDelegate(string trueString)
    {
        Localize.MainTranslation = trueString;
    }

    [HarmonyPatch(typeof(FourArchetypeSelector), nameof(FourArchetypeSelector.InitializeButtons))]
    [HarmonyPrefix]
    private static void OnInitializeButtons(FourArchetypeSelector __instance)
    {
        var modTypes = DiscoRunner.globalConfig.newGameArchetypes;
        var typeCount = modTypes.Length;

        if (typeCount >= 4) // these arrays only account for 3 archetypes but we let modders provide 4
        {
            __instance.portraits = __instance.portraits.Resize(4);
            __instance.archetypes = __instance.archetypes.Resize(4);
        }

        for (var i = 0; i < Math.Min(4, typeCount); i++)
        {
            var modType = modTypes[i];
            if (modType == null) continue;

            var template = ArchetypeUtils.ToSunshineTemplate(modType);
            var task = AssetUtils.LoadPortrait(
                DiscoToPixels.EncodeTextureName(modType.source!, modType.portraitLocation),
                null);
            int idx = i;
            task.ContinueWith(task => AssignPortrait(task, idx));
            __instance.archetypes[i] = template;
        }

        void AssignPortrait(DiscoTask<Sprite> handle, int idx)
        {
            var result = handle.Result;
            FourArchetypeSelector.Singleton.archetypeButtons[idx].SetPortrait(result);
        }
    }

    [HarmonyPatch(typeof(FourArchetypeSelector), nameof(FourArchetypeSelector.InitializeButtons))]
    [HarmonyPostfix]
    private static void OnInitializeButtonsPostfix(FourArchetypeSelector __instance)
    {
        var modTypes = DiscoRunner.globalConfig.newGameArchetypes;
        var typeCount = modTypes.Length;

        var customChar = __instance.CustomCharacterButton;
        __instance.archetypeButtons.Remove(customChar);

        if (typeCount >= 4) // ignore vanilla custom button in favor of our own but keep the positioning 
        {
            customChar.gameObject.SetActive(false);

            var fourthButton = __instance.archetypeButtons[3].transform.Cast<RectTransform>();
            fourthButton.parent = customChar.transform.parent;
            fourthButton.anchoredPosition = new Vector2(850, 53);
            fourthButton.pivot = new Vector2(1, 0.5f);
            fourthButton.anchorMax = new Vector2(0.5f, 0.5f);
            fourthButton.anchorMin = new Vector2(0.5f, 0.5f);
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
        }

    }

    [HarmonyPatch(typeof(SM.CharacterSheetFactory), nameof(SM.CharacterSheetFactory.TransferLeveledSkills))]
    [HarmonyPostfix]
    private static void OnTransferLeveledSkills(SunshineCharacterTemplate p, SM.CharacterSheet targetSheet)
    {
        var modType = DiscoRunner.globalConfig.newGameArchetypes.FirstOrDefault(a => a.name == p.name);
        if (modType?.skillBonuses == null) return;

        var sheetSkills = CharacterComponents.Skills.Of(targetSheet);
        if (sheetSkills == null)
        {
            DiscoRunner.Log.LogError("TransferLeveledSkills : your character sheet is invalid!");
            return;
        }
        foreach (var (skill, bonus) in modType.skillBonuses)
        {
            var rawSkill = sheetSkills.GetRawSkill(skill);
            SM.CharacterSheetFactory.TransferLeveledSkill(bonus, rawSkill);
        }
    }

}
