using HarmonyLib;
using PC = PixelCrushers.DialogueSystem;

namespace DiscoAPI.Patches;

public class DialoguePatches
{
    // DE uses the "Articy Id" for asset referencing (probably because PixelCrushers' asset identification systems are *awful*).
    // Since these are strings, we can trivially spoof out own "Articy Id" in whatever format we like.
    // We have to maintain our own cache for this, though.
    [HarmonyPatch(typeof(ArticyBridge), nameof(ArticyBridge.GetAssetByArticyId))]
    [HarmonyPrefix]
    private static bool GetAssetByArticyId(string articyKey, ref PC.Asset __result)
    {
        ArticyBridge.InitializeArticyIdToAsset();
        if (DiscoRunner.manager.dialogue.fakeArticyIDToAssetCache.TryGetValue(articyKey, out var value))
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
    // I don't know what LogRenderer.DelayedAdd does. I have even less idea why this works.
    // My best guess is that it has something to do with Il2CppInterop and its marshalling code.
    // There may be a better target for this patch that fixes more bugs, but this is the best I've found.
    //
    // BEHOLD!
    [HarmonyPatch(typeof(LogRenderer), nameof(LogRenderer.DelayedAdd))]
    [HarmonyPostfix]
    private static void AllYourDialogueAreBelongToUs() { }
}