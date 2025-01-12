using HarmonyLib;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using SM = Sunshine.Metric;
using DiscoAPI.Runtime.Dialogue;

namespace DiscoAPI.Runtime.Patches;

public static class PagesPatches
{

	[HarmonyPatch(typeof(ActorsPortraitsBundleManager), nameof(ActorsPortraitsBundleManager.LoadPortraitSpriteAsync))]
	[HarmonyPrefix]
	private static bool OnLoadPortraitSpriteAsync(ref AsyncOperationHandle<Sprite?> __result, string textureName, Il2CppSystem.Action<AsyncOperationHandle<Sprite?>> del)
	{
		if (PixelsToDisco.TryDecodeTextureName(textureName, out string source, out string path))
		{
			__result = DiscoRunner.GetSource(source)!.Router.Portraits.Get(path);
			__result.add_Completed(del);
			return false;
		}

		return true;
	}

	// [HarmonyPatch(typeof(SkillPortraitPanel), nameof(SkillPortraitPanel.LoadSkillPortraitAsync))]
	// [HarmonyPrefix]
	// private static void OnLoadSkillPortraitAsync(SkillPortraitPanel __instance)
	// {
	// 	DiscoAPIPlugin.Instance.Log.LogInfo($"nb that there are {CharacterSheet.reverseIndex.Count} sheets.");

	// 	PixelCrushers.DialogueSystem.Actor actor = CharacterSheetTooltip.ActorFromModifiable(__instance.currentSkill);
	// 	DiscoAPIPlugin.Instance.Log.LogInfo($"{__instance.currentSkill.skillType} is about to load '{actor?.Name ?? "noone"}'");
	// }

	[HarmonyPatch(typeof(CharacterSheetInfoPanel), nameof(CharacterSheetInfoPanel.ShowSkill))]
	[HarmonyPrefix]
	private static bool OnShowSkill(CharacterSheetInfoPanel __instance, SM.Skill skill)
	{
		if (skill.skillType == SM.SkillType.NONE)
		{
			__instance.tabPanel.SetActive(false);
			return false;
		}
		else
		{
			return true;
		}
	}

	[HarmonyPatch(typeof(SkillPortraitPanel), nameof(SkillPortraitPanel.OnSelectButtonClicked))]
	[HarmonyPatch(typeof(SkillPortraitPanel), nameof(SkillPortraitPanel.OnPointerEnter))]
	[HarmonyPatch(typeof(SkillPortraitPanel), nameof(SkillPortraitPanel.OnPointerExit))]
	[HarmonyPrefix]
	private static bool PrePointerEvent(SkillPortraitPanel __instance)
	{
		if (__instance.skill == SM.SkillType.NONE) return false;
		else return true;
	}

	[HarmonyPatch(typeof(SkillPortraitPanel), nameof(SkillPortraitPanel.UpdateData))]
	[HarmonyPrefix]
	private static bool OnUpdateData(SkillPortraitPanel __instance)
	{
		if (__instance.skill == SM.SkillType.NONE)
		{
			__instance.isHovered = false;
			__instance.isSelectHovered = false;
			__instance.skillPortrayLabel.skillNumber.enabled = false;
			__instance.UpdateSelectionVisuals();
			return false;
		}
		else return true;
	}

	[HarmonyPatch(typeof(Charsheet.SkillPortrayConfigurator), nameof(Charsheet.SkillPortrayConfigurator.UpdateSkillPortraitPanelsContent))]
	[HarmonyPrefix]
	private static bool OnSkillConfiguratorUpdateSkillPortraitPanelsContent(Charsheet.SkillPortrayConfigurator __instance)
	{
		var skills = (SM.SkillType[])System.Enum.GetValues(typeof(SM.SkillType));
		for (int i = 0, j = 0; i < skills.Length; i++)
		{
			SM.SkillType skillType = skills[i];

			if (SkillUtils.IsExcludedFromPortraits(skillType)) continue;
			SkillPortraitPanel? skillPortraitPanel = null;

			foreach (var sk in __instance.skillList)
			{
				if (sk.name == skillType.ToString())
				{
					skillPortraitPanel = sk;
					break;
				}
			}

			if (skillPortraitPanel != null)
			{
				var skillRef = DiscoRunner.globalConfig.panels[j++].skill;
				skillType = skillRef == null ? SM.SkillType.NONE : SkillUtils.Skills.GetRaw(skillRef.ResolveId());
				SM.Skill skill = new SM.Skill(skillType, null);
				skillPortraitPanel.SetSkill(skill);
			}
		}

		if (CharsheetView.Singleton != null)
		{
			CharsheetView.Singleton.UpdateSkillPortraitPanels(__instance.skillList);
		}

		return false;
	}
}
