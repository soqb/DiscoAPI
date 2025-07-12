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
        Area? foundArea = AreaUtils.Areas.FirstOrDefault(a => a.id == areaId);
        if (foundArea == default)
        {
            return;
        }
        
        ApplicationManager.Singleton.ScenePropertiesList.Add(foundArea.GetSceneProperties());
        
        var navMesh = LoadNavmeshData(foundArea.navMeshLocation, "");
        FastLoadManager.m_FastLoadManager.navMeshDataCollection.dict.Add(foundArea.sceneName, navMesh);
    }


    [HarmonyPatch(typeof(SceneTransitionManager), nameof(SceneTransitionManager.LoadSceneCoR))]
    [HarmonyPrefix]
    private static void OnLoadSceneCoR(string sceneName, string destinationId, bool showLoadingScreen,
        bool hideLoadingScreen, bool isMapChanging)
    {
        if (!AreaUtils.IsModdedArea(sceneName)) return;
        
        var scene = AreaUtils.LoadModScene(AreaUtils.Areas.First(a => a.sceneName == sceneName));
        FastLoadManager.m_FastLoadManager.allScenes.Add(sceneName, scene);
    }
    
    [HarmonyPatch(typeof(SceneTransitionManager), nameof(SceneTransitionManager.LoadSceneCoR))]
    [HarmonyPostfix]
    private static System.Collections.IEnumerator OnLoadSceneCoRPostfix(string sceneName, string destinationId, bool showLoadingScreen,
        bool hideLoadingScreen, bool isMapChanging, IEnumerator __result)
    {
        while (__result.MoveNext())
            yield return __result.Current;
        
        if (!AreaUtils.IsModdedArea(sceneName)) yield break;
        NavMesh.RemoveAllNavMeshData();
        NavMesh.AddNavMeshData(FastLoadManager.m_FastLoadManager.navMeshDataCollection.dict[sceneName]);
    }
    

    private static NavMeshData LoadNavmeshData(string meshPath, string bundleName)
    {
        AreaUtils.sceneBundle ??= AssetBundle.LoadFromFile(bundleName);

        return AreaUtils.sceneBundle.LoadAsset<NavMeshData>(meshPath);
    }
}
