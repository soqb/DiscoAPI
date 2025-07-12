using System.Linq;
using BepInEx.Unity.IL2CPP.Utils.Collections;
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
        if (foundArea == null)
        {
            return;
        }
        
        ApplicationManager.Singleton.ScenePropertiesList.Add(foundArea.GetSceneProperties());
    }
    
    [HarmonyPatch(typeof(SceneTransitionManager), nameof(SceneTransitionManager.LoadSceneCoR))]
    [HarmonyPostfix]
    private static void OnLoadSceneCoRPostfix(ref IEnumerator __result, IEnumerator __state, string sceneName, string destinationId, bool showLoadingScreen,
        bool hideLoadingScreen, bool isMapChanging)
    {
        if (!AreaUtils.TryFromSceneName(sceneName, out Area foundArea))
        {
            return;
        }
        
        __result = SpecialRoutine(__result, foundArea).WrapToIl2Cpp();
    }

    private static System.Collections.IEnumerator SpecialRoutine(IEnumerator original, Area foundArea)
    {
        yield return AreaUtils.LoadModScene(foundArea);
        FastLoadManager.m_FastLoadManager.allScenes.Add(foundArea.sceneName, SceneManager.GetSceneAt(SceneManager.sceneCount - 1));

        yield return null;
        
        while (original.MoveNext())
            yield return original.Current;
    }

    [HarmonyPatch(typeof(NavMeshDataCollection), nameof(NavMeshDataCollection.GetNavMeshDataByScene))]
    [HarmonyPrefix]
    private static bool OnGetNavMeshDataByScene(ref NavMeshData __result, string scenePath)
    {
        var sceneWithNoPathHardcode = scenePath[14..^6];
        var trueScenePath =
            FastLoadManager.m_FastLoadManager.allScenes.entries.FirstOrDefault(s => s.key.Contains(sceneWithNoPathHardcode));
        if (trueScenePath == null || !AreaUtils.TryFromSceneName(trueScenePath.key, out Area foundArea))
        {
            return true;
        }
        
        var navMesh = AreaUtils.LoadNavmeshData(foundArea);
        FastLoadManager.m_FastLoadManager.navMeshDataCollection.dict.Add(foundArea.sceneName, navMesh);
        __result = navMesh;
        return false;
    }
    
}
