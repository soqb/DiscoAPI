using System.IO;
using System.Linq;
using DiscoAPI.Runtime.SaveSystem;
using HarmonyLib;
using Il2CppSystem;

namespace DiscoAPI.Runtime.Patches;

public static class PersistencePatches
{
    [HarmonyPatch(nameof(SunshinePersistence), nameof(SunshinePersistence.SaveCoR))]
    [HarmonyPostfix]
    private static void OnSaveCoR(string fileNamePrefix, SunshinePersistence.SAVE_MODE saveMode, Action onSaveComplete,
        string replacedFileNamePrefix)
    {
        DiscoRunner.Log.LogInfo("SaveCoR called !");
        string truePath = FindLastSavefileName();
        DiscoRunner.saveSystem.TriggerSaveEvent(truePath);
    }

    [HarmonyPatch(nameof(SunshinePersistence), nameof(SunshinePersistence.Load))] // workaround for inlined LoadCoR
    [HarmonyPostfix]
    private static void OnLoadCoR(string fileName, bool isBundled)
    {
        DiscoRunner.Log.LogInfo("LoadCoR called !");
        DiscoRunner.saveSystem.TriggerLoadEvent(fileName);
    }

    // works around a bug where the realtime savefile timestamp != patch trigger time
    private static string FindLastSavefileName()
    {
        var saveDir = new DirectoryInfo(SunshinePersistenceFileManager.GetSaveGameDirectoryPath());
        return saveDir.EnumerateFiles("*.jpg").OrderByDescending(f => f.LastWriteTime).First().Name[..^4];
    }
}