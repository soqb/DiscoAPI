using DiscoAPI.Runtime.SaveSystem;
using HarmonyLib;
using Il2CppSystem;

namespace DiscoAPI.Runtime.Patches;

public static class PersistencePatches
{
    [HarmonyPatch(nameof(SunshinePersistence), nameof(SunshinePersistence.SaveCoR))]
    [HarmonyPostfix]
    private static void OnSaveCoR(ref string fileNamePrefix, SunshinePersistence.SAVE_MODE saveMode, Action onSaveComplete,
        string replacedFileNamePrefix)
    {
        DiscoRunner.saveSystem ??= new ModSaveSystem();
        string truePath = SunshinePersistenceFileManager.PutDateSuffixOnPath(fileNamePrefix, out _);
        DiscoRunner.saveGame.Invoke(truePath);
    }

    [HarmonyPatch(nameof(SunshinePersistence), nameof(SunshinePersistence.LoadCoR))]
    [HarmonyPostfix]
    private static void OnLoadCoR(ref string fileNamePrefix, bool isBundled)
    {
        DiscoRunner.saveSystem ??= new ModSaveSystem();
        string truePath = SunshinePersistenceFileManager.PutDateSuffixOnPath(fileNamePrefix, out _);
        DiscoRunner.loadSavedGame.Invoke(truePath);
    }
}