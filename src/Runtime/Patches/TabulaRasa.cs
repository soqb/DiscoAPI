using DiscoAPI.Runtime.Utils;
using FortressOccident;
using HarmonyLib;
using Sunshine;
using UnityEngine;

namespace DiscoAPI.Runtime.Patches;

internal static class TabulaRasa
{
    private static void Log(string message)
    {
        DiscoRunner.Log.LogInfo($"[TabulaRasa] {message}");
    }

    public static bool IsCustomContent(GameObject obj)
    {
        if (obj == null)
            return false;

        if (OverlayRegistry.IsPartOfOverlay(obj))
            return true;

        var scene = obj.scene;
        if (scene.IsValid() && AreaUtils.FromSceneName(scene.name) != null)
            return true;

        return false;
    }

    private static bool ShouldDisableObject(GameObject obj)
    {
        if (obj == null)
            return false;

        if (obj.GetComponent<TransitionEntity>() != null)
            return false;

        if (IsCustomContent(obj))
            return false;

        if (obj.GetComponent<CharacterScheduleManager>() != null)
            return true;

        if (obj.GetComponent<SenseOrb>() != null)
            return true;

        if (obj.GetComponent<ContainerSource>() != null)
            return true;

        if (obj.GetComponent<BasicEntity>() != null)
            return true;

        if (obj.GetComponent<GeneralScheduleManager>() != null)
            return true;

        // if (obj.GetComponent<SpriteRenderer>() != null)
        //     return true;

        return false;
    }

    private static void DisableVanillaChildren(GameObject parent)
    {
        var transform = parent.transform;
        for (int i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i).gameObject;

            if (ShouldDisableObject(child))
            {
                Log($"Disabling vanilla object: '{child.name}'");
                child.SetActive(false);
            }

            if (child.transform.childCount > 0)
            {
                DisableVanillaChildren(child);
            }
        }
    }

    [HarmonyPatch(typeof(SceneLoadingManager), nameof(SceneLoadingManager.SetObjects))]
    [HarmonyPrefix]
    private static void DisableVanillaContent(SceneLoadingManager __instance)
    {
        if (!DiscoAPISettings.EnableTabulaRasa)
            return;

        Log($"Disabling vanilla content");

        for (int i = 0; i < __instance.ObjectList.Count; i++)
        {
            var obj = __instance.ObjectList[i];
            if (obj != null)
            {
                DisableVanillaChildren(obj);
            }
        }

        Log($"Completed disabling vanilla conent");
    }
}
