using System;
using HarmonyLib;

namespace DiscoAPI.Runtime.Patches;

public static class MiscPatches
{
	// its good etiquette to not let people cheese too easily:
	[HarmonyPatch(typeof(Achievements), nameof(Achievements.Set), typeof(string))]
	[HarmonyPatch(typeof(Achievements), nameof(Achievements.SetStat), typeof(string), typeof(float))]
	[HarmonyPatch(typeof(Achievements), nameof(Achievements.SetStat), typeof(string), typeof(int))]
	[HarmonyPatch(typeof(Achievements), nameof(Achievements.ResetAllStats), typeof(bool))]
	[HarmonyPrefix]
	public static bool CancelSetAchievements() => DiscoAPISettings.AllowAchievements;

}
