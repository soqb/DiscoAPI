using System.Linq;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using DiscoAPI.Common.Assets;
using DiscoAPI.Runtime.Utils;
using FortressOccident;
using HarmonyLib;
using Il2CppSystem.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace DiscoAPI.Runtime.Patches;

public static class AreaPatches
{
    [HarmonyPatch(typeof(ApplicationManager), nameof(ApplicationManager.ChangeArea))]
    [HarmonyPrefix]
    private static void OnChangeArea(ApplicationManager __instance, string areaId, string destinationId, ref bool isGameLoad)
    {
        if (__instance.CurrentSceneProperties != null)
        {
            var currentScene = __instance.CurrentSceneProperties.SceneName;
            Area? foundPresentArea = AreaUtils.FromScenePath(currentScene);
            if (foundPresentArea != null)
            {
                __instance.ScenePropertiesList.Remove(AreaUtils.GetSceneProperties(foundPresentArea));
                FastLoadManager.m_FastLoadManager.StartCoroutine(UnloadArea(foundPresentArea).WrapToIl2Cpp());
            }
        }

        Area? foundNextArea = AreaUtils.Areas.FirstOrDefault(a => a.id == areaId);
        if (foundNextArea == null) return;

        __instance.ScenePropertiesList.Add(AreaUtils.GetSceneProperties(foundNextArea));
    }

    private static System.Collections.IEnumerator UnloadArea(Area area)
    {
        var oldSceneFound = FastLoadManager.m_FastLoadManager.allScenes.TryGetValue(area.scenePath, out Scene oldScene);
        if (!oldSceneFound)
        {
            DiscoRunner.Log.LogError($"called to unload scene {area.scenePath} that was not loaded in FastLoadManager!");
            yield break;
        }

        FastLoadManager.m_FastLoadManager.allScenes.Remove(area.scenePath);

        yield return SceneManager.UnloadSceneAsync(oldScene);
        yield return null;

        Resources.UnloadUnusedAssets();
        DiscoRunner.Log.LogInfo($"unload of area '{area.id}' complete");
    }

    [HarmonyPatch(typeof(SceneTransitionManager), nameof(SceneTransitionManager.LoadSceneCoR))]
    [HarmonyPostfix]
    private static void OnLoadSceneCoRPostfix(ref IEnumerator __result, string sceneName, string destinationId, bool showLoadingScreen,
        bool hideLoadingScreen, bool isMapChanging)
    {
        var foundArea = AreaUtils.FromScenePath(sceneName); // sceneName is actually a path
        if (foundArea == null) return;

        __result = PatchedSceneLoadRoutine(__result, foundArea).WrapToIl2Cpp();
    }

    private static System.Collections.IEnumerator PatchedSceneLoadRoutine(IEnumerator original, Area foundArea)
    {
        var task = AreaUtils.LoadModScene(foundArea);
        yield return task.ToCoroutine();

        if (!task.Result)
        {
            DiscoRunner.Log.LogError($"failed to load scene {foundArea.scenePath}");
            yield break;
        }

        FastLoadManager.m_FastLoadManager.allScenes.TryAdd(foundArea.scenePath, SceneManager.GetSceneAt(SceneManager.sceneCount - 1));

        if (foundArea.vtPath != null)
        {
            var vt = VirtualTextures.CustomVirtualTextureManager.VtFromArea(foundArea);

            yield return null;

            original.MoveNext();
            yield return original.Current;

            // scene is now loaded:
            GameObject vtContainer = new();
            vtContainer.transform.position = Vector3.zero;
            vtContainer.transform.localScale = Vector3.one * 0.55f;
            VirtualTextures.VTSceneContents.InstantiateForTexture(vtContainer.transform, vt);
        }
        else
        {
            yield return WaitFor.EndOfFrame();
        }

        if (!FastLoadManager.m_FastLoadManager.navMeshDataCollection.dict.ContainsKey(foundArea.id))
        {
            var navLoadOp = AreaUtils.LoadNavmeshData(foundArea);
            yield return navLoadOp.ToCoroutine();

            FastLoadManager.m_FastLoadManager.navMeshDataCollection.dict.Add(foundArea.id, navLoadOp.Result);
        }

        yield return original;
    }


    [HarmonyPatch(typeof(NavMeshDataCollection), nameof(NavMeshDataCollection.GetNavMeshDataByScene))]
    [HarmonyPrefix]
    private static bool OnGetNavMeshDataByScene(ref NavMeshData? __result, string scenePath)
    {
        var sceneNameNoHardcodePath = scenePath[14..^6];
        var foundArea = AreaUtils.Areas.FirstOrDefault(a => a.id == sceneNameNoHardcodePath);
        if (foundArea == null) return true;

        __result = FastLoadManager.m_FastLoadManager.navMeshDataCollection.dict[sceneNameNoHardcodePath];
        return false;
    }

}

public class Areas
{
    public static AssetLocation<Area> Whirling_int_f1 = new("Whirling-int-f1");
    public static AssetLocation<Area> Whirling_int_f2 = new("Whirling-int-f2");
    public static AssetLocation<Area> Whirling_int_f3 = new("Whirling-int-f3");
    public static AssetLocation<Area> Whirling_int_f3_antechamber = new("Whirling-int-f3-antechamber");
    public static AssetLocation<Area> Martinaise_ext = new("Martinaise-ext");
    public static AssetLocation<Area> Doomed_commerce_int_f1 = new("Doomed-commerce-int-f1");
    public static AssetLocation<Area> Doomed_commerce_int_f2 = new("Doomed-commerce-int-f2");
    public static AssetLocation<Area> Doomed_commerce_int_s1 = new("Doomed-commerce-int-s1");
    public static AssetLocation<Area> Capeside_coalchamber_int = new("Capeside-coalchamber-int");
    public static AssetLocation<Area> Secretary_int = new("Secretary-int");
    public static AssetLocation<Area> Pawnshop_int = new("Pawnshop-int");
    public static AssetLocation<Area> Cunos_shack_int = new("Cunos-shack-int");
    public static AssetLocation<Area> Capesider_smoker_int = new("Capeside-smoker-int");
    public static AssetLocation<Area> Sea_fortress_int = new("Sea-fortress-int");
    public static AssetLocation<Area> Tent_int = new("Tent-int");
    public static AssetLocation<Area> Second_home_int = new("Second-home-int");
    public static AssetLocation<Area> FV_house_int = new("FV-house-int");
    public static AssetLocation<Area> Instigators_lair_int = new("Instigators-lair-int");
    public static AssetLocation<Area> Union_container_int = new("Union-container-int");
    public static AssetLocation<Area> Union_boss_int = new("Union-boss-int");
    public static AssetLocation<Area> Capeside_wcw_int = new("Capeside-wcw-int");
    public static AssetLocation<Area> Crypto_garys_apt_int = new("Crypto-garys-apt-int");
    public static AssetLocation<Area> Dream_2 = new("Dream-2");
    public static AssetLocation<Area> Dream_3_ext = new("Dream-3-ext");
    public static AssetLocation<Area> Dream_3_int = new("Dream-3-int");
    public static AssetLocation<Area> FV_shack_int = new("FV-shack-int");
    public static AssetLocation<Area> Feld_int = new("Feld-int");
    public static AssetLocation<Area> Commustudent_int = new("Commustudent-int");
}