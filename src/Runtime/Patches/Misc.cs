using System;
using HarmonyLib;

namespace DiscoAPI.Runtime.Patches;

public static class MiscPatches
{
	// its good etiquette to not let people cheese too easily:
	[HarmonyPatch(typeof(Achievements), nameof(Achievements.Set), new Type[] { typeof(string) })]
	[HarmonyPatch(typeof(Achievements), nameof(Achievements.SetStat), new Type[] { typeof(string), typeof(float) })]
	[HarmonyPatch(typeof(Achievements), nameof(Achievements.SetStat), new Type[] { typeof(string), typeof(int) })]
	[HarmonyPatch(typeof(Achievements), nameof(Achievements.ResetAllStats), new Type[] { typeof(bool) })]
	[HarmonyPrefix]
	public static bool CancelAchievementsSet() => DiscoAPISettings.AllowAchievements;
}
