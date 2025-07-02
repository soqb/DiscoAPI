using HarmonyLib;

namespace DiscoAPI.Runtime.Patches;

public static class PersistencePatches
{
    [HarmonyPatch(typeof(SunshinePersistenceFileManager), nameof(SunshinePersistenceFileManager.PutDateSuffixOnPath))]
    [HarmonyPostfix] // alternative method patched due to inlined SaveCoR
    private static void OnPutDateSuffixOnPath(string __result)
    {
        DiscoRunner.Log.LogInfo("SaveCoR called !");
        DiscoRunner.saveSystem.TriggerSaveEvent(__result);
    }

    [HarmonyPatch(typeof(SunshinePersistence), nameof(SunshinePersistence.Load))]
    [HarmonyPostfix] // alternative method patched due to inlined LoadCoR
    private static void OnLoadCoR(string fileName, bool isBundled)
    {
        DiscoRunner.Log.LogInfo("LoadCoR called !");
        DiscoRunner.saveSystem.TriggerLoadEvent(fileName);
    }
}