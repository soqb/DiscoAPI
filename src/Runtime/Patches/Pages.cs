using HarmonyLib;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using SM = Sunshine.Metric;
using SV = Sunshine.Views;
using System;
using Il2CppInterop.Runtime;
using LocalizationCustomSystem;

namespace DiscoAPI.Runtime.Patches;

public static class PagesPatches
{
	[HarmonyPatch(typeof(ActorsPortraitsBundleManager), nameof(ActorsPortraitsBundleManager.LoadPortraitSpriteAsync))]
	[HarmonyPrefix]
	private static bool OnLoadPortraitSpriteAsync(ref AsyncOperationHandle<Sprite?> __result, string textureName, Il2CppSystem.Action<AsyncOperationHandle<Sprite?>> del)
	{
		if (!textureName.StartsWith(AssetUtils.EXTRA_TEXTURE_PREFIX)) return true;
		__result = AssetUtils.LoadPortrait(textureName, del);
		return false;
	}

	private static SkillPanelConfig GetSkillConfigForPanel(SkillPortraitPanel panel)
	{
		int index = -1;
		for (int i = 0; i < CharsheetView.Singleton.skillPortraitPanels.Length; i++)
			if (Il2CppSystem.Object.Equals(CharsheetView.Singleton.skillPortraitPanels[i], panel)) index = i;

		if (index == -1) throw new Exception($"expected to find a skill panel which matches {panel.skill}");
		else return DiscoRunner.globalConfig.skillPanels[index];
	}

	// :(
	private static Il2CppSystem.Reflection.MethodInfo SetSkillPortraitMethod = Il2CppType.Of<SkillPortraitPanel>()
		.GetMethod(nameof(SkillPortraitPanel.SetSkillPortrait), Il2CppSystem.Reflection.BindingFlags.NonPublic | Il2CppSystem.Reflection.BindingFlags.Instance);

	[HarmonyPatch(typeof(SkillPortraitPanel), nameof(SkillPortraitPanel.LoadSkillPortraitAsync))]
	[HarmonyPrefix]
	private static bool OnLoadSkillPortraitAsync(SkillPortraitPanel __instance)
	{
		if (__instance.isAsyncPrepared) return false;

		string? portrait = GetSkillConfigForPanel(__instance).portraitOverride;
		if (portrait == null) return true;

		var del = SetSkillPortraitMethod.CreateDelegate(Il2CppType.Of<Il2CppSystem.Action<AsyncOperationHandle<Sprite?>>>(), __instance);
		__instance.spriteHandle = AssetUtils.LoadPortrait(portrait, del.Cast<Il2CppSystem.Action<AsyncOperationHandle<Sprite?>>>());
		__instance.isAsyncPrepared = true;
		return false;
	}

	[HarmonyPatch(typeof(SkillPortraitPanel), nameof(SkillPortraitPanel.UnloadSkillPortraitAsync))]
	[HarmonyPrefix]
	private static bool OnUnloadSkillPortraitAsync(SkillPortraitPanel __instance)
	{
		// FIXME(investigate): **TEMPORARY** workaround for the fact that some portraits get very confused when unloading.
		return false;
	}

	[HarmonyPatch(typeof(CharacterSheetInfoPanel), nameof(CharacterSheetInfoPanel.ShowSkill))]
	[HarmonyPrefix]
	private static bool OnShowSkill(CharacterSheetInfoPanel __instance, SM.Skill skill)
	{
		if (skill.skillType == SM.SkillType.NONE)
		{
			__instance.tabPanel.SetActive(false);
			return false;
		}
		else return true;
	}

	[HarmonyPatch(typeof(SkillPortraitPanel), nameof(SkillPortraitPanel.OnSelectButtonClicked))]
	[HarmonyPatch(typeof(SkillPortraitPanel), nameof(SkillPortraitPanel.OnPointerEnter))]
	[HarmonyPatch(typeof(SkillPortraitPanel), nameof(SkillPortraitPanel.OnPointerExit))]
	[HarmonyPrefix]
	private static bool PreSkillPanelPointer(SkillPortraitPanel __instance)
	{
		__instance.portrait.color = Color.black;
		return (GetSkillConfigForPanel(__instance).flags & SkillPanelConfig.Flags.DisableSelection) == 0;
	}

	[HarmonyPatch(typeof(SkillPortraitSelection), nameof(SkillPortraitSelection.OnSelect))]
	[HarmonyPatch(typeof(SkillPortraitSelection), nameof(SkillPortraitSelection.OnDeselect))]
	[HarmonyPrefix]
	private static bool PreSkillPanelSelect(SkillPortraitSelection __instance)
		=> (GetSkillConfigForPanel(__instance.skillPortraitPanel).flags & SkillPanelConfig.Flags.DisableSelection) == 0;

