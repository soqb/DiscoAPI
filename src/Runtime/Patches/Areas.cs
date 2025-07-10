using System.Linq;
using DiscoAPI.Common.Assets;
using DiscoAPI.Runtime.Assets;
using DiscoAPI.Runtime.Components;
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
    private static void OnChangeArea(string areaId, string destinationId, ref bool isGameLoad)
    {
        Area? foundArea = ModAreaState.Areas.FirstOrDefault(a => a.id == areaId);
        if (foundArea == default)
        {
            ModAreaState.isLoadingModState = false;
            return;
        }

        ModAreaState.isLoadingModState = true;
        ApplicationManager.Singleton.ScenePropertiesList.Add(foundArea.GetSceneProperties());
        
        var navMesh = LoadNavmeshData(foundArea.navMeshLocation, ModAreaState.bundleName);
        FastLoadManager.m_FastLoadManager.navMeshDataCollection.dict.Add(foundArea.sceneName, navMesh);
    }


    [HarmonyPatch(typeof(SceneTransitionManager), nameof(SceneTransitionManager.LoadSceneCoR))]
    [HarmonyPrefix]
    private static void OnLoadSceneCoR(string sceneName, string destinationId, bool showLoadingScreen,
        bool hideLoadingScreen, bool isMapChanging)
    {
        if (!ModAreaState.isLoadingModState) return;
        var scene = LoadModScene(sceneName, ModAreaState.bundleName);
        FastLoadManager.m_FastLoadManager.allScenes.Add(sceneName, scene);
    }
    
    [HarmonyPatch(typeof(SceneTransitionManager), nameof(SceneTransitionManager.LoadSceneCoR))]
    [HarmonyPostfix]
    private static System.Collections.IEnumerator OnLoadSceneCoRPostfix(string sceneName, string destinationId, bool showLoadingScreen,
        bool hideLoadingScreen, bool isMapChanging, IEnumerator __result)
    {
        while (__result.MoveNext())
            yield return __result.Current;
        
        if (!ModAreaState.isLoadingModState) yield break;
        NavMesh.RemoveAllNavMeshData();
        NavMesh.AddNavMeshData(FastLoadManager.m_FastLoadManager.navMeshDataCollection.dict[sceneName]);
    }

    private static Scene LoadModScene(string sceneName, string bundleName, string aliasPrefix = "")
    {
        ModAreaState.sceneBundle ??= AssetBundle.LoadFromFile(bundleName);
        var scenePath = ModAreaState.sceneBundle.GetAllScenePaths().FirstOrDefault(s => s == sceneName);

        if (scenePath == default)
        {
            DiscoRunner.Log.LogError($"could not load {sceneName} from bundle {bundleName}!");
            return default;
        }

        return SceneManager.LoadScene(sceneName, new LoadSceneParameters() { loadSceneMode = LoadSceneMode.Additive });
    }

    private static NavMeshData LoadNavmeshData(string meshPath, string bundleName)
    {
        ModAreaState.sceneBundle ??= AssetBundle.LoadFromFile(bundleName);

        return ModAreaState.sceneBundle.LoadAsset<NavMeshData>(meshPath);
    }
}

internal static class ModAreaState
{
    public static string bundleName = "BepInEx/plugins/dca/assetbundles/scenes";
    public static bool isLoadingModState;
    public static AssetBundle? sceneBundle;
    public static IAssetArena<Area> Areas => DiscoRunner.manager.Assets.GetArena<Area>();
}