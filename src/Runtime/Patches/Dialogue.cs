using System;
using System.IO;
using System.Linq;
using HarmonyLib;
using Il2CppInterop.Runtime;
using PC = PixelCrushers.DialogueSystem;

namespace DiscoAPI.Runtime.Patches;

internal static class DialoguePatches
{
    // DE uses the "Articy Id" for asset referencing (probably because PixelCrushers' asset identification systems are *awful*).
    // Since these are strings, we can trivially spoof out own "Articy Id" in whatever format we like.
    // We have to maintain our own cache for this, though.
    [HarmonyPatch(typeof(ArticyBridge), nameof(ArticyBridge.GetAssetByArticyId))]
    [HarmonyPrefix]
    private static bool GetAssetByArticyId(string articyKey, ref PC.Asset __result)
    {
        ArticyBridge.InitializeArticyIdToAsset();
        if (DiscoRunner.manager.Dialogue.fakeArticyIDToAssetCache.TryGetValue(articyKey, out var value))
        {
            __result = value;
        }
        else
        {
            __result = ArticyBridge.articyIdToAssetCache[articyKey];
        }
        return false;
    }

    // This is a hack while voiceovers aren't implemented.
    // We force the method to return null instead of throwing an exception for missing clips.
    [HarmonyPatch(typeof(VOTool.VoiceOverClipsPlayer), nameof(VOTool.VoiceOverClipsPlayer.GetClipToPlay))]
    [HarmonyPatch(typeof(VOTool.VoiceOverClipsLibrary), nameof(VOTool.VoiceOverClipsLibrary.GetClipToPlay))]
    [HarmonyPrefix]
    private static bool GetClipToPlay(string? articyID, ref VOTool.VoiceClipInformation? __result)
    {

        if (articyID != null && VOTool.VoiceOverClipsPlayer.Singleton.clipDictonary.TryGetValue(articyID, out var value))
        {
            __result = value;
        }
        else
        {
            __result = null;
        }
        return false;
    }

    // This hack is by far the wierdest of all.
    // Without it, mod-defined skillchecks will cause the ui to bug out,
    // softlocking on future skillchecks and when leaving the dialogue menu.
    //
    // BEHOLD!
    [HarmonyPatch(typeof(LogRenderer), nameof(LogRenderer.DelayedAdd))]
    [HarmonyPostfix]
    private static void AllYourDialogueAreBelongToUs(FinalEntry entry)
    {
    }
    
    [HarmonyPatch(typeof(PC.Sequencer), nameof(PC.Sequencer.GetTypeFromName))]
    [HarmonyPostfix] // enables injection of modded sequence commands
    public static void GetSequencerType(string typeName, ref Il2CppSystem.Type? __result)
    {
        if (__result != null) return;
        var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic && a.Location.Contains("BepInEx" + Path.DirectorySeparatorChar + "plugins"));
        foreach (var asm in assemblies)
        {
            try
            {
                var types = asm.GetTypes();
                for (int j = 0; j < types.Length; j++)
                {
                    var type = types[j];
                    if (string.Equals(type.Name, typeName)) __result = Il2CppType.From(type);
                }
            }
            catch (Exception e)
            {
                DiscoRunner.Log.LogError("Error occured while looking for a sequencer command type: " + typeName);
                DiscoRunner.Log.LogError(e);
            }
        }
    }
}
