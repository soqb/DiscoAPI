using System;
using DiscoAPI.Runtime.Assets;
using HarmonyLib;

namespace DiscoAPI.Runtime.Patches;

internal static class MiscPatches
{
	// its good etiquette to not let people cheese too easily.
	// we're definitely double-counting some functions but inlining screws us in some places:
	[HarmonyPatch(typeof(Achievements), nameof(Achievements.Set), typeof(string))]
	[HarmonyPatch(typeof(Achievements), nameof(Achievements.SetStat), typeof(string), typeof(float))]
	[HarmonyPatch(typeof(Achievements), nameof(Achievements.SetStat), typeof(string), typeof(int))]
	[HarmonyPatch(typeof(Achievements), nameof(Achievements.Clear), typeof(string))]
	[HarmonyPatch(typeof(Achievements), nameof(Achievements.ResetAllStats), typeof(bool))]
	[HarmonyPatch(typeof(SteamAchievements), nameof(SteamAchievements.Set), typeof(string))]
	[HarmonyPatch(typeof(SteamAchievements), nameof(SteamAchievements.SetStat), typeof(string), typeof(float))]
	[HarmonyPatch(typeof(SteamAchievements), nameof(SteamAchievements.SetStat), typeof(string), typeof(int))]
	[HarmonyPatch(typeof(SteamAchievements), nameof(SteamAchievements.Clear), typeof(string))]
	[HarmonyPatch(typeof(SteamAchievements), nameof(SteamAchievements.ResetAllStats), typeof(bool))]
	[HarmonyPrefix]
	private static bool CancelSetAchievements() => DiscoAPISettings.AllowAchievements;

	[HarmonyPatch(typeof(Il2CppSystem.Enum), nameof(Il2CppSystem.Enum.GetName))]
	[HarmonyPrefix]
	private static bool OnEnumGetName(ref string? __result, Il2CppSystem.Type? enumType, Il2CppSystem.Object value)
	{
		if (enumType == null) return true;
		else if (EnumArena.GlobalEnumOverrideNames.TryGetValue((enumType, Il2CppSystem.Convert.ToInt64(value)), out __result)) return false;
		else return true;
	}

	[HarmonyPatch(
		typeof(Il2CppSystem.Enum), nameof(Il2CppSystem.Enum.Parse),
		new Type[] { typeof(Il2CppSystem.Type), typeof(string), typeof(bool) }
	)]
	[HarmonyPrefix]
	private static bool OnEnumParse(ref object __result, Il2CppSystem.Type enumType, string value, bool ignoreCase)
	{
		if (enumType == null) return true;

		var map = ignoreCase ? EnumArena.GlobalEnumOverrideValuesLowercase : EnumArena.GlobalEnumOverrideValues;
		if (ignoreCase) value = value.ToLowerInvariant();

		if (map.TryGetValue((enumType, value), out long val))
		{
			__result = Il2CppSystem.Enum.ToObject(enumType, val);
			return false;
		}
		else return true;
	}
}
