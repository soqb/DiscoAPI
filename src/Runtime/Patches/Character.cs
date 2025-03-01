using HarmonyLib;
using SM = Sunshine.Metric;
using SD = Sunshine.Dialogue;
using PC = PixelCrushers.DialogueSystem;
using DiscoAPI.Common.Assets;
using DiscoAPI.Runtime.Assets;
using UnityEngine;
using LocalizationCustomSystem;
using System;

namespace DiscoAPI.Runtime.Patches;

public static class CharacterPatches
{
	[HarmonyPatch(typeof(ThoughtAlterant), nameof(ThoughtAlterant.PassiveSuccess))]
	[HarmonyPrefix]
	private static bool OnPassiveSuccess(ref bool __result, PC.DialogueEntry entry)
	{
		if (DiscoAPISettings.AllChecksPass)
		{
			__result = !PassiveNode.IsAntiPassiveNode(entry);
			return false;
		}

		return true;
	}

	[HarmonyPatch(typeof(SM.SunshineRoller), nameof(SM.SunshineRoller.RollOne))]
	[HarmonyPrefix]
	private static bool OnRollOneDie(ref int __result)
	{
		if (DiscoAPISettings.AllChecksPass)
		{
			__result = SM.SunshineRoller.NextRollBonus + 6;
			return false;
		}

		return true;
	}

	[HarmonyPatch(typeof(EnddayHealing), nameof(EnddayHealing.VolitionHealAmount))]
	[HarmonyPrefix]
	private static bool OnEnddayVolitionHealAmount(ref bool __result)
	{
		__result = DiscoRunner.world!.you.MoraleRaw.damageValue < 0.0;
		return false;
	}

	[HarmonyPatch(typeof(EnddayHealing), nameof(EnddayHealing.EnduranceHealAmount))]
	[HarmonyPrefix]
	private static bool OnEnddayEnduranceHealAmount(ref bool __result)
	{
		__result = DiscoRunner.world!.you.HealthRaw.damageValue < 0.0;
		return false;
	}

	[HarmonyPatch(typeof(CharacterManipulations), nameof(CharacterManipulations.DamageVolition))]
	[HarmonyPrefix]
	private static bool OnDamageVolition(int amount)
	{
		Sunshine.EndgameManager.NewspaperToShow = null;
		if (amount > 0)
		{
			CharacterSheet you = DiscoRunner.world!.you;
			you.MoraleRaw.DamageValue(amount);
			you.Recalc();
			CharacterManipulations.PlayVolitionDamageVisual();
			HudController.Singleton.RefreshAll();
			NotificationSystem.NotificationManager.Singleton.ShowNotification(NotificationSystem.NotificationType.DamagedMorale, (-amount).ToString());
		}
		return false;
	}

	[HarmonyPatch(typeof(CharacterManipulations), nameof(CharacterManipulations.HealVolition))]
	[HarmonyPrefix]
	private static bool OnHealVolition(int amount)
	{
		if (amount > 0)
		{
			CharacterSheet you = DiscoRunner.world!.you;
			you.MoraleRaw.HealValue(Mathf.Min(amount, you.MoraleRaw.maximumValue - you.MoraleRaw.value));
			you.Recalc();
			HudController.Singleton.RefreshAll();
			NotificationSystem.NotificationManager.Singleton.ShowNotification(NotificationSystem.NotificationType.HealedMorale, $"+{amount}");
		}
		return false;
	}

	[HarmonyPatch(typeof(CharacterManipulations), nameof(CharacterManipulations.DamageEndurance))]
	[HarmonyPrefix]
	private static bool OnDamageEndurance(int amount)
	{
		Sunshine.EndgameManager.NewspaperToShow = null;
		if (amount > 0)
		{
			CharacterSheet you = DiscoRunner.world!.you;
			you.MoraleRaw.DamageValue(amount);
			you.Recalc();
			CharacterManipulations.PlayVolitionDamageVisual();
			HudController.Singleton.RefreshAll();
			NotificationSystem.NotificationManager.Singleton.ShowNotification(NotificationSystem.NotificationType.DamagedHealth, (-amount).ToString());
		}
		return false;
	}