	[HarmonyPatch(typeof(SkillPortraitPanel), nameof(SkillPortraitPanel.UpdateData))]
	[HarmonyPrefix]
	private static bool OnUpdateData(SkillPortraitPanel __instance)
	{
		var cfg = GetSkillConfigForPanel(__instance);
		if ((cfg.flags & SkillPanelConfig.Flags.HideOverlay) != 0)
		{
			__instance.isHovered = false;
			__instance.isSelectHovered = false;
			__instance.skillPortrayLabel.skillNumber.enabled = false;
			__instance.UpdateSelectionVisuals();
			return false;
		}
		else return cfg.skill != null;
	}

	[HarmonyPatch(typeof(Charsheet.SkillPortrayConfigurator), nameof(Charsheet.SkillPortrayConfigurator.UpdateSkillPortraitPanelsContent))]
	[HarmonyPrefix]
	private static bool OnSkillConfiguratorUpdateSkillPortraitPanelsContent(Charsheet.SkillPortrayConfigurator __instance)
	{
		var skills = (SM.SkillType[])System.Enum.GetValues(typeof(SM.SkillType));
		for (int i = 0; i < __instance.skillList.Length; i++)
		{
			var panel = __instance.skillList[i];
			SM.SkillType skillType = Array.Find(skills, skill => skill.ToString() == panel.name);
			if (panel == null || SkillUtils.IsExcludedFromPortraits(skillType)) continue;

			var cfg = DiscoRunner.globalConfig.skillPanels[i];
			SM.Skill skill = new(cfg.skill == null ? SM.SkillType.NONE : SkillUtils.Skills.GetRaw(cfg.skill.ResolveId()), null);
			panel.SetSkill(skill);
		}


		if (CharsheetView.Singleton != null)
		{
			CharsheetView.Singleton.UpdateSkillPortraitPanels(__instance.skillList);
		}

		return false;
	}

	[HarmonyPatch(typeof(Charsheet.SkillPortraitLabelsConfigurator), "OnLanguageChanged")]
	[HarmonyPrefix]
	private static bool OnLabelConfiguratorLanguageChanged(Charsheet.SkillPortraitLabelsConfigurator __instance)
	{
		if (!__instance.languageToLabelsSettings.TryGetValue(LocalizationManager.GetCurrentLanguageCode(), out var preset))
		{
			preset = __instance.languageToLabelsSettings["en"];
		}
		for (int i = 0; i < __instance.portraitLabels.Length; i++)
		{
			var label = __instance.portraitLabels[i];

			Charsheet.SkillPortraitLabelSettings? settings = null;
			if (__instance.skillToPresetIndex.TryGetValue(label.SkillType, out var value2))
			{
				settings = preset.labelsSettings[value2];
			}
			else
			{
				var cfg = DiscoRunner.globalConfig.skillPanels[i];
				if (cfg.labelSettings != null)
				{
					settings = new()
					{
						fontSize = cfg.labelSettings.fontSize,
						labelOffset = cfg.labelSettings.labelOffset,
						lineSpace = -31.7f,
						nameplateSize = cfg.labelSettings.nameplateSize,
						labelText = cfg.labelSettings.labelText ?? cfg.skill?.Resolve()?.displayName ?? "",
						skill = label.SkillType,
					};
				}
				else if (cfg.skill == null)
				{
					// this is fine since its inert anyway.
					settings = new()
					{
						skill = SM.SkillType.NONE,
						labelText = "",
					};
				}
			}


			// if (settings != null)
			// {
			// 	DiscoRunner.Log.LogInfo($"{settings.fontSize}, {settings.labelOffset}, {settings.leftMargin}, {settings.lineSpace}, {settings.nameplateSize}, {settings.textOffset}, {settings.labelText}, {settings.skill}");
			// 	preset.Apply(label.SkillPortraitLabel, settings);
			// }
			// else UnityEngine.Debug.LogError($"Charsheet skill portrait label for {label.SkillType} is missing settings in preset");
		}

		return false;
	}

	[HarmonyPatch(typeof(SV.ViewController), nameof(SV.ViewController.SwitchToView))]
	[HarmonyPrefix]
	private static void OnSwitchToViewController(SV.ViewController __instance, SV.View view, SV.VIEW_STACK_OPERATION stackOperation)
	{
		if (stackOperation == SV.VIEW_STACK_OPERATION.STACK_PREVIOUS && view.GetViewType() == SV.ViewType.OPTIONS)
		{
			View.ModConfigView.EnsureTabInstalled(view.transform);
		}
	}

	[HarmonyPatch(typeof(SettingsHeaderController), nameof(SettingsHeaderController.SelectControlsView))]
	[HarmonyPrefix]
	private static void OnSelectControlsView(SettingsHeaderController __instance)
	{
		View.ModConfigView.SetOptionsTab(__instance, View.ModConfigView.OptionsTab.Controls);
	}

	[HarmonyPatch(typeof(SettingsHeaderController), nameof(SettingsHeaderController.SelectSettingsView))]
	[HarmonyPrefix]
	private static void OnSelectSettingsView(SettingsHeaderController __instance)
	{
		View.ModConfigView.SetOptionsTab(__instance, View.ModConfigView.OptionsTab.Settings);
	}
}
