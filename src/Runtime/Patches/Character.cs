using HarmonyLib;
using SM = Sunshine.Metric;
using PC = PixelCrushers.DialogueSystem;
using DiscoAPI.Common.Dialogue;
using DiscoAPI.Common.Assets;
using Il2CppInterop.Runtime;

namespace DiscoAPI.Runtime.Patches;

public static class CharacterPatches
{
	private static IAssetArena<Skill> Skills => DiscoRunner.manager.Assets.skills;

	private static Actor ActorForSkill(SM.SkillType st)
	{
		return ((Actor)DiscoRunner.manager.Assets.Resolve(Skills[(int)st].actor));
	}

	private static string GetActorSkillName(SM.SkillType type)
	{
		if ((int)type <= Skill.VANILLA_MAX) return SM.Skill.actorSkillNames[(int)type];
		else return ActorForSkill(type).displayName;
	}

	private static string? GetSkillOrAbilityName(SM.Modifiable modifiable)
	{
		// excellent example of why il2cpp is a bit weird:
		if (modifiable.GetIl2CppType() == Il2CppType.Of<SM.Skill>())
			return GetActorSkillName(modifiable.Cast<SM.Skill>().skillType);
		else if (modifiable.GetIl2CppType() == Il2CppType.Of<SM.Ability>())
			return SM.Ability.GetActorAbilityName(modifiable.Cast<SM.Ability>().abilityType);
		else
			return null;
	}

	// the vanilla method does a static match against the recognised skilltypes so we need to change that:
	[HarmonyPatch(typeof(SM.Skill), nameof(SM.Skill.GetActorSkillName))]
	[HarmonyPrefix]
	private static bool OnGetActorSkillName(ref string __result, SM.SkillType skillType)
	{
		__result = GetActorSkillName(skillType);
		return false;
	}

	// we have to patch this method too
	// (even though vanilla calls `GetActorSkillName` which is patched above)
	// because sometimes it's inlined which causes problems.
	[HarmonyPatch(typeof(CharacterSheetTooltip), nameof(CharacterSheetTooltip.ActorFromModifiable))]
	[HarmonyPatch(new System.Type[] { typeof(SM.Modifiable) })]
	[HarmonyPrefix]
	private static bool OnActorFromMod(ref PC.Actor? __result, SM.Modifiable modifiable)
	{
		string? skillOrAbilityName = GetSkillOrAbilityName(modifiable);
		if (skillOrAbilityName == null)
		{
			__result = null;
		}
		else
		{
			__result = ArticyBridge.GetActor(skillOrAbilityName);
		}
		return false;
	}

	// [HarmonyPatch(typeof(LocalizationCustomSystem.LocalizationUtils), nameof(LocalizationCustomSystem.LocalizationUtils.GetActorLocalizedField))]
	// [HarmonyPatch(new System.Type[] { typeof(PC.Actor), typeof(string) })]
	// [HarmonyPrefix]
	// private static bool OnGetActorLocalizedField(ref string __result, PC.Actor actor, string fieldName)
	// {

	// 	string localizedTerm = LocalizationCustomSystem.LocalizationManager.GetLocalizedTerm(LocalizationCustomSystem.LocalizationUtils.GetActorLocalizationTerm(actor, fieldName));
	// 	if (Voidforge.SingletonComponent<LocalizationCustomSystem.LocalizationManager>.Singleton.DebugLogs && localizedTerm == null)
	// 	{
	// 		UnityEngine.Debug.Log("Missing localization for Actor: " + actor.Name + " field: " + fieldName);
	// 	}
	// 	__result = localizedTerm ?? LocalizationCustomSystem.LocalizationUtils.UpdateWrongName(actor.LookupValue(fieldName));
	// 	return false;
	// }

	[HarmonyPatch(typeof(LocalizationCustomSystem.LocalizationManager), nameof(LocalizationCustomSystem.LocalizationManager.GetLocalizedTermToUpper))]
	[HarmonyPrefix]
	private static bool OnGetLocalizedTermToUpper(ref string? __result, string term, bool removeNewLines)
	{
		string? text = I2.Loc.LocalizationManager.GetTermTranslation(term + "_UPPER");
		if (string.IsNullOrEmpty(text))
		{
			text = LocalizationCustomSystem.LocalizationManager.GetLocalizedTerm(term)?.ToUpper();
		}
		if (removeNewLines)
		{
			text = text?.Replace(TextUtils.NewLineString, string.Empty);
		}
		__result = text; return false;
	}

	[HarmonyPatch(typeof(SM.CharacterSheet), nameof(SM.CharacterSheet.GetSkill))]
	[HarmonyPrefix]
	private static bool OnGetSkill(ref SM.Skill __result, SM.CharacterSheet __instance, SM.SkillType type)
	{
		// DiscoAPIPlugin.Instance.Log.LogInfo($"getting skill value of {type}");
		if ((int)type <= Skill.VANILLA_MAX) return true;

		__result = CharacterSheet.GetForSM(__instance).skillMap[Skills[(int)type].Ref];
		return false;
	}