	[HarmonyPatch(typeof(CharacterManipulations), nameof(CharacterManipulations.HealEndurance))]
	[HarmonyPrefix]
	private static bool OnHealEndurance(int amount)
	{
		if (amount > 0)
		{
			CharacterSheet you = DiscoRunner.world!.you;
			you.MoraleRaw.HealValue(Mathf.Min(amount, you.HealthRaw.maximumValue - you.MoraleRaw.value));
			you.Recalc();
			HudController.Singleton.RefreshAll();
			NotificationSystem.NotificationManager.Singleton.ShowNotification(NotificationSystem.NotificationType.HealedHealth, $"+{amount}");
		}
		return false;
	}

	[HarmonyPatch(typeof(SD.CharacterLuaFunctions), nameof(SD.CharacterLuaFunctions.CurrentVolition))]
	[HarmonyPrefix]
	private static bool OnCurrentVolition(ref double __result)
	{
		__result = DiscoRunner.world!.you.MoraleRaw.value;
		return false;
	}

	[HarmonyPatch(typeof(SD.CharacterLuaFunctions), nameof(SD.CharacterLuaFunctions.HasVolitionDamage))]
	[HarmonyPrefix]
	private static bool OnHasVolitionDamage(ref bool __result)
	{
		__result = DiscoRunner.world!.you.MoraleRaw.damageValue < 0.0;
		return false;
	}

	[HarmonyPatch(typeof(SD.CharacterLuaFunctions), nameof(SD.CharacterLuaFunctions.HealAllVolition))]
	[HarmonyPrefix]
	private static bool OnHealAllVolition()
	{
		CharacterManipulations.HealVolition(-DiscoRunner.world!.you.MoraleRaw.damageValue);
		return false;
	}

	[HarmonyPatch(typeof(SD.CharacterLuaFunctions), nameof(SD.CharacterLuaFunctions.CurrentEndurance))]
	[HarmonyPrefix]
	private static bool OnCurrentEndurance(ref double __result)
	{
		__result = DiscoRunner.world!.you.HealthRaw.value;
		return false;
	}

	[HarmonyPatch(typeof(SD.CharacterLuaFunctions), nameof(SD.CharacterLuaFunctions.HasEnduranceDamage))]
	[HarmonyPrefix]
	private static bool OnHasEnduranceDamage(ref bool __result)
	{
		__result = DiscoRunner.world!.you.HealthRaw.damageValue < 0.0;
		return false;
	}

	[HarmonyPatch(typeof(SD.CharacterLuaFunctions), nameof(SD.CharacterLuaFunctions.HealAllEndurance))]
	[HarmonyPrefix]
	private static bool OnHealAllEndurance()
	{
		CharacterManipulations.HealEndurance(-DiscoRunner.world!.you.HealthRaw.damageValue);
		return false;
	}

	private static EnumArena<SM.SkillType, Skill> Skills => SkillUtils.Skills;
	// the vanilla method does a static match against the recognised skilltypes so we need to change that:
	[HarmonyPatch(typeof(SM.Skill), nameof(SM.Skill.GetActorSkillName))]
	[HarmonyPrefix]
	private static bool OnGetActorSkillName(ref string __result, SM.SkillType skillType)
	{
		__result = SkillUtils.GetActorSkillName(skillType)!;
		return false;
	}

