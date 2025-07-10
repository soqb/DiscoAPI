using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AddressablesTools;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using DiscoAPI.Common.Assets;
using DiscoAPI.Runtime.Components;
using FortressOccident;
using HarmonyLib;
using Sunshine;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

namespace DiscoAPI.Runtime.Patches;

public static class AreaPatches
{
    
    [HarmonyPatch(typeof(ApplicationManager), nameof(ApplicationManager.ChangeArea))]
    [HarmonyPrefix]
    private static void OnChangeArea(string areaId, string destinationId, ref bool isGameLoad)
    {
        Area? foundArea = ModWorld.Areas.FirstOrDefault(a => a.id == areaId);
        if (foundArea == default)
        {
            ModAreaState.isLoadingModState = false;
            return;
        }

        ModAreaState.isLoadingModState = true;
        ApplicationManager.Singleton.ScenePropertiesList.Add(foundArea.GetSceneProperties());
        // resolve navmesh asset
        FastLoadManager.m_FastLoadManager.navMeshDataCollection.NavMeshes.Add(null);
        FastLoadManager.m_FastLoadManager.navMeshDataCollection.dict.Add(foundArea.sceneName, null);
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
    private static void OnLoadSceneCoRPostfix(string sceneName, string destinationId, bool showLoadingScreen,
        bool hideLoadingScreen, bool isMapChanging)
    {
        if (!ModAreaState.isLoadingModState) return;
        NavMesh.RemoveAllNavMeshData();
        NavMesh.AddNavMeshData(FastLoadManager.m_FastLoadManager.navMeshDataCollection.dict[sceneName]);
    }

    private static Scene LoadModScene(string sceneName, string bundleName, string aliasPrefix = "")
    {
        var bundle = new AssetBundleRoute<Scene>(bundleName, aliasPrefix);
        var loadOp = bundle.Get(sceneName);
        var result = loadOp.WaitForCompletion();

        if (loadOp.Status == AsyncOperationStatus.Failed)
        {
            DiscoRunner.Log.LogError($"could not load {sceneName} from bundle {bundleName}!");
            return default;
        }

        return result;
    }
}

internal static class ModAreaState
{
    public static string bundleName = "scenes";
    public static bool isLoadingModState;
}