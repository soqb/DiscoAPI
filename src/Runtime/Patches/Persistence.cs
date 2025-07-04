using HarmonyLib;

namespace DiscoAPI.Runtime.Patches;

public static class PersistencePatches
{
    [HarmonyPatch(typeof(SunshinePersistenceFileManager), nameof(SunshinePersistenceFileManager.PutDateSuffixOnPath))]
    [HarmonyPostfix] // alternative method patched due to inlined SaveCoR, triggers before basegame serialization
    private static void OnPutDateSuffixOnPath(string __result)
    {
        DiscoRunner.Log.LogInfo("SaveCoR called !");
        DiscoRunner.saveSystem.TriggerSaveEvent(__result);
    }

    private static string? lastLoadFilename;

    [HarmonyPatch(typeof(SunshinePersistence), nameof(SunshinePersistence.Load))]
    [HarmonyPrefix]
    private static void OnLoad(string fileName, bool isBundled)
    {
        lastLoadFilename = fileName;
    }

    [HarmonyPatch(typeof(SunshinePersistenceLoadDataManager), nameof(SunshinePersistenceLoadDataManager.ApplyLoadedDataFromMemory))]
    [HarmonyPostfix] // alternative method patched due ensure basegame data loaded
    private static void OnApplyLoadedDataFromMemory()
    {
        DiscoRunner.Log.LogInfo("LoadCoR called !");
        DiscoRunner.saveSystem.TriggerLoadEvent(lastLoadFilename!);
    }
}