	// we have to patch this method too
	// (even though vanilla calls `GetActorSkillName` which is patched above)
	// because sometimes it's inlined which causes problems.
	[HarmonyPatch(typeof(CharacterSheetTooltip), nameof(CharacterSheetTooltip.ActorFromModifiable))]
	[HarmonyPatch(new System.Type[] { typeof(SM.Modifiable) })]
	[HarmonyPrefix]
	private static bool OnActorFromModifiable(ref PC.Actor? __result, SM.Modifiable modifiable)
	{
		string? skillOrAbilityName = SkillUtils.GetSkillOrAbilityName(modifiable);
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
	[HarmonyPatch(typeof(I2.Loc.LocalizationManager), nameof(I2.Loc.LocalizationManager.GetTermTranslation))]
	[HarmonyPrefix]
	private static bool OnGetTermTranslation(ref string? __result, string? Term)
	{
		if (Term == null || !Term.StartsWith("\0RAW\0")) return true;
		Term = Term.Substring(5);
		__result = Term;
		return false;

	}
	[HarmonyPatch(typeof(I2.Loc.I2Utils), nameof(I2.Loc.I2Utils.GetValidTermName))]
	[HarmonyPrefix]
	private static bool OnGetValidTermName(ref string? __result, string? text)
	{
		if (text == null || !text.StartsWith("\0RAW\0")) return true;
		__result = text;
		return false;

	}

	[HarmonyPatch(typeof(LocalizationManager), nameof(LocalizationManager.GetLocalizedTermToUpper))]
	[HarmonyPrefix]
	private static bool OnGetLocalizedTermToUpper(ref string? __result, string term, bool removeNewLines)
	{
		string? text = I2.Loc.LocalizationManager.GetTermTranslation(term + "_UPPER");
		if (string.IsNullOrEmpty(text))
		{
			text = LocalizationManager.GetLocalizedTerm(term)?.ToUpper();
		}
		if (removeNewLines)
		{
			text = text?.Replace(TextUtils.NewLineString, string.Empty);
		}
		__result = text;
		return false;
	}

	[HarmonyPatch(typeof(LocalizationUtils), nameof(LocalizationUtils.GetActorLocalizedFieldToUpper))]
	[HarmonyPrefix]
	private static bool OnGetActorLocalizedFieldToUpper(ref string? __result, PC.Actor actor, string fieldName)
	{
		// i have no idea why the vanilla method fails sometimes.. this is not a complicated method.
		string localized = LocalizationManager.GetLocalizedTermToUpper(LocalizationCustomSystem.LocalizationUtils.GetActorLocalizationTerm(actor, fieldName));
		if (localized != null) __result = localized;
		else
		{
			string value = actor.LookupValue(fieldName);
			if (value == null) __result = null;
			else __result = LocalizationUtils.UpdateWrongName(value.ToUpper());
		}

		return false;
	}

	private static string? GetActorLocalizedField(PC.Actor actor, string field)
	{
		string localized = LocalizationManager.GetLocalizedTerm(LocalizationCustomSystem.LocalizationUtils.GetActorLocalizationTerm(actor, field));
		if (localized != null) return localized;
		else
		{
			string value = actor!.LookupValue(field);
			if (value == null) return null;
			else return LocalizationUtils.UpdateWrongName(value);
		}
	}

	[HarmonyPatch(typeof(LocalizationUtils), nameof(LocalizationUtils.GetActorLocalizedField), new Type[] { typeof(PC.Actor), typeof(string) })]
	[HarmonyPrefix]
	private static bool OnGetActorLocalizedField(ref string? __result, PC.Actor actor, string fieldName)
	{
		// i have no idea why the vanilla method fails sometimes.. this is not a complicated method.
		__result = GetActorLocalizedField(actor, fieldName);
		return false;
	}

	[HarmonyPatch(typeof(LocalizationUtils), nameof(LocalizationUtils.GetActorLocalizedField), new Type[] { typeof(string), typeof(string) })]
	[HarmonyPrefix]
	private static bool OnGetActorLocalizedField(ref string? __result, string name, string fieldName)
	{
		var actor = DialogueBridgePixelCrushers.DialogueSystem.MasterDatabase.GetActor(name);
		if (actor == null)
		{
			UnityEngine.Debug.Log("Actor: " + name + " doesn't extists in the database!");
			__result = null;
		}
		else __result = GetActorLocalizedField(actor, fieldName);

		return false;
	}

	[HarmonyPatch(typeof(SM.Skill), nameof(SM.Skill.SkillTypeToLocalizedName))]
	[HarmonyPostfix]
	private static void OnSkillTypeToLocalizedName(ref string? __result, SM.SkillType type)
	{
		if (__result == null) __result = SkillUtils.Lookup(type)?.displayName;
	}

	[HarmonyPatch(typeof(SM.Skill), nameof(SM.Skill.SkillTypeToLocalizedNameToUpper))]
	[HarmonyPostfix]
	private static void OnSkillTypeToLocalizedNameToUpper(ref string? __result, SM.SkillType type)
	{
		if (__result == null) __result = SkillUtils.Lookup(type)?.displayName.ToUpper();
	}

	[HarmonyPatch(typeof(SM.CharacterSheet), nameof(SM.CharacterSheet.GetSkill))]
	[HarmonyPrefix]
	private static bool OnGetSkill(ref SM.Skill __result, SM.CharacterSheet __instance, SM.SkillType type)
	{
		// DiscoAPIPlugin.Instance.Log.LogInfo($"getting skill value of {type}");
		if ((int)type <= Skill.VANILLA_MAX) return true;

		__result = CharacterSheet.GetForSM(__instance).skillMap[SkillUtils.Lookup(type)!.Location];
		return false;
	}

	// charsheet initialization resets all the skills so we need to do the same:
	[HarmonyPatch(typeof(SM.CharacterSheet), nameof(SM.CharacterSheet.Initialize))]
	[HarmonyPrefix]
	private static void OnInitialize(SM.CharacterSheet __instance, bool force)
	{
		if (__instance.intellect != null && !force) return;

		var sheet = CharacterSheet.GetForSM(__instance);
		for (int i = Skills.baseCount; i < Skills.Count; i++)
			sheet.skillMap[Skills[i]!.Location] = new(Skills.GetRaw(i), __instance);
	}

	// most methods don't use the skill fields, but instead a certain array so we update that when we need to:
	[HarmonyPatch(typeof(SM.CharacterSheet), nameof(SM.CharacterSheet.RepopulateLists))]
	[HarmonyPostfix]
	private static void OnRepopulateSheetLists(SM.CharacterSheet __instance)
	{
		int targetCount = Skills.Count + Skill.VANILLA_SKILL_PORTRAIT_COUNT - Skill.VANILLA_SKILL_COUNT;
		SM.Skill[] ar = new SM.Skill[targetCount];
		__instance.skills.CopyTo(ar, 0);

		var sheet = CharacterSheet.GetForSM(__instance);
		sheet.EnsureSkillsInstalled();

		for (int i = 0; i < Skills.Count - Skills.baseCount; i++)
		{
			var loc = Skills[i + Skills.baseCount]!.Location;
			ar[i + Skill.VANILLA_SKILL_PORTRAIT_COUNT] = sheet.skillMap[loc];
		}

		__instance.skills = ar;

		// foreach (var sk in __instance.skills)
		// 	DiscoAPIPlugin.Instance.Log.LogInfo($"   * {sk.skillType}");
	}

	// these methods reduce efficiency slightly but who care atp.
	[HarmonyPatch(typeof(SM.Skill), nameof(SM.Skill.GetColor))]
	[HarmonyPrefix]
	private static bool OnGetColor(ref Color __result, SM.SkillType skillType)
	{
		if ((int)skillType <= Skill.VANILLA_MAX) return true;

		__result = SkillUtils.Lookup(skillType)!.ability switch
		{
			AbilityType.Int => ColorConfiguration.Singleton.ColorINT,
			AbilityType.Psy => ColorConfiguration.Singleton.ColorPSY,
			AbilityType.Fys => ColorConfiguration.Singleton.ColorFYS,
			AbilityType.Mot => ColorConfiguration.Singleton.ColorMOT,
			_ => Color.magenta,
		};
		return false;
	}

	[HarmonyPatch(typeof(SM.Skill), nameof(SM.Skill.GetAbility))]
	[HarmonyPrefix]
	private static bool OnGetSkillAbility(ref SM.AbilityType __result, SM.SkillType skillType)
	{
		if ((int)skillType <= Skill.VANILLA_MAX) return true;

		__result = SkillUtils.AbilityToSunshine(SkillUtils.Lookup(skillType)!.ability);
		return false;
	}
}

