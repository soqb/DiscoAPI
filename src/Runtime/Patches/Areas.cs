using FortressOccident;
using HarmonyLib;

namespace DiscoAPI.Runtime.Patches;

public static class AreaPatches
{
    [HarmonyPatch(typeof(ApplicationManager), nameof(ApplicationManager.ChangeArea))]
    [HarmonyPrefix]
    private static void OnChangeArea(string areaId, string destinationId, bool isGameLoad)
    {
        
    }
}