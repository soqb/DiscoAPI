using HarmonyLib;
using SM = Sunshine.Metric;
using PC = PixelCrushers.DialogueSystem;
using DiscoAPI.Common.Assets;
using UnityEngine;
using LocalizationCustomSystem;
using System;
using System.Text;
using DiscoAPI.Runtime.Components;
using DiscoAPI.Runtime.Utils;
using Il2CppSystem.Collections.Generic;

namespace DiscoAPI.Runtime.Patches;

internal static class CharacterPatches
{
	[HarmonyPatch(typeof(SM.CharacterSheet), nameof(SM.CharacterSheet.Recalc))]
	[HarmonyPostfix]
	private static void OnRecalc(SM.CharacterSheet __instance)
	{
		foreach ((_, object datum) in CharacterComponents.Of(__instance).ComponentData)
			if (datum is IRecalculable) ((IRecalculable)datum).Recalc();
	}

	// debugging function for skill value mismatch
	// [HarmonyPatch(typeof(SM.Modifiable), nameof(SM.Modifiable.Recalc))]
	// [HarmonyPostfix]
	// public static void OnSkillRecalc(SM.Modifiable __instance, SM.CharacterSheet ch)
	// {
	// 	PrintModifiable(__instance);
	// }

	public static void PrintModifiable(SM.Modifiable modifiable)
	{
		StringBuilder sb = new();
		var maybeSkill = modifiable.TryCast<SM.Skill>();
		var maybeAbility = modifiable.TryCast<SM.Ability>();
		if (maybeSkill != null)
		{
			var modSkill = SkillUtils.Lookup(maybeSkill.skillType);
			if (modSkill != null)
			{
				sb.Append($"Recalcing skill {modSkill.displayName}\n");
			}
			else
			{
				sb.Append($"Recalcing skill {maybeSkill.skillType.ToString()}\n");
			}
		}
		else if (maybeAbility != null)
		{
			sb.Append($"Recalcing ability {maybeAbility.abilityType.ToString()}\n");
		}

		if (modifiable.modifiers != null)
		{
			for (int i = 0; i < modifiable.modifiers.Count; i++)
			{
				var mod = modifiable.modifiers[i];
				sb.Append(
					$"    MODIFIER {i} |  AMOUNT:{mod.Amount} TYPE:{mod.type.ToString()} CAUSE:{mod.modifierCause?.GetDisplayName() ?? "UNKNOWN"}\n");
			}
		}

		sb.Append($"RECALC RESULTS -> CalculatedAbility:{modifiable.calculatedAbility} Value:{modifiable.value} MaxValue:{modifiable.maximumValue} Modifiers:{modifiable.modifiers?.Count}\n\n");
		DiscoRunner.Log.LogInfo(sb.ToString());
	}

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
	private static bool OnGetSkill(ref SM.Skill? __result, SM.CharacterSheet __instance, SM.SkillType type)
	{
		// DiscoAPIPlugin.Instance.Log.LogInfo($"getting skill value of {type}");
		if ((int)type <= Skill.VANILLA_MAX) return true;

		__result = CharacterComponents.Skills.Of(__instance)!.GetRawSkill(SkillUtils.Lookup(type)!.Location);
		return false;
	}

	// charsheet initialization resets all the skills so we need to do the same:
	[HarmonyPatch(typeof(SM.CharacterSheet), nameof(SM.CharacterSheet.Initialize))]
	[HarmonyPrefix]
	private static void OnInitialize(SM.CharacterSheet __instance, bool force)
	{
		var sheet = CharacterComponents.Of(__instance);
		if (__instance.intellect != null && !force && sheet.Contains(CharacterComponents.Skills)) return;

		sheet.GetOrCreate(CharacterComponents.Skills, sh => new()).ReinitializeFromNativeInstance(__instance);
	}

	// most methods don't use the skill fields, but instead a certain array so we update that when we need to:
	[HarmonyPatch(typeof(SM.CharacterSheet), nameof(SM.CharacterSheet.RepopulateLists))]
	[HarmonyPostfix]
	private static void OnRepopulateSheetLists(SM.CharacterSheet __instance)
	{
		CharacterComponents.Skills.Of(__instance)!.RepopulateNativeInstanceLists(__instance);
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

	[HarmonyPatch(typeof(SM.CharacterSheet), nameof(SM.CharacterSheet.MakeSkills))]
	[HarmonyPrefix]
	private static bool OnMakeSkills(SM.CharacterSheet __instance)
	{
		// nb: ensures mod skills have their modifiers reset since their lifetimes are managed separately
		for (var i = 0; i < __instance.skills.Count; i++)
		{
			var smSkill = __instance.skills[i];
			if (!SkillUtils.SkillIsVanilla(smSkill.skillType))
			{
				smSkill.modifiers = new List<SM.Modifier>();
			}
			var newMod = new SM.Modifier(SM.ModifierType.CALCULATED_ABILITY, 0, null, __instance.GetAbility(smSkill.abilityType).Cast<IModifierCause>(), smSkill.skillType);
			smSkill.modifiers.Add(newMod);
		}
		return false;
	}

}

