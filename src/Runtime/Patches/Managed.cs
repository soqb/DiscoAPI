using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace DiscoAPI.Runtime.Patches;

public static class ManagedPatches
{
	private static ManualLogSource mockUnityLogger = BepInEx.Logging.Logger.CreateLogSource("Unity");

	// This patch makes debugging much friendlier since BepInEx chooses to ignoer stacktraces...
	[HarmonyPatch(typeof(BepInEx.Unity.IL2CPP.Logging.IL2CPPUnityLogSource), nameof(BepInEx.Unity.IL2CPP.Logging.IL2CPPUnityLogSource.UnityLogCallback))]
	[HarmonyPostfix]
	private static void UnityErrorStacktrace(string exception, LogType type)
	{
		switch (type)
		{
			case LogType.Error:
			case LogType.Exception:
			case LogType.Assert:
				mockUnityLogger.LogError(exception);
				break;
			default:
				break;
		}
	}
}
