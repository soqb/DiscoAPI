using HarmonyLib;

namespace DiscoAPI.Runtime.Patches;

internal static class PersistencePatches
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

    [HarmonyPatch(typeof(SunshinePersistenceLoadDataManager), nameof(SunshinePersistenceLoadDataManager.LoadDataAfterLoadingArea))]
    [HarmonyPostfix] // this patch triggers after the basegame completely finishes loading the gameworld
    private static void OnLoadDataAfterLoadingArea()
    {
        DiscoRunner.Log.LogInfo("LoadCoR called !");
        DiscoRunner.saveSystem.TriggerLoadEvent(lastLoadFilename!);
    }
}