	// charsheet initialization resets all the skills so we need to do the same:
	[HarmonyPatch(typeof(SM.CharacterSheet), nameof(SM.CharacterSheet.Initialize))]
	[HarmonyPrefix]
	private static void OnInitialize(SM.CharacterSheet __instance, bool force)
	{
		if (__instance.intellect != null && !force) return;

		var sheet = CharacterSheet.GetForSM(__instance);
		for (int i = Skill.VANILLA_MAX + 1; i <= Skills.MaxId; i++)
			sheet.skillMap[Skills[i].Ref] = new((SM.SkillType)i, __instance);
	}

	// most methods don't use the skill fields, but instead a certain array so we update that when we need to:
	[HarmonyPatch(typeof(SM.CharacterSheet), nameof(SM.CharacterSheet.RepopulateLists))]
	[HarmonyPostfix]
	private static void OnRepopulateSheetLists(SM.CharacterSheet __instance)
	{
		DiscoAPIPlugin.Instance.Log.LogInfo($"repopulating sheet list");

		int targetCount = Skills.Count + Skill.VANILLA_SKILL_PORTRAIT_COUNT - Skill.VANILLA_SKILL_COUNT;
		SM.Skill[] ar = new SM.Skill[targetCount];
		__instance.skills.CopyTo(ar, 0);

		DiscoAPIPlugin.Instance.Log.LogInfo($" > skills has {__instance.skills.Count} but needs {targetCount}");

		var sheet = CharacterSheet.GetForSM(__instance);
		sheet.EnsureSkillsInstalled();

		for (int i = Skill.VANILLA_MAX + 1; i <= Skills.MaxId; i++)
		{
			int idx = i - Skill.VANILLA_MAX - 1 + Skill.VANILLA_SKILL_PORTRAIT_COUNT;
			ar[i - Skill.VANILLA_MAX - 1 + Skill.VANILLA_SKILL_PORTRAIT_COUNT] = sheet.skillMap[Skills[i].Ref];
		}

		__instance.skills = ar;

		// foreach (var sk in __instance.skills)
		// 	DiscoAPIPlugin.Instance.Log.LogInfo($"   * {sk.skillType}");
	}

	[HarmonyPatch(typeof(SM.Skill), nameof(SM.Skill.GetAbility))]
	[HarmonyPrefix]
	private static bool OnGetSkillAbility(ref SM.AbilityType __result, SM.SkillType skillType)
	{
		if ((int)skillType <= Skill.VANILLA_MAX) { return true; }

		__result = Skill.AbilityToSunshine(Skills[(int)skillType].ability);
		return false;
	}

	[HarmonyPatch(typeof(Charsheet.SkillPortraitLabelsConfigurator), "OnLanguageChanged")]
	[HarmonyPrefix]
	private static void OnLabelConfiguratorLanguageChanged(Charsheet.SkillPortraitLabelsConfigurator __instance)
	{
		DiscoAPIPlugin.Instance.Log.LogInfo($"configurator language changed...");
		for (int i = 0; i < __instance.labelsSettings.Length; i++)
		{
			var sco = __instance.labelsSettings[i].scriptable;
			var preset = sco.Cast<Charsheet.SkillPortraitLabelsPreset>();

			if (preset.labelsSettings.Length != Skill.VANILLA_SKILL_PORTRAIT_COUNT) continue;

			int settingCount = Skills.MaxId - Skill.VANILLA_MAX + Skill.VANILLA_SKILL_PORTRAIT_COUNT;
			Charsheet.SkillPortraitLabelSettings[] settings = new Charsheet.SkillPortraitLabelSettings[settingCount];
			preset.labelsSettings.CopyTo(settings, 0);

			for (int j = Skill.VANILLA_MAX + 1; j <= Skills.MaxId; j++)
			{
				SM.SkillType skill = (SM.SkillType)j;

				var old = settings[0]!;
				var now = new Charsheet.SkillPortraitLabelSettings()
				{
					fontSize = old.fontSize,
					labelOffset = old.labelOffset,
					leftMargin = old.leftMargin,
					lineSpace = old.lineSpace,
					nameplateSize = old.nameplateSize,
					textOffset = old.textOffset,
					labelText = Skills[j].displayName,
					skill = skill,
				};
				int computed = j - Skill.VANILLA_MAX - 1 + Skill.VANILLA_SKILL_PORTRAIT_COUNT;
				settings[computed] = now;
			}

			// DiscoAPIPlugin.Instance.Log.LogInfo($" > preset looks like:");
			// foreach (var se in preset.labelsSettings)
			// 	DiscoAPIPlugin.Instance.Log.LogInfo($"   * {se.labelText}");

			preset.labelsSettings = settings;
		}

		if (__instance.skillToPresetIndex.Count == Skills.Count - Skill.VANILLA_SKILL_COUNT + Skill.VANILLA_SKILL_PORTRAIT_COUNT)
			return;

		for (int j = Skill.VANILLA_MAX + 1; j <= Skills.MaxId; j++)
		{
			SM.SkillType skill = (SM.SkillType)j;
			int computed = j - Skill.VANILLA_MAX - 1 + Skill.VANILLA_SKILL_PORTRAIT_COUNT;
			__instance.skillToPresetIndex.Add(skill, computed);
		}
	}
}

